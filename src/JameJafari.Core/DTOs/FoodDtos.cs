using System.ComponentModel.DataAnnotations;

namespace JameJafari.Core.DTOs;

public record FoodIngredientDto(
    int Id,
    int CostTypeId,
    string CostTypeName,
    string? UnitName,
    decimal Units,
    decimal Price,
    decimal? RecommendedPrice);

public record FoodIngredientInput(
    [Range(1, int.MaxValue, ErrorMessage = "ماده اولیه الزامی است")]
    int CostTypeId,

    [Range(0.0001, 999999, ErrorMessage = "مقدار باید بیشتر از صفر باشد")]
    decimal Units,

    [Range(0.01, 999999999, ErrorMessage = "قیمت باید بیشتر از صفر باشد")]
    decimal Price);

public record FoodGenerationDto(
    int Id,
    string Name,
    DateTime CookDate,
    int TotalCount,
    decimal TotalCost,
    decimal CostPerUnit,
    string? Description,
    IReadOnlyList<FoodIngredientDto> Ingredients,
    AuditInfoDto Audit);

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

public record IngredientPriceRecommendationDto(
    int CostTypeId,
    string CostTypeName,
    string? UnitName,
    decimal RecommendedPrice);
