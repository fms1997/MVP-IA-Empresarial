using System.Text;
using LocalMind.Api.DTOs.Documents;
using LocalMind.Api.Services.Ai;
using LocalMind.Api.Services.Rag;
namespace LocalMind.Api.Services.Chat;

public class ChatService : IChatService
{
    private readonly IOllamaService _ollamaService;
    private readonly IRagService _ragService;
    public ChatService(IOllamaService ollamaService, IRagService ragService)
    {
        _ollamaService = ollamaService;
        _ragService = ragService;
    }

    public async Task<ChatResult> GenerateResponseAsync(int userId, string message, CancellationToken cancellationToken = default)
    {
        var ragResult = await _ragService.SearchAsync(userId, message, cancellationToken);

        if (!ragResult.HasContext)
        {
            return new ChatResult
            {
                Response = await _ollamaService.SendMessageAsync(message, cancellationToken),
                UsedRag = false
            };
        }

        var contextBuilder = new StringBuilder();
        foreach (var match in ragResult.Matches)
        {
            contextBuilder.AppendLine($"Fuente: {match.FileName} | chunk {match.ChunkIndex} | relevancia {match.Score:P1}");
            contextBuilder.AppendLine(match.Content);
            contextBuilder.AppendLine("---");
        }

        var systemPrompt = "Respondé siempre en español. Usá únicamente el contexto de documentos provisto cuando sea relevante. " +
                           "Si el contexto no alcanza para responder, decilo claramente. Incluí una sección final llamada 'Fuentes' con el nombre del archivo y chunk usado.";

        var userPrompt = $"Contexto de documentos:\n{contextBuilder}\n\nPregunta del usuario:\n{message}";
        var response = await _ollamaService.SendMessageAsync(systemPrompt, userPrompt, cancellationToken);

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
                Preview = match.Content.Length > 220 ? $"{match.Content[..220]}..." : match.Content
            }).ToList()
        };
    }
}