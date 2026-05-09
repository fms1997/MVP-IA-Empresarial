namespace LocalMind.Api.DTOs;

public class ChatRequest
{
    public int? ConversationId { get; set; }

    public string Message { get; set; } = string.Empty;
}