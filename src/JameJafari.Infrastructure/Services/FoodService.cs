using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class FoodService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<IngredientPriceRecommendationDto>> GetRecommendationsAsync()
    {
        return await cache.GetOrSetAsync(
            CacheKeys.IngredientPriceRecs,
            async _ => await ComputeRecommendationsAsync(),
            options => options.SetDuration(LookupCache.IngredientRecsDuration));
    }

    public async Task<IReadOnlyList<FoodGenerationDto>> GetByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var items = await db.FoodGenerations
            .AsNoTracking()
            .Include(f => f.Ingredients).ThenInclude(i => i.CostType).ThenInclude(c => c!.Unit)
            .Include(f => f.CreatedBy).Include(f => f.UpdatedBy)
            .Where(f => f.CookDate >= start && f.CookDate < end)
            .OrderBy(f => f.Name)
            .ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<FoodGenerationDto> CreateAsync(CreateFoodGenerationRequest request, int userId)
    {
        var totalCost = request.Ingredients.Sum(i => i.Units * i.Price);
        var costPerUnit = request.TotalCount > 0 ? totalCost / request.TotalCount : 0;

        var recommendations = await GetRecommendationsAsync();
        var requestedIds = request.Ingredients.Select(i => i.CostTypeId).ToHashSet();
        var recDict = recommendations
            .Where(r => requestedIds.Contains(r.CostTypeId))
            .ToDictionary(r => r.CostTypeId, r => r.RecommendedPrice);

        var entity = new FoodGeneration
        {
            Name = request.Name,
            CookDate = request.CookDate,
            TotalCount = request.TotalCount,
            TotalCost = totalCost,
            CostPerUnit = costPerUnit,
            Description = request.Description,
            CreatedById = userId,
            Ingredients = request.Ingredients.Select(i => new FoodIngredient
            {
                CostTypeId = i.CostTypeId,
                Units = i.Units,
                Price = i.Price,
                RecommendedPrice = recDict.GetValueOrDefault(i.CostTypeId)
            }).ToList()
        };

        db.FoodGenerations.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<FoodGenerationDto?> UpdateAsync(int id, UpdateFoodGenerationRequest request, int userId)
    {
        var entity = await db.FoodGenerations
            .Include(f => f.Ingredients)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (entity is null) return null;

        var totalCost = request.Ingredients.Sum(i => i.Units * i.Price);
        var costPerUnit = request.TotalCount > 0 ? totalCost / request.TotalCount : 0;

        var recommendations = await GetRecommendationsAsync();
        var requestedIds = request.Ingredients.Select(i => i.CostTypeId).ToHashSet();
        var recDict = recommendations
            .Where(r => requestedIds.Contains(r.CostTypeId))
            .ToDictionary(r => r.CostTypeId, r => r.RecommendedPrice);

        entity.Name = request.Name;
        entity.CookDate = request.CookDate;
        entity.TotalCount = request.TotalCount;
        entity.TotalCost = totalCost;
        entity.CostPerUnit = costPerUnit;
        entity.Description = request.Description;
        entity.UpdatedById = userId;

        db.FoodIngredients.RemoveRange(entity.Ingredients);
        entity.Ingredients = request.Ingredients.Select(i => new FoodIngredient
        {
            CostTypeId = i.CostTypeId,
            Units = i.Units,
            Price = i.Price,
            RecommendedPrice = recDict.GetValueOrDefault(i.CostTypeId)
        }).ToList();

        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return await GetByIdAsync(id);
    }

    public async Task<FoodGenerationDto?> GetByIdAsync(int id)
    {
        var f = await db.FoodGenerations
            .AsNoTracking()
            .Include(x => x.Ingredients).ThenInclude(i => i.CostType).ThenInclude(c => c!.Unit)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        return f is null ? null : Map(f);
    }

    private async Task<IReadOnlyList<IngredientPriceRecommendationDto>> ComputeRecommendationsAsync()
    {
        var ingredients = await db.CostTypes
            .AsNoTracking()
            .Where(c => c.IsIngredient && c.IsActive)
            .Select(c => new { c.Id, c.Name, UnitName = c.Unit != null ? c.Unit.Name : null })
            .ToListAsync();

        if (ingredients.Count == 0)
            return Array.Empty<IngredientPriceRecommendationDto>();

        var ingredientIds = ingredients.Select(i => i.Id).ToList();

        var foodAvgs = await (
                from fi in db.FoodIngredients.AsNoTracking()
                join f in db.FoodGenerations.AsNoTracking() on fi.FoodGenerationId equals f.Id
                where ingredientIds.Contains(fi.CostTypeId)
                group fi by fi.CostTypeId into g
                select new
                {
                    CostTypeId = g.Key,
                    Avg = g.Average(fi => fi.Price / (fi.Units == 0 ? 1m : fi.Units)),
                    Count = g.Count()
                })
            .ToListAsync();

        var costAvgs = await db.CostTransactions
            .AsNoTracking()
            .Where(ct => ingredientIds.Contains(ct.CostTypeId))
            .GroupBy(ct => ct.CostTypeId)
            .Select(g => new
            {
                CostTypeId = g.Key,
                Avg = g.Average(ct => ct.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        var foodMap = foodAvgs.ToDictionary(x => x.CostTypeId);
        var costMap = costAvgs.ToDictionary(x => x.CostTypeId);

        return ingredients.Select(ing =>
        {
            foodMap.TryGetValue(ing.Id, out var food);
            costMap.TryGetValue(ing.Id, out var cost);

            var totalCount = (food?.Count ?? 0) + (cost?.Count ?? 0);
            decimal avg = 0;
            if (totalCount > 0)
            {
                var weighted =
                    (food?.Avg ?? 0) * (food?.Count ?? 0) +
                    (cost?.Avg ?? 0) * (cost?.Count ?? 0);
                avg = weighted / totalCount;
            }

            return new IngredientPriceRecommendationDto(
                ing.Id, ing.Name, ing.UnitName, Math.Round(avg, 2));
        }).ToList();
    }

    private static FoodGenerationDto Map(FoodGeneration f) => new(
        f.Id, f.Name, f.CookDate, f.TotalCount, f.TotalCost, f.CostPerUnit, f.Description,
        f.Ingredients.Select(i => new FoodIngredientDto(
            i.Id, i.CostTypeId, i.CostType.Name, i.CostType.Unit?.Name,
            i.Units, i.Price, i.RecommendedPrice)).ToList(),
        AuditHelper.ToDto(f));
}
