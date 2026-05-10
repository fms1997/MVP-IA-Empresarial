namespace LocalMind.Api.Models;

public class ChatMetric
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int? ConversationId { get; set; }

    public string ModelUsed { get; set; } = string.Empty;

    public long ResponseTimeMs { get; set; }

    public int ApproxTokens { get; set; }

    public bool UsedRag { get; set; }

    public bool UsedTool { get; set; }

    public string? ToolName { get; set; }

    public int ChunksUsed { get; set; }

    public string Route { get; set; } = "chat";

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
