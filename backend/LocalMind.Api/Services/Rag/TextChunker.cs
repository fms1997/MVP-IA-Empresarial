using System.Text.RegularExpressions;

namespace LocalMind.Api.Services.Rag;

public partial class TextChunker : ITextChunker
{
    public IReadOnlyList<string> Split(string text, int chunkSize, int overlap)
    {
        var normalized = WhitespaceRegex().Replace(text, " ").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        chunkSize = Math.Max(300, chunkSize);
        overlap = Math.Clamp(overlap, 0, chunkSize / 2);

        var chunks = new List<string>();
        var start = 0;

        while (start < normalized.Length)
        {
            var length = Math.Min(chunkSize, normalized.Length - start);
            var end = start + length;

            if (end < normalized.Length)
            {
                var sentenceEnd = normalized.LastIndexOfAny(new[] { '.', '?', '!', '\n' }, end - 1, length);
                if (sentenceEnd > start + chunkSize / 2)
                {
                    end = sentenceEnd + 1;
                }
            }

            var chunk = normalized[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            if (end >= normalized.Length)
            {
                break;
            }

            start = Math.Max(0, end - overlap);
        }

        return chunks;
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
