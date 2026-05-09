namespace LocalMind.Api.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    public Conversation Conversation { get; set; } = null!;

    public string Role { get; set; } = string.Empty;
    // user | assistant

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}