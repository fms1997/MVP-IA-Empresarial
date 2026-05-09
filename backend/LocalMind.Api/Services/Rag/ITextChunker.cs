namespace LocalMind.Api.Services.Rag;

public interface ITextChunker
{
    IReadOnlyList<string> Split(string text, int chunkSize, int overlap);
}
