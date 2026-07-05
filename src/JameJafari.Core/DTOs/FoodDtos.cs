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
    int CostTypeId,
    decimal Units,
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
    string Name,
    DateTime CookDate,
    int TotalCount,
    string? Description,
    IReadOnlyList<FoodIngredientInput> Ingredients);

public record IngredientPriceRecommendationDto(
    int CostTypeId,
    string CostTypeName,
    string? UnitName,
    decimal RecommendedPrice);
