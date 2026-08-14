using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class GeneralTypeService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<GeneralTypeDto>> GetByCategoryAsync(GeneralTypeCategory category, bool includeInactive = false)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.GeneralTypes(category, includeInactive),
            async _ =>
            {
                var query = db.GeneralTypes.AsNoTracking().Where(g => g.Category == category);
                if (!includeInactive)
                    query = query.Where(g => g.IsActive);
                return await Project(query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name))
                    .ToListAsync();
            },
            options => options.SetDuration(LookupCache.GeneralTypesDuration));
    }

    public async Task<GeneralTypeDto?> GetByIdAsync(int id) =>
        await Project(db.GeneralTypes.AsNoTracking().Where(g => g.Id == id))
            .FirstOrDefaultAsync();

    public async Task<GeneralTypeDto> CreateAsync(CreateGeneralTypeRequest request, int userId)
    {
        if (!Enum.TryParse<GeneralTypeCategory>(request.Category, true, out var category))
            throw new ArgumentException("Invalid category");

        var entity = new GeneralType
        {
            Name = request.Name,
            Code = request.Code,
            Category = category,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.GeneralTypes.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateGeneralTypesAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<GeneralTypeDto?> UpdateAsync(int id, UpdateGeneralTypeRequest request, int userId)
    {
        var entity = await db.GeneralTypes.FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateGeneralTypesAsync(cache);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.GeneralTypes.FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateGeneralTypesAsync(cache);
        return true;
    }

    private static IQueryable<GeneralTypeDto> Project(IQueryable<GeneralType> query) =>
        query.Select(g => new GeneralTypeDto(
            g.Id,
            g.Name,
            g.Code,
            g.Category.ToString(),
            g.SortOrder,
            g.IsActive,
            new AuditInfoDto(
                g.CreatedAt,
                g.CreatedBy != null ? g.CreatedBy.Username : null,
                g.CreatedBy != null ? g.CreatedBy.AvatarPath : null,
                g.UpdatedAt,
                g.UpdatedBy != null ? g.UpdatedBy.Username : null,
                g.UpdatedBy != null ? g.UpdatedBy.AvatarPath : null)));
}
