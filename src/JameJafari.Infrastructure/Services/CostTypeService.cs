using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class CostTypeService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<CostTypeResponse>> GetAllAsync(bool? isIngredient = null, bool activeOnly = true)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.CostTypes(isIngredient, activeOnly),
            async _ =>
            {
                var query = db.CostTypes.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(c => c.IsActive);
                if (isIngredient.HasValue) query = query.Where(c => c.IsIngredient == isIngredient.Value);
                var rows = await ProjectRows(query.OrderBy(c => c.Name)).ToListAsync();
                return (IReadOnlyList<CostTypeResponse>)rows.Select(ToDto).ToList();
            },
            options => options.SetDuration(LookupCache.CostTypesDuration));
    }

    public async Task<CostTypeResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.CostTypes.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<CostTypeResponse> CreateAsync(CreateCostTypeRequest request, int userId)
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

    public async Task<CostTypeResponse?> UpdateAsync(int id, UpdateCostTypeRequest request, int userId)
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

    private static IQueryable<CostTypeRow> ProjectRows(IQueryable<CostType> query) =>
        query.Select(c => new CostTypeRow
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsIngredient = c.IsIngredient,
            UnitId = c.UnitId,
            UnitName = c.Unit != null ? c.Unit.Name : null,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            CreatedByUsername = c.CreatedBy != null ? c.CreatedBy.Username : null,
            CreatedByAvatarPath = c.CreatedBy != null ? c.CreatedBy.AvatarPath : null,
            UpdatedAt = c.UpdatedAt,
            UpdatedByUsername = c.UpdatedBy != null ? c.UpdatedBy.Username : null,
            UpdatedByAvatarPath = c.UpdatedBy != null ? c.UpdatedBy.AvatarPath : null
        });

    private static CostTypeResponse ToDto(CostTypeRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        IsIngredient = row.IsIngredient,
        UnitId = row.UnitId,
        UnitName = row.UnitName,
        IsActive = row.IsActive,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class CostTypeRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public bool IsIngredient { get; init; }
        public int? UnitId { get; init; }
        public string? UnitName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
