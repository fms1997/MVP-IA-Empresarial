using LocalMind.Api.Services.Ai;

namespace LocalMind.Api.Services.Chat;

public class ChatService : IChatService
{
    private readonly IOllamaService _ollamaService;

    public ChatService(IOllamaService ollamaService)
    {
        _ollamaService = ollamaService;
    }

    public async Task<string> GenerateResponseAsync(string message)
    {
        return await _ollamaService.SendMessageAsync(message);
    }
}