namespace CleanQuoteAI.Api.Models;

public class Message
{
    public int Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = null!; // 'user' | 'assistant'
    public string Contenu { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Conversation? Conversation { get; set; }
}
