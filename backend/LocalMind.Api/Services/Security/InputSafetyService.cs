using Microsoft.Extensions.Options;

namespace LocalMind.Api.Services.Security;

public class InputSafetyService : IInputSafetyService
{
    private readonly ChatSecurityOptions _options;

    public InputSafetyService(IOptions<ChatSecurityOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateChatMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("El mensaje no puede estar vacío.");
        }

        if (message.Length > _options.MaxMessageLength)
        {
            throw new InvalidOperationException($"El mensaje supera el límite de {_options.MaxMessageLength} caracteres.");
        }

        var normalized = message.Trim().ToLowerInvariant();
        var isBlocked = _options.BlockedPromptPatterns.Any(pattern =>
            normalized.Contains(pattern.ToLowerInvariant(), StringComparison.Ordinal));

        if (isBlocked)
        {
            throw new InvalidOperationException("El mensaje fue bloqueado por seguridad básica de prompt injection.");
        }
    }
}
