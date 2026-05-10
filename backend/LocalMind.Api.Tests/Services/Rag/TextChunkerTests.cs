using LocalMind.Api.Services.Rag;

namespace LocalMind.Api.Tests.Services.Rag;

public class TextChunkerTests
{
    [Fact]
    public void Split_ReturnsEmptyList_WhenTextIsWhitespace()
    {
        var chunker = new TextChunker();

        var chunks = chunker.Split("   \n\t  ", 900, 150);

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_NormalizesWhitespaceAndCreatesChunks()
    {
        var chunker = new TextChunker();
        var text = string.Join(" ", Enumerable.Range(1, 220).Select(index => $"palabra{index}"));

        var chunks = chunker.Split(text, 300, 50);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.DoesNotContain("  ", chunk));
    }
}
