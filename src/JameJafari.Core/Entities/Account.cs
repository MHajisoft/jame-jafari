namespace JameJafari.Core.Entities;

public class Account : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IncomeTransaction> IncomeTransactions { get; set; } = [];
    public ICollection<CostTransaction> CostTransactions { get; set; } = [];
}
