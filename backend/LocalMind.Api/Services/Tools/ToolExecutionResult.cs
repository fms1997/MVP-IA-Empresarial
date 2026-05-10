namespace LocalMind.Api.Services.Tools;

public class ToolExecutionResult
{
    public string ToolName { get; init; } = string.Empty;

    public string Response { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
        new Dictionary<string, object?>();
}
