using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using CleanQuoteAI.Api.Services;

namespace CleanQuoteAI.Api.Endpoints;

public static class ChatEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private const string SystemPrompt = """
        Tu es un expert en devis pour entreprises de nettoyage professionnel
        avec 20 ans d'expérience en France.

        Collecte ces informations UNE PAR UNE si manquantes :
        - Type de local (bureau, commerce, entrepôt, restaurant, médical)
        - Superficie totale en m²
        - Nombre d'étages et de sanitaires
        - Fréquence souhaitée (quotidien, x fois/semaine, mensuel)
        - Horaires (jour, soir, nuit, week-end)
        - Localisation (ville, code postal)
        - Contraintes particulières (matériaux, produits éco, accès sécurisé)

        Quand tu as toutes les infos, génère le devis en JSON dans ce format,
        à l'intérieur d'un bloc de code ```json :
        {
          "devis": {
            "reference": "DV-2026-XXXX",
            "client": { "type_local": "", "superficie_m2": 0, "adresse": "" },
            "prestations": [
              {
                "nom": "",
                "frequence": "",
                "temps_estime_h": 0,
                "tarif_horaire_ht": 0,
                "montant_ht": 0
              }
            ],
            "options": {
              "economique": { "total_ttc_mensuel": 0, "description": "" },
              "standard":   { "total_ttc_mensuel": 0, "description": "" },
              "premium":    { "total_ttc_mensuel": 0, "description": "" }
            },
            "recapitulatif": {
              "total_ht_mensuel": 0,
              "total_ttc_mensuel": 0,
              "total_ttc_annuel": 0
            },
            "comparaison_marche": {
              "prix_bas": 0,
              "prix_haut": 0,
              "notre_position": "compétitif | premium | économique"
            }
          }
        }

        Propose toujours 3 options : Économique / Standard / Premium.
        Compare toujours avec les prix du marché fournis dans le contexte RAG.
        Justifie chaque prix avec les normes de temps standards.
        Réponds toujours en français.
        """;

    private record ChatRequest(string SessionId, Guid? ConversationId, string Message);

    public static void MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", HandleChatAsync);
    }

    private static async Task HandleChatAsync(
        HttpContext ctx,
        ChatRequest request,
        AnthropicClient anthropic,
        ConversationService conversations,
        RagService rag,
        DevisService devisService,
        IConfiguration config,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Chat");
        var ct = ctx.RequestAborted;

        ctx.Response.Headers.ContentType = "text/event-stream";
        ctx.Response.Headers.CacheControl = "no-cache";
        ctx.Response.Headers.Connection = "keep-alive";

        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                await WriteSseAsync(ctx, new { type = "error", message = "Message vide." }, ct);
                return;
            }

            var conversation = await conversations.GetOrCreateAsync(request.SessionId, request.ConversationId, ct);
            await conversations.AddMessageAsync(conversation.Id, "user", request.Message, ct);
            await WriteSseAsync(ctx, new { type = "start", conversationId = conversation.Id }, ct);

            // Contexte RAG : les 5 chunks les plus proches de la question
            string ragContext;
            try
            {
                var chunks = await rag.SearchAsync(request.Message, top: 5, ct);
                ragContext = "Contexte RAG (tarifs, temps standards, exemples et normes internes) :\n\n"
                    + string.Join("\n\n", chunks.Select(c => $"[{c.Categorie}] {c.Contenu}"));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Recherche RAG indisponible, poursuite sans contexte");
                ragContext = "Contexte RAG indisponible : utilise les tarifs moyens du marché français du nettoyage.";
            }

            var history = await conversations.GetMessagesAsync(conversation.Id, ct);
            var messages = history
                .Select(m => new MessageParam
                {
                    Role = m.Role == "user" ? Role.User : Role.Assistant,
                    Content = m.Contenu,
                })
                .ToList();

            var parameters = new MessageCreateParams
            {
                Model = config["Anthropic:Model"] ?? "claude-sonnet-4-6",
                MaxTokens = 16000,
                Thinking = new ThinkingConfigAdaptive(),
                System = new List<TextBlockParam>
                {
                    new() { Text = SystemPrompt, CacheControl = new CacheControlEphemeral() },
                    new() { Text = ragContext },
                },
                Messages = messages,
            };

            var fullText = new StringBuilder();
            await foreach (var streamEvent in anthropic.Messages.CreateStreaming(parameters).WithCancellation(ct))
            {
                if (streamEvent.TryPickContentBlockDelta(out var delta) &&
                    delta.Delta.TryPickText(out var text))
                {
                    fullText.Append(text.Text);
                    await WriteSseAsync(ctx, new { type = "delta", text = text.Text }, ct);
                }
            }

            var assistantText = fullText.ToString();
            await conversations.AddMessageAsync(conversation.Id, "assistant", assistantText, ct);

            // Détection et sauvegarde du devis structuré
            if (DevisService.ExtractDevisJson(assistantText) is { } devisJson)
            {
                var devis = await devisService.SaveAsync(conversation.Id, devisJson, ct);
                await WriteSseAsync(ctx, new
                {
                    type = "devis",
                    devisId = devis.Id,
                    reference = devis.Reference,
                    devis = JsonSerializer.Deserialize<JsonElement>(devisJson),
                }, ct);
            }

            await WriteSseAsync(ctx, new { type = "done", conversationId = conversation.Id }, ct);
        }
        catch (OperationCanceledException)
        {
            // client déconnecté — rien à faire
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur pendant le chat");
            if (!ct.IsCancellationRequested)
            {
                await WriteSseAsync(ctx, new
                {
                    type = "error",
                    message = "Une erreur est survenue : " + ex.Message,
                }, CancellationToken.None);
            }
        }
    }

    private static async Task WriteSseAsync(HttpContext ctx, object payload, CancellationToken ct)
    {
        await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(payload, JsonOpts)}\n\n", ct);
        await ctx.Response.Body.FlushAsync(ct);
    }
}
