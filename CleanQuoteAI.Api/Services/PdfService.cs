using System.Globalization;
using System.Text.Json.Nodes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CleanQuoteAI.Api.Services;

/// <summary>Génère le PDF d'un devis avec QuestPDF (charte : bleu marine #1e3a5f).</summary>
public class PdfService
{
    private const string Navy = "#1e3a5f";
    private const string LightGrey = "#f2f4f7";
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public byte[] GeneratePdf(Models.Devis devis)
    {
        var root = JsonNode.Parse(devis.ContenuJson)?["devis"]
            ?? throw new InvalidOperationException("JSON du devis invalide");

        var client = root["client"];
        var prestations = root["prestations"]?.AsArray() ?? [];
        var options = root["options"];
        var recap = root["recapitulatif"];
        var marche = root["comparaison_marche"];

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1f2937"));

                page.Header().Column(col =>
                {
                    col.Item().Background(Navy).Padding(16).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CleanQuote.AI").FontSize(20).Bold().FontColor(Colors.White);
                            c.Item().Text("Devis de nettoyage professionnel").FontSize(10).FontColor("#c8d3e0");
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Réf. {devis.Reference}").FontSize(12).Bold().FontColor(Colors.White);
                            c.Item().Text($"Date : {devis.CreatedAt.ToLocalTime():dd/MM/yyyy}").FontSize(9).FontColor("#c8d3e0");
                            c.Item().Text($"Statut : {devis.Statut}").FontSize(9).FontColor("#c8d3e0");
                        });
                    });
                    col.Item().PaddingTop(12);
                });

                page.Content().Column(col =>
                {
                    // --- Client ---
                    col.Item().PaddingBottom(10).Background(LightGrey).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Local").Bold().FontColor(Navy);
                            c.Item().Text($"{S(client?["type_local"])} — {N(client?["superficie_m2"])} m²");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Adresse").Bold().FontColor(Navy);
                            c.Item().Text(S(client?["adresse"]));
                        });
                    });

                    // --- Prestations ---
                    col.Item().PaddingBottom(4).Text("Prestations").FontSize(13).Bold().FontColor(Navy);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.6f);
                        });

                        table.Header(header =>
                        {
                            foreach (var title in new[] { "Prestation", "Fréquence", "Temps (h)", "Tarif HT/h", "Montant HT" })
                                header.Cell().Background(Navy).Padding(6)
                                    .Text(title).Bold().FontColor(Colors.White).FontSize(9);
                        });

                        var odd = false;
                        foreach (var p in prestations)
                        {
                            var bg = odd ? LightGrey : "#ffffff";
                            table.Cell().Background(bg).Padding(6).Text(S(p?["nom"]));
                            table.Cell().Background(bg).Padding(6).Text(S(p?["frequence"]));
                            table.Cell().Background(bg).Padding(6).Text(N(p?["temps_estime_h"]));
                            table.Cell().Background(bg).Padding(6).Text(Euro(p?["tarif_horaire_ht"]));
                            table.Cell().Background(bg).Padding(6).Text(Euro(p?["montant_ht"]));
                            odd = !odd;
                        }
                    });

                    // --- Options ---
                    if (options is not null)
                    {
                        col.Item().PaddingTop(14).PaddingBottom(4).Text("Vos 3 options").FontSize(13).Bold().FontColor(Navy);
                        col.Item().Row(row =>
                        {
                            AddOption(row, "Économique", options["economique"], false);
                            AddOption(row, "Standard", options["standard"], true);
                            AddOption(row, "Premium", options["premium"], false);
                        });
                    }

                    // --- Récapitulatif ---
                    if (recap is not null)
                    {
                        col.Item().PaddingTop(14).AlignRight().Width(240).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total HT mensuel").Bold();
                                r.ConstantItem(90).AlignRight().Text(Euro(recap["total_ht_mensuel"]));
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total TTC mensuel").Bold();
                                r.ConstantItem(90).AlignRight().Text(Euro(recap["total_ttc_mensuel"]));
                            });
                            c.Item().Background(Navy).Padding(6).Row(r =>
                            {
                                r.RelativeItem().Text("Total TTC annuel").Bold().FontColor(Colors.White);
                                r.ConstantItem(90).AlignRight().Text(Euro(recap["total_ttc_annuel"])).Bold();
                            });
                        });
                    }

                    // --- Comparaison marché ---
                    if (marche is not null)
                    {
                        col.Item().PaddingTop(14).Background(LightGrey).Padding(10).Column(c =>
                        {
                            c.Item().Text("Comparaison avec le marché").Bold().FontColor(Navy);
                            c.Item().Text(
                                $"Fourchette constatée : {Euro(marche["prix_bas"])} à {Euro(marche["prix_haut"])} / mois — " +
                                $"Notre position : {S(marche["notre_position"])}");
                        });
                    }
                });

                page.Footer().PaddingTop(8).BorderTop(1).BorderColor("#d0d7e2").Row(row =>
                {
                    row.RelativeItem().Text("CleanQuote.AI — Devis généré par intelligence artificielle, valable 30 jours. TVA 20 %.")
                        .FontSize(8).FontColor("#6b7280");
                    row.ConstantItem(60).AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor("#6b7280");
                        x.CurrentPageNumber().FontSize(8).FontColor("#6b7280");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void AddOption(RowDescriptor row, string titre, JsonNode? option, bool highlight)
    {
        row.RelativeItem().Padding(3).Border(1).BorderColor(highlight ? Navy : "#d0d7e2").Column(c =>
        {
            c.Item().Background(highlight ? Navy : LightGrey).Padding(6).AlignCenter()
                .Text(titre).Bold().FontColor(highlight ? Colors.White : Navy);
            c.Item().Padding(8).AlignCenter()
                .Text($"{Euro(option?["total_ttc_mensuel"])} TTC/mois").FontSize(12).Bold().FontColor(Navy);
            c.Item().PaddingHorizontal(8).PaddingBottom(8)
                .Text(S(option?["description"])).FontSize(8).FontColor("#4b5563");
        });
    }

    private static string S(JsonNode? node) => node?.ToString() ?? "—";

    private static double? ToDouble(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        if (value.TryGetValue<double>(out var d)) return d;
        if (value.TryGetValue<string>(out var s) &&
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }

    private static string N(JsonNode? node)
        => ToDouble(node) is { } d ? d.ToString("0.##", Fr) : "—";

    private static string Euro(JsonNode? node)
        => ToDouble(node) is { } d ? d.ToString("N2", Fr) + " €" : "—";
}
