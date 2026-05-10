namespace LocalMind.Api.Services.Tools;

public interface IAiToolService
{
    Task<ToolExecutionResult?> TryExecuteAsync(
        ToolIntent intent,
        string message,
        CancellationToken cancellationToken = default);
}
