using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public class FoodIngredientResponse
{
    public int Id { get; init; }
    public int CostTypeId { get; init; }
    public string CostTypeName { get; init; } = "";
    public string? UnitName { get; init; }
    public decimal Units { get; init; }
    public decimal Price { get; init; }
    public decimal? RecommendedPrice { get; init; }
}

public record FoodIngredientInput(
    [Range(1, int.MaxValue, ErrorMessage = "ماده اولیه الزامی است")]
    int CostTypeId,

    [Range(0.0001, 999999, ErrorMessage = "مقدار باید بیشتر از صفر باشد")]
    decimal Units,

    [Range(0.01, 999999999, ErrorMessage = "قیمت باید بیشتر از صفر باشد")]
    decimal Price);

public class FoodGenerationResponse : ResponseBase
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public DateTime CookDate { get; init; }
    public int TotalCount { get; init; }
    public decimal TotalCost { get; init; }
    public decimal CostPerUnit { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<FoodIngredientResponse> Ingredients { get; init; } = [];
}

public record CreateFoodGenerationRequest(
    [Required(ErrorMessage = "نام غذا الزامی است")]
    [StringLength(200, ErrorMessage = "نام غذا حداکثر ۲۰۰ کاراکتر")]
    string Name,

    DateTime CookDate,

    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید حداقل ۱ باشد")]
    int TotalCount,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    [MinLength(1, ErrorMessage = "حداقل یک ماده اولیه الزامی است")]
    IReadOnlyList<FoodIngredientInput> Ingredients);

public record UpdateFoodGenerationRequest(
    [Required(ErrorMessage = "نام غذا الزامی است")]
    [StringLength(200, ErrorMessage = "نام غذا حداکثر ۲۰۰ کاراکتر")]
    string Name,

    DateTime CookDate,

    [Range(1, int.MaxValue, ErrorMessage = "تعداد باید حداقل ۱ باشد")]
    int TotalCount,

    [StringLength(500, ErrorMessage = "توضیحات حداکثر ۵۰۰ کاراکتر")]
    string? Description,

    [MinLength(1, ErrorMessage = "حداقل یک ماده اولیه الزامی است")]
    IReadOnlyList<FoodIngredientInput> Ingredients);

public record IngredientPriceRecommendationResponse(
    int CostTypeId,
    string CostTypeName,
    string? UnitName,
    decimal RecommendedPrice);
