namespace JameJafari.Core.Entities;

public class CostTransaction : AuditableEntity
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public decimal Amount { get; set; }
    public int CostTypeId { get; set; }
    public CostType CostType { get; set; } = null!;
    public ICollection<TransactionAttachment> Attachments { get; set; } = [];
    public string? TrackingCode { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
