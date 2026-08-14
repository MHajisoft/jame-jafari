using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class CostTypeService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<CostTypeDto>> GetAllAsync(bool? isIngredient = null, bool activeOnly = true)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.CostTypes(isIngredient, activeOnly),
            async _ =>
            {
                var query = db.CostTypes.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(c => c.IsActive);
                if (isIngredient.HasValue) query = query.Where(c => c.IsIngredient == isIngredient.Value);
                return await Project(query.OrderBy(c => c.Name)).ToListAsync();
            },
            options => options.SetDuration(LookupCache.CostTypesDuration));
    }

    public async Task<CostTypeDto?> GetByIdAsync(int id)
    {
        return await Project(db.CostTypes.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync();
    }

    public async Task<CostTypeDto> CreateAsync(CreateCostTypeRequest request, int userId)
    {
        var entity = new CostType
        {
            Name = request.Name,
            Description = request.Description,
            IsIngredient = request.IsIngredient,
            UnitId = request.UnitId,
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.CostTypes.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateCostTypesAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<CostTypeDto?> UpdateAsync(int id, UpdateCostTypeRequest request, int userId)
    {
        var entity = await db.CostTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsIngredient = request.IsIngredient;
        entity.UnitId = request.UnitId;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateCostTypesAsync(cache);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.CostTypes.FirstOrDefaultAsync(c => c.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateCostTypesAsync(cache);
        return true;
    }

    private static IQueryable<CostTypeDto> Project(IQueryable<CostType> query) =>
        query.Select(c => new CostTypeDto(
            c.Id,
            c.Name,
            c.Description,
            c.IsIngredient,
            c.UnitId,
            c.Unit != null ? c.Unit.Name : null,
            c.IsActive,
            new AuditInfoDto(
                c.CreatedAt,
                c.CreatedBy != null ? c.CreatedBy.Username : null,
                c.CreatedBy != null ? c.CreatedBy.AvatarPath : null,
                c.UpdatedAt,
                c.UpdatedBy != null ? c.UpdatedBy.Username : null,
                c.UpdatedBy != null ? c.UpdatedBy.AvatarPath : null)));
}
