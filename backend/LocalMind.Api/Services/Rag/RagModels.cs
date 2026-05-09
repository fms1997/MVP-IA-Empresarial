namespace LocalMind.Api.Services.Rag;

public record RagChunkMatch(
    int DocumentId,
    string FileName,
    int ChunkIndex,
    string Content,
    double Score);

public record RagSearchResult(IReadOnlyList<RagChunkMatch> Matches)
{
    public bool HasContext => Matches.Count > 0;
}
