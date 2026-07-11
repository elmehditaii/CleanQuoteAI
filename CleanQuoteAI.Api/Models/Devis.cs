namespace CleanQuoteAI.Api.Models;

public class Devis
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Reference { get; set; } = null!;
    public string ContenuJson { get; set; } = null!; // jsonb
    public string Statut { get; set; } = "brouillon";
    public DateTime CreatedAt { get; set; }

    public Conversation? Conversation { get; set; }
}
