using LocalMind.Api.DTOs.Documents;

namespace LocalMind.Api.Services.Rag;

public interface IRagService
{
    Task<DocumentResponse> UploadDocumentAsync(int userId, IFormFile file, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentResponse>> GetDocumentsAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunkResponse>> GetDocumentChunksAsync(int userId, int documentId, CancellationToken cancellationToken = default);

    Task<RagSearchResult> SearchAsync(int userId, string query, CancellationToken cancellationToken = default);
}
