using CleanQuoteAI.Api.Data;
using CleanQuoteAI.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CleanQuoteAI.Api.Services;

/// <summary>Charge et sauvegarde l'historique des conversations depuis PostgreSQL.</summary>
public class ConversationService(AppDbContext db)
{
    public async Task<Conversation> GetOrCreateAsync(string sessionId, Guid? conversationId, CancellationToken ct = default)
    {
        if (conversationId is { } id)
        {
            var existing = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (existing is not null) return existing;
        }

        var conversation = new Conversation { SessionId = sessionId, CreatedAt = DateTime.UtcNow };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task AddMessageAsync(Guid conversationId, string role, string contenu, CancellationToken ct = default)
    {
        db.Messages.Add(new Message
        {
            ConversationId = conversationId,
            Role = role,
            Contenu = contenu,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    public Task<List<Message>> GetMessagesAsync(Guid conversationId, CancellationToken ct = default)
        => db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
            .ToListAsync(ct);

    public async Task<List<object>> ListAsync(string? sessionId, CancellationToken ct = default)
    {
        var query = db.Conversations.AsQueryable();
        if (!string.IsNullOrEmpty(sessionId))
            query = query.Where(c => c.SessionId == sessionId);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => (object)new
            {
                id = c.Id,
                sessionId = c.SessionId,
                createdAt = c.CreatedAt,
                apercu = c.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => m.Contenu)
                    .FirstOrDefault(),
                nbMessages = c.Messages.Count,
                devisId = c.Devis
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => (Guid?)d.Id)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
    }
}
