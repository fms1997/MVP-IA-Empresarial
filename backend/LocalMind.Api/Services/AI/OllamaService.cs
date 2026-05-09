using System.Text;
using System.Text.Json;

namespace LocalMind.Api.Services.Ai;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> SendMessageAsync(string message)
    {
        var model = _configuration["Ollama:Model"] ?? "qwen2.5-coder:7b";

        var payload = new
        {
            model,
            messages = new[]
            {
            new { role = "system", content = "Respondé siempre en español." },
            new { role = "user", content = message }
        },
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);

        Console.WriteLine("Enviando a Ollama:");
        Console.WriteLine(json);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/chat", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Respuesta Ollama:");
            Console.WriteLine(responseJson);

            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(responseJson);

            return document.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "No pude generar una respuesta.";
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR OLLAMA:");
            Console.WriteLine(ex.ToString());

            return $"Error al conectar con Ollama: {ex.Message}";
        }
    }
}