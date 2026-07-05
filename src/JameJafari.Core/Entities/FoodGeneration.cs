namespace JameJafari.Core.Entities;

public class FoodGeneration : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime CookDate { get; set; }
    public int TotalCount { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CostPerUnit { get; set; }
    public string? Description { get; set; }

    public ICollection<FoodIngredient> Ingredients { get; set; } = [];
}
