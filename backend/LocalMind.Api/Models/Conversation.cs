namespace LocalMind.Api.Models;

public class Conversation
{
    public int Id { get; set; }

    public string Title { get; set; } = "Nueva conversación";

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ChatMessage> Messages { get; set; } = new();
}