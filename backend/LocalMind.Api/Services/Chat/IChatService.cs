namespace LocalMind.Api.Services.Chat;

public interface IChatService
{
    Task<string> GenerateResponseAsync(string message);
}