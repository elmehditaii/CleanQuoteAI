using System.Text.Json;
using CleanQuoteAI.Api.Services;

namespace CleanQuoteAI.Api.Endpoints;

public static class DevisEndpoints
{
    public static void MapDevisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/conversations", async (string? sessionId, ConversationService conversations, CancellationToken ct) =>
            Results.Ok(await conversations.ListAsync(sessionId, ct)));

        app.MapGet("/api/conversations/{id:guid}/messages", async (Guid id, ConversationService conversations, CancellationToken ct) =>
        {
            var messages = await conversations.GetMessagesAsync(id, ct);
            return Results.Ok(messages.Select(m => new
            {
                id = m.Id,
                role = m.Role,
                contenu = m.Contenu,
                createdAt = m.CreatedAt,
            }));
        });

        app.MapGet("/api/devis/{id:guid}", async (Guid id, DevisService devisService, CancellationToken ct) =>
        {
            var devis = await devisService.GetAsync(id, ct);
            if (devis is null) return Results.Problem(statusCode: 404, title: "Devis introuvable");
            return Results.Ok(new
            {
                id = devis.Id,
                conversationId = devis.ConversationId,
                reference = devis.Reference,
                statut = devis.Statut,
                createdAt = devis.CreatedAt,
                contenu = JsonSerializer.Deserialize<JsonElement>(devis.ContenuJson),
            });
        });

        app.MapGet("/api/devis/{id:guid}/pdf", async (Guid id, DevisService devisService, PdfService pdfService, CancellationToken ct) =>
        {
            var devis = await devisService.GetAsync(id, ct);
            if (devis is null) return Results.Problem(statusCode: 404, title: "Devis introuvable");
            var pdf = pdfService.GeneratePdf(devis);
            return Results.File(pdf, "application/pdf", $"{devis.Reference}.pdf");
        });
    }
}
