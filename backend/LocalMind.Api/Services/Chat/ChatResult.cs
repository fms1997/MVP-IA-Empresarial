using LocalMind.Api.DTOs.Documents;

namespace LocalMind.Api.Services.Chat;

public class ChatResult
{
    public string Response { get; set; } = string.Empty;

    public bool UsedRag { get; set; }

    public int ChunksUsed { get; set; }

    public IReadOnlyList<RagSourceResponse> Sources { get; set; } = Array.Empty<RagSourceResponse>();
}
