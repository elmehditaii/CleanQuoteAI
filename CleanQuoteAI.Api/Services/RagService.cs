using System.Text.Json;
using CleanQuoteAI.Api.Data;
using CleanQuoteAI.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace CleanQuoteAI.Api.Services;

/// <summary>
/// Indexe Data/tarifs.json dans pgvector au démarrage et fournit la recherche
/// sémantique (top 5 chunks par similarité cosinus) pour le contexte du chat.
/// </summary>
public class RagService(
    AppDbContext db,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IWebHostEnvironment env,
    ILogger<RagService> logger)
{
    private const int BatchSize = 64;

    private record TarifChunk(string Categorie, string Contenu, JsonElement? Metadata);

    public async Task EnsureIndexedAsync(CancellationToken ct = default)
    {
        if (await db.RagChunks.AnyAsync(ct))
        {
            logger.LogInformation("RAG : {Count} chunks déjà indexés, indexation ignorée",
                await db.RagChunks.CountAsync(ct));
            return;
        }

        var path = Path.Combine(env.ContentRootPath, "Data", "tarifs.json");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", "tarifs.json");

        var json = await File.ReadAllTextAsync(path, ct);
        var chunks = JsonSerializer.Deserialize<List<TarifChunk>>(json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("tarifs.json invalide");

        logger.LogInformation("RAG : indexation de {Count} chunks via Voyage AI...", chunks.Count);

        foreach (var batch in chunks.Chunk(BatchSize))
        {
            var vectors = await embeddings.GenerateAsync(
                batch.Select(c => c.Contenu),
                new EmbeddingGenerationOptions
                {
                    Dimensions = 1024,
                    AdditionalProperties = new() { ["input_type"] = "document" },
                },
                ct);

            for (var i = 0; i < batch.Length; i++)
            {
                db.RagChunks.Add(new RagChunk
                {
                    Categorie = batch[i].Categorie,
                    Contenu = batch[i].Contenu,
                    Embedding = new Vector(vectors[i].Vector),
                    Metadata = batch[i].Metadata?.GetRawText(),
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("RAG : indexation terminée");
    }

    public async Task<List<RagChunk>> SearchAsync(string question, int top = 5, CancellationToken ct = default)
    {
        var result = await embeddings.GenerateAsync(
            [question],
            new EmbeddingGenerationOptions
            {
                Dimensions = 1024,
                AdditionalProperties = new() { ["input_type"] = "query" },
            },
            ct);

        var queryVector = new Vector(result[0].Vector);

        return await db.RagChunks
            .OrderBy(c => c.Embedding!.CosineDistance(queryVector))
            .Take(top)
            .ToListAsync(ct);
    }
}
