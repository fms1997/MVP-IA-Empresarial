using System.Text;
using LocalMind.Api.DTOs.Documents;
using LocalMind.Api.Services.Ai;
using LocalMind.Api.Services.Rag;
using LocalMind.Api.Services.Tools;

namespace LocalMind.Api.Services.Chat;

public class ChatService : IChatService
{
    private readonly IOllamaService _ollamaService;
    private readonly IRagService _ragService;
    private readonly IToolIntentDetector _toolIntentDetector;
    private readonly IAiToolService _aiToolService;

    public ChatService(
        IOllamaService ollamaService,
        IRagService ragService,
        IToolIntentDetector toolIntentDetector,
        IAiToolService aiToolService)
    {
        _ollamaService = ollamaService;
        _ragService = ragService;
        _toolIntentDetector = toolIntentDetector;
        _aiToolService = aiToolService;
    }

    public async Task<ChatResult> GenerateResponseAsync(
        int userId,
        string message,
        CancellationToken cancellationToken = default)
    {
        /*
         * FLUJO VIEJO, COMENTADO:
         *
         * Antes el sistema hacía esto primero:
         *
         * var ragResult = await _ragService.SearchAsync(userId, message, cancellationToken);
         *
         * Eso provocaba que preguntas como:
         *
         * "Calculá 120 / 20"
         *
         * terminaran usando RAG si había algún documento parecido cargado.
         *
         * Por eso te devolvía:
         *
         * usedRag: true
         * usedTool: false
         * toolName: null
         * route: "chat"
         *
         * O sea: respondía bien a veces, pero no ejecutaba la tool real.
         */

        /*
         * FLUJO NUEVO:
         *
         * 1. Primero detectamos si el mensaje parece pedir una tool.
         * 2. También detectamos si es una pregunta sobre documentos.
         * 3. Si es tool y NO es pregunta de documento, ejecutamos la tool.
         * 4. Si no aplica tool, seguimos con RAG como antes.
         */

        var toolIntent = _toolIntentDetector.Detect(message);
        var isDocumentQuestion = _toolIntentDetector.IsDocumentQuestion(message);

        if (toolIntent is not ToolIntent.None && !isDocumentQuestion)
        {
            var toolResult = await _aiToolService.TryExecuteAsync(
                toolIntent,
                message,
                cancellationToken);

            if (toolResult is not null)
            {
                return new ChatResult
                {
                    Response = toolResult.Response,
                    UsedTool = true,
                    ToolName = toolResult.ToolName,
                    Route = "tool"
                };
            }
        }

        /*
         * RAG:
         *
         * Si no hubo tool, o si la pregunta era sobre documentos,
         * recién acá buscamos contexto en documentos.
         */

        var ragResult = await _ragService.SearchAsync(
            userId,
            message,
            cancellationToken);

        /*
         * CHAT NORMAL:
         *
         * Si RAG no encuentra contexto útil, se responde con Ollama normal.
         */

        if (!ragResult.HasContext)
        {
            return new ChatResult
            {
                Response = await _ollamaService.SendMessageAsync(
                    message,
                    cancellationToken),
                UsedRag = false
            };
        }

        /*
         * RESPUESTA CON RAG:
         *
         * Si RAG sí encontró contexto, construimos el prompt con fuentes.
         */

        var contextBuilder = new StringBuilder();

        foreach (var match in ragResult.Matches)
        {
            contextBuilder.AppendLine(
                $"Fuente: {match.FileName} | chunk {match.ChunkIndex} | relevancia {match.Score:P1}");

            contextBuilder.AppendLine(match.Content);
            contextBuilder.AppendLine("---");
        }

        var systemPrompt =
            "Respondé siempre en español. Usá únicamente el contexto de documentos provisto cuando sea relevante. " +
            "Si el contexto no alcanza para responder, decilo claramente. Incluí una sección final llamada 'Fuentes' con el nombre del archivo y chunk usado.";

        var userPrompt =
            $"Contexto de documentos:\n{contextBuilder}\n\nPregunta del usuario:\n{message}";

        var response = await _ollamaService.SendMessageAsync(
            systemPrompt,
            userPrompt,
            cancellationToken);

        return new ChatResult
        {
            Response = response,
            UsedRag = true,
            ChunksUsed = ragResult.Matches.Count,
            Sources = ragResult.Matches.Select(match => new RagSourceResponse
            {
                DocumentId = match.DocumentId,
                FileName = match.FileName,
                ChunkIndex = match.ChunkIndex,
                Score = Math.Round(match.Score, 4),
                Preview = match.Content.Length > 220
                    ? $"{match.Content[..220]}..."
                    : match.Content
            }).ToList()
        };
    }
}