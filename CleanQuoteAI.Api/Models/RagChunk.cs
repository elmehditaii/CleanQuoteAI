using Pgvector;

namespace CleanQuoteAI.Api.Models;

public class RagChunk
{
    public int Id { get; set; }
    public string Categorie { get; set; } = null!;
    public string Contenu { get; set; } = null!;
    public Vector? Embedding { get; set; } // vector(1024) — dimension Voyage AI
    public string? Metadata { get; set; }  // jsonb
    public DateTime CreatedAt { get; set; }
}
