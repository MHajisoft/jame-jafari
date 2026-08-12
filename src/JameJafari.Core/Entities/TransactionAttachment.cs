namespace JameJafari.Core.Entities;

public class TransactionAttachment
{
    public int Id { get; set; }
    public string Path { get; set; } = null!;
    public int? IncomeTransactionId { get; set; }
    public IncomeTransaction? IncomeTransaction { get; set; }
    public int? CostTransactionId { get; set; }
    public CostTransaction? CostTransaction { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
