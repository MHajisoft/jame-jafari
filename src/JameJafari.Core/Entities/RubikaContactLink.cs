namespace JameJafari.Core.Entities;

public class RubikaContactLink
{
    public int Id { get; set; }
    public string NormalizedPhone { get; set; } = "";
    public string ChatId { get; set; } = "";
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
