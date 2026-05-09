using LocalMind.Api.Data;
using LocalMind.Api.DTOs.Documents;
using LocalMind.Api.Models;
using LocalMind.Api.Services.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalMind.Api.Services.Rag;

public class RagService : IRagService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".txt",
        ".md"
    };

    private readonly AppDbContext _context;
    private readonly IOllamaService _ollamaService;
    private readonly IDocumentTextExtractor _textExtractor;
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingSerializer _embeddingSerializer;
    private readonly RagOptions _options;

    public RagService(
        AppDbContext context,
        IOllamaService ollamaService,
        IDocumentTextExtractor textExtractor,
        ITextChunker chunker,
        IEmbeddingSerializer embeddingSerializer,
        IOptions<RagOptions> options)
    {
        _context = context;
        _ollamaService = ollamaService;
        _textExtractor = textExtractor;
        _chunker = chunker;
        _embeddingSerializer = embeddingSerializer;
        _options = options.Value;
    }

    public async Task<DocumentResponse> UploadDocumentAsync(int userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var storageRoot = GetStorageRoot();
        var documentsPath = Path.Combine(storageRoot, "documents", userId.ToString());
        var chunksPath = Path.Combine(storageRoot, "chunks", userId.ToString());
        var vectorStorePath = Path.Combine(storageRoot, "vector-store");
        Directory.CreateDirectory(documentsPath);
        Directory.CreateDirectory(chunksPath);
        Directory.CreateDirectory(vectorStorePath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedFilePath = Path.Combine(documentsPath, storedFileName);

        await using (var output = File.Create(storedFilePath))
        await using (var input = file.OpenReadStream())
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        var extractedText = await _textExtractor.ExtractTextAsync(file, cancellationToken);
        var chunks = _chunker.Split(extractedText, _options.ChunkSize, _options.ChunkOverlap);

        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("No se pudo extraer texto útil del documento.");
        }

        var document = new Document
        {
            UserId = userId,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Status = "Processed"
        };

        for (var index = 0; index < chunks.Count; index++)
        {
            var chunk = chunks[index];
            var embedding = await _ollamaService.GenerateEmbeddingAsync(chunk, cancellationToken);
            var chunkFileName = $"{Path.GetFileNameWithoutExtension(storedFileName)}-{index}.txt";
            await File.WriteAllTextAsync(Path.Combine(chunksPath, chunkFileName), chunk, cancellationToken);

            document.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = index,
                Content = chunk,
                EmbeddingJson = _embeddingSerializer.Serialize(embedding),
                SourceFileName = document.OriginalFileName
            });
        }

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        return ToResponse(document);
    }

    public async Task<IReadOnlyList<DocumentResponse>> GetDocumentsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(document => document.UserId == userId)
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new DocumentResponse
            {
                Id = document.Id,
                OriginalFileName = document.OriginalFileName,
                SizeBytes = document.SizeBytes,
                Status = document.Status,
                ChunkCount = document.Chunks.Count,
                CreatedAt = document.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunkResponse>> GetDocumentChunksAsync(int userId, int documentId, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Documents
            .AnyAsync(document => document.Id == documentId && document.UserId == userId, cancellationToken);

        if (!exists)
        {
            return Array.Empty<DocumentChunkResponse>();
        }

        return await _context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == documentId)
            .OrderBy(chunk => chunk.ChunkIndex)
            .Select(chunk => new DocumentChunkResponse
            {
                Id = chunk.Id,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RagSearchResult> SearchAsync(int userId, string query, CancellationToken cancellationToken = default)
    {
        var chunks = await _context.DocumentChunks
            .AsNoTracking()
            .Include(chunk => chunk.Document)
            .Where(chunk => chunk.Document.UserId == userId)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return new RagSearchResult(Array.Empty<RagChunkMatch>());
        }

        var queryEmbedding = await _ollamaService.GenerateEmbeddingAsync(query, cancellationToken);

        var matches = chunks
            .Select(chunk => new RagChunkMatch(
                chunk.DocumentId,
                chunk.SourceFileName,
                chunk.ChunkIndex,
                chunk.Content,
                CosineSimilarity(queryEmbedding, _embeddingSerializer.Deserialize(chunk.EmbeddingJson))))
            .Where(match => match.Score >= _options.MinSimilarityScore)
            .OrderByDescending(match => match.Score)
            .Take(_options.MaxRetrievedChunks)
            .ToList();

        return new RagSearchResult(matches);
    }

    private static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
        {
            return 0;
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var index = 0; index < left.Count; index++)
        {
            dot += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("El archivo está vacío.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"El archivo supera el límite de {_options.MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Solo se permiten archivos PDF, TXT o MD.");
        }
    }

    private string GetStorageRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.StorageRoot));
    }

    private static DocumentResponse ToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            OriginalFileName = document.OriginalFileName,
            SizeBytes = document.SizeBytes,
            Status = document.Status,
            ChunkCount = document.Chunks.Count,
            CreatedAt = document.CreatedAt
        };
    }
}
