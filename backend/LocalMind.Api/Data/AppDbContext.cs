using LocalMind.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalMind.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatMetric> ChatMetrics => Set<ChatMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>()
            .HasMany(document => document.Chunks)
            .WithOne(chunk => chunk.Document)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasIndex(document => new { document.UserId, document.CreatedAt });

        modelBuilder.Entity<DocumentChunk>()
            .HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex });
        modelBuilder.Entity<ChatMetric>()
            .HasIndex(metric => new { metric.UserId, metric.CreatedAt });
    }
}