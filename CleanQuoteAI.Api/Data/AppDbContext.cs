using CleanQuoteAI.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CleanQuoteAI.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Devis> Devis => Set<Devis>();
    public DbSet<RagChunk> RagChunks => Set<RagChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Conversation>(e =>
        {
            e.ToTable("conversations");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(c => c.SessionId).HasColumnName("session_id").HasMaxLength(100).IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.HasIndex(c => c.SessionId);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("messages");
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasColumnName("id");
            e.Property(m => m.ConversationId).HasColumnName("conversation_id");
            e.Property(m => m.Role).HasColumnName("role").HasMaxLength(20);
            e.Property(m => m.Contenu).HasColumnName("contenu");
            e.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId);
        });

        modelBuilder.Entity<Devis>(e =>
        {
            e.ToTable("devis");
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(d => d.ConversationId).HasColumnName("conversation_id");
            e.Property(d => d.Reference).HasColumnName("reference").HasMaxLength(50);
            e.Property(d => d.ContenuJson).HasColumnName("contenu_json").HasColumnType("jsonb");
            e.Property(d => d.Statut).HasColumnName("statut").HasMaxLength(20).HasDefaultValue("brouillon");
            e.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.HasOne(d => d.Conversation)
                .WithMany(c => c.Devis)
                .HasForeignKey(d => d.ConversationId);
        });

        modelBuilder.Entity<RagChunk>(e =>
        {
            e.ToTable("rag_chunks");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Categorie).HasColumnName("categorie").HasMaxLength(100);
            e.Property(c => c.Contenu).HasColumnName("contenu");
            e.Property(c => c.Embedding).HasColumnName("embedding").HasColumnType("vector(1024)");
            e.Property(c => c.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            e.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            // Index vectoriel ivfflat (cosinus) pour la recherche rapide
            e.HasIndex(c => c.Embedding)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
        });
    }
}
