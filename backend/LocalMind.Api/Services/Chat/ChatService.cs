//using System.Text;
//using LocalMind.Api.DTOs.Documents;
//using LocalMind.Api.Services.Ai;
//using LocalMind.Api.Services.Rag;
//using LocalMind.Api.Services.Tools;

//namespace LocalMind.Api.Services.Chat;

//public class ChatService : IChatService
//{
//    private readonly IOllamaService _ollamaService;
//    private readonly IRagService _ragService;
//    private readonly IToolIntentDetector _toolIntentDetector;
//    private readonly IAiToolService _aiToolService;

//    public ChatService(
//        IOllamaService ollamaService,
//        IRagService ragService,
//        IToolIntentDetector toolIntentDetector,
//        IAiToolService aiToolService)
//    {
//        _ollamaService = ollamaService;
//        _ragService = ragService;
//        _toolIntentDetector = toolIntentDetector;
//        _aiToolService = aiToolService;
//    }

//    public async Task<ChatResult> GenerateResponseAsync(
//        int userId,
//        string message,
//        CancellationToken cancellationToken = default)
//    {
//        /*
//         * FLUJO VIEJO, COMENTADO:
//         *
//         * Antes el sistema hacía esto primero:
//         *
//         * var ragResult = await _ragService.SearchAsync(userId, message, cancellationToken);
//         *
//         * Eso provocaba que preguntas como:
//         *
//         * "Calculá 120 / 20"
//         *
//         * terminaran usando RAG si había algún documento parecido cargado.
//         *
//         * Por eso te devolvía:
//         *
//         * usedRag: true
//         * usedTool: false
//         * toolName: null
//         * route: "chat"
//         *
//         * O sea: respondía bien a veces, pero no ejecutaba la tool real.
//         */

//        /*
//         * FLUJO NUEVO:
//         *
//         * 1. Primero detectamos si el mensaje parece pedir una tool.
//         * 2. También detectamos si es una pregunta sobre documentos.
//         * 3. Si es tool y NO es pregunta de documento, ejecutamos la tool.
//         * 4. Si no aplica tool, seguimos con RAG como antes.
//         */

//        var toolIntent = _toolIntentDetector.Detect(message);
//        var isDocumentQuestion = _toolIntentDetector.IsDocumentQuestion(message);

//        if (toolIntent is not ToolIntent.None && !isDocumentQuestion)
//        {
//            var toolResult = await _aiToolService.TryExecuteAsync(
//                toolIntent,
//                message,
//                cancellationToken);

//            if (toolResult is not null)
//            {
//                return new ChatResult
//                {
//                    Response = toolResult.Response,
//                    UsedTool = true,
//                    ToolName = toolResult.ToolName,
//                    Route = "tool"
//                };
//            }
//        }

//        /*
//         * RAG:
//         *
//         * Si no hubo tool, o si la pregunta era sobre documentos,
//         * recién acá buscamos contexto en documentos.
//         */

//        var ragResult = await _ragService.SearchAsync(
//            userId,
//            message,
//            cancellationToken);

//        /*
//         * CHAT NORMAL:
//         *
//         * Si RAG no encuentra contexto útil, se responde con Ollama normal.
//         */

//        if (!ragResult.HasContext)
//        {
//            return new ChatResult
//            {
//                Response = await _ollamaService.SendMessageAsync(
//                    message,
//                    cancellationToken),
//                UsedRag = false
//            };
//        }

//        /*
//         * RESPUESTA CON RAG:
//         *
//         * Si RAG sí encontró contexto, construimos el prompt con fuentes.
//         */

//        var contextBuilder = new StringBuilder();

//        foreach (var match in ragResult.Matches)
//        {
//            contextBuilder.AppendLine(
//                $"Fuente: {match.FileName} | chunk {match.ChunkIndex} | relevancia {match.Score:P1}");

//            contextBuilder.AppendLine(match.Content);
//            contextBuilder.AppendLine("---");
//        }

//        var systemPrompt =
//            "Respondé siempre en español. Usá únicamente el contexto de documentos provisto cuando sea relevante. " +
//            "Si el contexto no alcanza para responder, decilo claramente. Incluí una sección final llamada 'Fuentes' con el nombre del archivo y chunk usado.";

//        var userPrompt =
//            $"Contexto de documentos:\n{contextBuilder}\n\nPregunta del usuario:\n{message}";

//        var response = await _ollamaService.SendMessageAsync(
//            systemPrompt,
//            userPrompt,
//            cancellationToken);

//        return new ChatResult
//        {
//            Response = response,
//            UsedRag = true,
//            ChunksUsed = ragResult.Matches.Count,
//            Sources = ragResult.Matches.Select(match => new RagSourceResponse
//            {
//                DocumentId = match.DocumentId,
//                FileName = match.FileName,
//                ChunkIndex = match.ChunkIndex,
//                Score = Math.Round(match.Score, 4),
//                Preview = match.Content.Length > 220
//                    ? $"{match.Content[..220]}..."
//                    : match.Content
//            }).ToList()
//        };
//    }
//}





































using System.Diagnostics;
using System.Security.Claims;
using LocalMind.Api.Data;
using LocalMind.Api.DTOs;
using LocalMind.Api.Models;
using LocalMind.Api.Services.Chat;
using LocalMind.Api.Services.Metrics;
using LocalMind.Api.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private const int ConversationTitleMaxLength = 40;

    private readonly AppDbContext _context;
    private readonly IChatService _chatService;
    private readonly IMetricsService _metricsService;
    private readonly IInputSafetyService _inputSafetyService;
    private readonly IConfiguration _configuration;

    public ChatController(
        AppDbContext context,
        IChatService chatService,
        IMetricsService metricsService,
        IInputSafetyService inputSafetyService,
        IConfiguration configuration)
    {
        _context = context;
        _chatService = chatService;
        _metricsService = metricsService;
        _inputSafetyService = inputSafetyService;
        _configuration = configuration;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage(
        ChatRequest request,
        CancellationToken cancellationToken)
    {
        _inputSafetyService.ValidateChatMessage(request.Message);

        var userId = GetUserId();
        var cleanMessage = request.Message.Trim();

        var conversation = await GetOrCreateConversationAsync(
            userId,
            request.ConversationId,
            cleanMessage,
            cancellationToken);

        if (conversation is null)
        {
            return NotFound(new
            {
                message = "No se encontró la conversación."
            });
        }

        var stopwatch = Stopwatch.StartNew();
        ChatResult? chatResult = null;

        try
        {
            chatResult = await _chatService.GenerateResponseAsync(
                userId,
                cleanMessage,
                cancellationToken);

            await SaveMessagesAsync(
                conversation.Id,
                cleanMessage,
                chatResult.Response,
                cancellationToken);

            stopwatch.Stop();

            await RecordMetricAsync(
                userId,
                conversation.Id,
                cleanMessage,
                chatResult,
                stopwatch.ElapsedMilliseconds,
                error: null,
                cancellationToken);

            return Ok(BuildChatResponse(conversation.Id, chatResult));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            await RecordMetricAsync(
                userId,
                conversation.Id,
                cleanMessage,
                chatResult,
                stopwatch.ElapsedMilliseconds,
                ex.Message,
                CancellationToken.None);

            throw;
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var conversations = await _context.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.CreatedAt)
            .Select(conversation => new
            {
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(conversations);
    }

    [HttpGet("history/{conversationId:int}")]
    public async Task<IActionResult> GetConversation(
        int conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        var conversation = await _context.Conversations
            .AsNoTracking()
            .Include(item => item.Messages)
            .FirstOrDefaultAsync(
                item => item.Id == conversationId && item.UserId == userId,
                cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            conversation.Id,
            conversation.Title,
            Messages = conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(message => new
                {
                    message.Role,
                    message.Content,
                    message.CreatedAt
                })
        });
    }

    private async Task<Conversation?> GetOrCreateConversationAsync(
        int userId,
        int? conversationId,
        string firstMessage,
        CancellationToken cancellationToken)
    {
        if (conversationId is not null and > 0)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(
                    conversation =>
                        conversation.Id == conversationId &&
                        conversation.UserId == userId,
                    cancellationToken);
        }

        var conversation = new Conversation
        {
            UserId = userId,
            Title = BuildConversationTitle(firstMessage),
            CreatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        return conversation;
    }

    private async Task SaveMessagesAsync(
        int conversationId,
        string userContent,
        string assistantContent,
        CancellationToken cancellationToken)
    {
        _context.ChatMessages.AddRange(
            new ChatMessage
            {
                ConversationId = conversationId,
                Role = "user",
                Content = userContent,
                CreatedAt = DateTime.UtcNow
            },
            new ChatMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = assistantContent,
                CreatedAt = DateTime.UtcNow
            });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordMetricAsync(
        int userId,
        int conversationId,
        string userMessage,
        ChatResult? chatResult,
        long elapsedMs,
        string? error,
        CancellationToken cancellationToken)
    {
        await _metricsService.RecordAsync(new ChatMetricCreate
        {
            UserId = userId,
            ConversationId = conversationId,
            ModelUsed = _configuration["Ollama:Model"] ?? "qwen2.5-coder:7b",
            ResponseTimeMs = elapsedMs,
            ApproxTokens = EstimateTokens(userMessage, chatResult?.Response),
            UsedRag = chatResult?.UsedRag ?? false,
            UsedTool = chatResult?.UsedTool ?? false,
            ToolName = chatResult?.ToolName,
            ChunksUsed = chatResult?.ChunksUsed ?? 0,
            Route = chatResult?.Route ?? "error",
            Error = error
        }, cancellationToken);
    }

    private static object BuildChatResponse(int conversationId, ChatResult chatResult)
    {
        return new
        {
            conversationId,
            response = chatResult.Response,
            usedRag = chatResult.UsedRag,
            usedTool = chatResult.UsedTool,
            toolName = chatResult.ToolName,
            route = chatResult.Route,
            chunksUsed = chatResult.ChunksUsed,
            sources = chatResult.Sources
        };
    }

    private static string BuildConversationTitle(string message)
    {
        return message.Length > ConversationTitleMaxLength
            ? message[..ConversationTitleMaxLength]
            : message;
    }

    private static int EstimateTokens(string prompt, string? response)
    {
        var totalCharacters = prompt.Length + (response?.Length ?? 0);

        return Math.Max(1, (int)Math.Ceiling(totalCharacters / 4.0));
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}