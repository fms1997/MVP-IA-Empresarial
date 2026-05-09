namespace LocalMind.Api.Services.Rag;

public interface IEmbeddingSerializer
{
    string Serialize(IReadOnlyList<float> embedding);

    float[] Deserialize(string embeddingJson);
}
