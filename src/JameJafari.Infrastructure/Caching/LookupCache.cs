using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Caching;

public static class LookupCache
{
    public static readonly TimeSpan PermissionsDuration = TimeSpan.FromHours(1);
    public static readonly TimeSpan GeneralTypesDuration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan AccountsDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CostTypesDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan IngredientRecsDuration = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan PersonsDuration = TimeSpan.FromMinutes(15);

    public static Task InvalidatePermissionsAsync(IFusionCache cache, CancellationToken ct = default) =>
        cache.RemoveAsync(CacheKeys.PermissionsAll, token: ct).AsTask();

    public static async Task InvalidateAccountsAsync(IFusionCache cache, CancellationToken ct = default)
    {
        await cache.RemoveAsync(CacheKeys.Accounts(true), token: ct);
        await cache.RemoveAsync(CacheKeys.Accounts(false), token: ct);
        await cache.RemoveAsync(CacheKeys.LookupAccounts(true), token: ct);
        await cache.RemoveAsync(CacheKeys.LookupAccounts(false), token: ct);
    }

    public static async Task InvalidateGeneralTypesAsync(IFusionCache cache, CancellationToken ct = default)
    {
        foreach (Core.Enums.GeneralTypeCategory category in Enum.GetValues<Core.Enums.GeneralTypeCategory>())
        {
            await cache.RemoveAsync(CacheKeys.GeneralTypes(category, true), token: ct);
            await cache.RemoveAsync(CacheKeys.GeneralTypes(category, false), token: ct);
            await cache.RemoveAsync(CacheKeys.LookupGeneralTypes(category), token: ct);
        }
    }

    public static async Task InvalidateCostTypesAsync(IFusionCache cache, CancellationToken ct = default)
    {
        foreach (var ingredient in new bool?[] { null, true, false })
        {
            await cache.RemoveAsync(CacheKeys.CostTypes(ingredient, true), token: ct);
            await cache.RemoveAsync(CacheKeys.CostTypes(ingredient, false), token: ct);
            await cache.RemoveAsync(CacheKeys.LookupCostTypes(ingredient, true), token: ct);
            await cache.RemoveAsync(CacheKeys.LookupCostTypes(ingredient, false), token: ct);
        }

        await InvalidateIngredientRecsAsync(cache, ct);
    }

    public static Task InvalidateIngredientRecsAsync(IFusionCache cache, CancellationToken ct = default) =>
        cache.RemoveAsync(CacheKeys.IngredientPriceRecs, token: ct).AsTask();

    public static Task InvalidatePersonsAsync(IFusionCache cache, CancellationToken ct = default) =>
        cache.RemoveAsync(CacheKeys.LookupPersons, token: ct).AsTask();
}
