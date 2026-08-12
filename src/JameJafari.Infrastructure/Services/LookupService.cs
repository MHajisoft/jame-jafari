using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class LookupService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<LookupItemDto>> GetAccountsAsync(bool activeOnly = true) =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupAccounts(activeOnly),
            async _ =>
            {
                var query = db.Accounts.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(a => a.IsActive);
                return await query
                    .OrderBy(a => a.Name)
                    .Select(a => new LookupItemDto(a.Id, a.Name))
                    .ToListAsync();
            },
            options => options.SetDuration(LookupCache.AccountsDuration));

    public async Task<IReadOnlyList<CostTypeLookupItemDto>> GetCostTypesAsync(
        bool? isIngredient = null,
        bool activeOnly = true) =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupCostTypes(isIngredient, activeOnly),
            async _ =>
            {
                var query = db.CostTypes.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(c => c.IsActive);
                if (isIngredient.HasValue) query = query.Where(c => c.IsIngredient == isIngredient.Value);
                return await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CostTypeLookupItemDto(
                        c.Id,
                        c.Name,
                        c.Unit != null ? c.Unit.Name : null))
                    .ToListAsync();
            },
            options => options.SetDuration(LookupCache.CostTypesDuration));

    public async Task<IReadOnlyList<LookupItemDto>> GetGeneralTypesAsync(GeneralTypeCategory category) =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupGeneralTypes(category),
            async _ => await db.GeneralTypes.AsNoTracking()
                .Where(g => g.Category == category && g.IsActive)
                .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
                .Select(g => new LookupItemDto(g.Id, g.Name))
                .ToListAsync(),
            options => options.SetDuration(LookupCache.GeneralTypesDuration));
}
