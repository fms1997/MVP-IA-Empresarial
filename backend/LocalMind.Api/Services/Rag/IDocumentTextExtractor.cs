namespace LocalMind.Api.Services.Rag;

public interface IDocumentTextExtractor
{
    Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken = default);
}
