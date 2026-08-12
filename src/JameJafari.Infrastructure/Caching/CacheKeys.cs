using JameJafari.Core.Enums;

namespace JameJafari.Infrastructure.Caching;

public static class CacheKeys
{
    public const string PermissionsAll = "permissions:all";
    public const string IngredientPriceRecs = "ingredient-price-recs";

    public static string GeneralTypes(GeneralTypeCategory category, bool includeInactive) =>
        $"general-types:{(int)category}:{includeInactive}";

    public static string Accounts(bool activeOnly) => $"accounts:{activeOnly}";

    public static string CostTypes(bool? isIngredient, bool activeOnly) =>
        $"cost-types:{(isIngredient.HasValue ? isIngredient.Value.ToString() : "all")}:{activeOnly}";
}
