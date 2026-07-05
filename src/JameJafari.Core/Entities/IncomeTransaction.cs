using JameJafari.Core.Enums;

namespace JameJafari.Core.Entities;

public class IncomeTransaction : AuditableEntity
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public int CostTypeId { get; set; }
    public CostType CostType { get; set; } = null!;
    public string? DocumentPath { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
