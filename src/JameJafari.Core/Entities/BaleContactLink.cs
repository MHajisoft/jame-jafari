namespace JameJafari.Core.Entities;

public class BaleContactLink
{
    public int Id { get; set; }
    public string NormalizedPhone { get; set; } = "";
    public long ChatId { get; set; }
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
