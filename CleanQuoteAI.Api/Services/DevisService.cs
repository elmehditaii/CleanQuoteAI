using System.Text.Json;
using CleanQuoteAI.Api.Data;
using CleanQuoteAI.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CleanQuoteAI.Api.Services;

/// <summary>Extrait le devis JSON généré par Claude et le persiste en base.</summary>
public class DevisService(AppDbContext db)
{
    /// <summary>
    /// Cherche un objet JSON contenant une propriété racine "devis" dans la réponse
    /// de Claude (bloc ```json ... ``` ou JSON brut) et le retourne normalisé.
    /// </summary>
    public static string? ExtractDevisJson(string text)
    {
        // 1. Blocs de code ```json ... ```
        foreach (var candidate in EnumerateFencedBlocks(text))
        {
            if (TryParseDevis(candidate, out var normalized)) return normalized;
        }

        // 2. JSON brut : chercher chaque '{' et équilibrer les accolades
        for (var start = text.IndexOf('{'); start >= 0; start = text.IndexOf('{', start + 1))
        {
            var candidate = ExtractBalancedObject(text, start);
            if (candidate is not null && TryParseDevis(candidate, out var normalized)) return normalized;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateFencedBlocks(string text)
    {
        var index = 0;
        while (true)
        {
            var open = text.IndexOf("```", index, StringComparison.Ordinal);
            if (open < 0) yield break;
            var contentStart = text.IndexOf('\n', open);
            if (contentStart < 0) yield break;
            var close = text.IndexOf("```", contentStart, StringComparison.Ordinal);
            if (close < 0) yield break;
            yield return text[(contentStart + 1)..close];
            index = close + 3;
        }
    }

    private static string? ExtractBalancedObject(string text, int start)
    {
        var depth = 0;
        var inString = false;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
            }
            else if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return text[start..(i + 1)];
        }
        return null;
    }

    private static bool TryParseDevis(string candidate, out string normalized)
    {
        normalized = "";
        try
        {
            using var doc = JsonDocument.Parse(candidate);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("devis", out _))
            {
                normalized = doc.RootElement.GetRawText();
                return true;
            }
        }
        catch (JsonException)
        {
            // pas un JSON valide, on continue
        }
        return false;
    }

    public async Task<Devis> SaveAsync(Guid conversationId, string devisJson, CancellationToken ct = default)
    {
        var reference = ExtractReference(devisJson)
            ?? $"DV-{DateTime.UtcNow.Year}-{Random.Shared.Next(1000, 10000)}";

        var devis = new Devis
        {
            ConversationId = conversationId,
            Reference = reference,
            ContenuJson = devisJson,
            Statut = "brouillon",
            CreatedAt = DateTime.UtcNow,
        };
        db.Devis.Add(devis);
        await db.SaveChangesAsync(ct);
        return devis;
    }

    private static string? ExtractReference(string devisJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(devisJson);
            if (doc.RootElement.TryGetProperty("devis", out var d) &&
                d.TryGetProperty("reference", out var r) &&
                r.ValueKind == JsonValueKind.String)
            {
                var value = r.GetString();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch (JsonException) { }
        return null;
    }

    public Task<Devis?> GetAsync(Guid id, CancellationToken ct = default)
        => db.Devis.FirstOrDefaultAsync(d => d.Id == id, ct);
}
