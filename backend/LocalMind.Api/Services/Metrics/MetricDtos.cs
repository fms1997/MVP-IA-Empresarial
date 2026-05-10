namespace LocalMind.Api.Services.Metrics;

public class MetricSummaryResponse
{
    public int TotalRequests { get; set; }

    public int RagRequests { get; set; }

    public int ToolRequests { get; set; }

    public int ErrorCount { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public int TotalApproxTokens { get; set; }

    public int TotalChunksUsed { get; set; }

    public IReadOnlyList<RouteMetricResponse> Routes { get; set; } = Array.Empty<RouteMetricResponse>();

    public IReadOnlyList<RecentMetricResponse> Recent { get; set; } = Array.Empty<RecentMetricResponse>();
}

public class RouteMetricResponse
{
    public string Route { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class RecentMetricResponse
{
    public int Id { get; set; }

    public string ModelUsed { get; set; } = string.Empty;

    public long ResponseTimeMs { get; set; }

    public int ApproxTokens { get; set; }

    public bool UsedRag { get; set; }

    public bool UsedTool { get; set; }

    public string? ToolName { get; set; }

    public int ChunksUsed { get; set; }

    public string Route { get; set; } = string.Empty;

    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ChatMetricCreate
{
    public int UserId { get; set; }

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
}
