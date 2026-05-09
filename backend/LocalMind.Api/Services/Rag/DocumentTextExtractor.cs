using System.Text;
using UglyToad.PdfPig;

namespace LocalMind.Api.Services.Rag;

public class DocumentTextExtractor : IDocumentTextExtractor
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".md",
        ".pdf"
    };

    public async Task<string> ExtractTextAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Solo se permiten archivos PDF, TXT o MD.");
        }

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return await ExtractPdfTextAsync(file, cancellationToken);
        }

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> ExtractPdfTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var output = File.Create(tempFile))
            await using (var input = file.OpenReadStream())
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            var builder = new StringBuilder();
            using var document = PdfDocument.Open(tempFile);

            foreach (var page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }

            return builder.ToString();
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
