using LocalMind.Api.Data;
using LocalMind.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalMind.Api.Services.Metrics;

public class MetricsService : IMetricsService
{
    private readonly AppDbContext _context;

    public MetricsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(ChatMetricCreate metric, CancellationToken cancellationToken = default)
    {
        _context.ChatMetrics.Add(new ChatMetric
        {
            UserId = metric.UserId,
            ConversationId = metric.ConversationId,
            ModelUsed = metric.ModelUsed,
            ResponseTimeMs = metric.ResponseTimeMs,
            ApproxTokens = metric.ApproxTokens,
            UsedRag = metric.UsedRag,
            UsedTool = metric.UsedTool,
            ToolName = metric.ToolName,
            ChunksUsed = metric.ChunksUsed,
            Route = metric.Route,
            Error = metric.Error,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MetricSummaryResponse> GetSummaryAsync(int userId, CancellationToken cancellationToken = default)
    {
        var metrics = await _context.ChatMetrics
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .ToListAsync(cancellationToken);

        var totalRequests = metrics.Count;

        return new MetricSummaryResponse
        {
            TotalRequests = totalRequests,
            RagRequests = metrics.Count(metric => metric.UsedRag),
            ToolRequests = metrics.Count(metric => metric.UsedTool),
            ErrorCount = metrics.Count(metric => !string.IsNullOrWhiteSpace(metric.Error)),
            AverageResponseTimeMs = totalRequests == 0
                ? 0
                : Math.Round(metrics.Average(metric => metric.ResponseTimeMs), 2),
            TotalApproxTokens = metrics.Sum(metric => metric.ApproxTokens),
            TotalChunksUsed = metrics.Sum(metric => metric.ChunksUsed),
            Routes = metrics
                .GroupBy(metric => metric.Route)
                .Select(group => new RouteMetricResponse
                {
                    Route = group.Key,
                    Count = group.Count()
                })
                .OrderByDescending(route => route.Count)
                .ToList(),
            Recent = metrics
                .Take(20)
                .Select(metric => new RecentMetricResponse
                {
                    Id = metric.Id,
                    ModelUsed = metric.ModelUsed,
                    ResponseTimeMs = metric.ResponseTimeMs,
                    ApproxTokens = metric.ApproxTokens,
                    UsedRag = metric.UsedRag,
                    UsedTool = metric.UsedTool,
                    ToolName = metric.ToolName,
                    ChunksUsed = metric.ChunksUsed,
                    Route = metric.Route,
                    Error = metric.Error,
                    CreatedAt = metric.CreatedAt
                })
                .ToList()
        };
    }
}
