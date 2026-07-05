namespace JameJafari.Core.Entities;

public class CostType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsIngredient { get; set; }
    public int? UnitId { get; set; }
    public GeneralType? Unit { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<IncomeTransaction> IncomeTransactions { get; set; } = [];
    public ICollection<CostTransaction> CostTransactions { get; set; } = [];
    public ICollection<FoodIngredient> FoodIngredients { get; set; } = [];
}
