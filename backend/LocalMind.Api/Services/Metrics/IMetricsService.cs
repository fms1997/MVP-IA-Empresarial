namespace LocalMind.Api.Services.Metrics;

public interface IMetricsService
{
    Task RecordAsync(ChatMetricCreate metric, CancellationToken cancellationToken = default);

    Task<MetricSummaryResponse> GetSummaryAsync(int userId, CancellationToken cancellationToken = default);
}
