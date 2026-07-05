namespace JameJafari.Core.Entities;

public class FoodIngredient
{
    public int Id { get; set; }
    public int FoodGenerationId { get; set; }
    public FoodGeneration FoodGeneration { get; set; } = null!;
    public int CostTypeId { get; set; }
    public CostType CostType { get; set; } = null!;
    public decimal Units { get; set; }
    public decimal Price { get; set; }
    public decimal? RecommendedPrice { get; set; }
}
