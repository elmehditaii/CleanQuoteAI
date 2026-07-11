namespace CleanQuoteAI.Api.Models;

public class Conversation
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public List<Message> Messages { get; set; } = [];
    public List<Devis> Devis { get; set; } = [];
}
