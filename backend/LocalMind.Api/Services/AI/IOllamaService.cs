namespace LocalMind.Api.Services.Ai;

public interface IOllamaService
{
    Task<string> SendMessageAsync(string message);
}