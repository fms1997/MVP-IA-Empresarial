namespace LocalMind.Api.Services.Tools;

public interface IToolIntentDetector
{
    ToolIntent Detect(string message);

    bool IsDocumentQuestion(string message);
}
