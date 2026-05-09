using System.Text.Json;

namespace LocalMind.Api.Services.Rag;

public class EmbeddingSerializer : IEmbeddingSerializer
{
    public string Serialize(IReadOnlyList<float> embedding)
    {
        return JsonSerializer.Serialize(embedding);
    }

    public float[] Deserialize(string embeddingJson)
    {
        return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
    }
}
