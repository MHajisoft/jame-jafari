namespace JameJafari.Core.Entities;

public class WhatsAppContactLink
{
    public int Id { get; set; }
    public string NormalizedPhone { get; set; } = "";
    /// <summary>WhatsApp <c>wa_id</c> (digits, no +).</summary>
    public string ChatId { get; set; } = "";
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
