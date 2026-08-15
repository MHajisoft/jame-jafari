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
    public async Task<IReadOnlyList<GeneralTypeResponse>> GetByCategoryAsync(GeneralTypeCategory category, bool includeInactive = false)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.GeneralTypes(category, includeInactive),
            async _ =>
            {
                var query = FilterByCategory(category, includeInactive);
                var rows = await ProjectRows(query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name))
                    .ToListAsync();
                return (IReadOnlyList<GeneralTypeResponse>)rows.Select(ToDto).ToList();
            },
            options => options.SetDuration(LookupCache.GeneralTypesDuration));
    }

    public async Task<PagedResult<GeneralTypeResponse>> GetPagedByCategoryAsync(
        GeneralTypeCategory category, bool includeInactive, int page, int pageSize)
    {
        var query = FilterByCategory(category, includeInactive);
        var total = await query.CountAsync();
        var rows = await ProjectRows(
                query.OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<GeneralTypeResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    static IQueryable<GeneralType> FilterByCategory(AppDbContext db, GeneralTypeCategory category, bool includeInactive)
    {
        var query = db.GeneralTypes.AsNoTracking().Where(g => g.Category == category);
        if (!includeInactive)
            query = query.Where(g => g.IsActive);
        return query;
    }

    IQueryable<GeneralType> FilterByCategory(GeneralTypeCategory category, bool includeInactive) =>
        FilterByCategory(db, category, includeInactive);

    public async Task<GeneralTypeResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.GeneralTypes.AsNoTracking().Where(g => g.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<GeneralTypeResponse> CreateAsync(CreateGeneralTypeRequest request, int userId)
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

    public async Task<GeneralTypeResponse?> UpdateAsync(int id, UpdateGeneralTypeRequest request, int userId)
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

    private static IQueryable<GeneralTypeRow> ProjectRows(IQueryable<GeneralType> query) =>
        query.Select(g => new GeneralTypeRow
        {
            Id = g.Id,
            Name = g.Name,
            Code = g.Code,
            Category = g.Category,
            SortOrder = g.SortOrder,
            IsActive = g.IsActive,
            CreatedAt = g.CreatedAt,
            CreatedByUsername = g.CreatedBy != null ? g.CreatedBy.Username : null,
            CreatedByAvatarPath = g.CreatedBy != null ? g.CreatedBy.AvatarPath : null,
            UpdatedAt = g.UpdatedAt,
            UpdatedByUsername = g.UpdatedBy != null ? g.UpdatedBy.Username : null,
            UpdatedByAvatarPath = g.UpdatedBy != null ? g.UpdatedBy.AvatarPath : null
        });

    private static GeneralTypeResponse ToDto(GeneralTypeRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Code = row.Code,
        Category = row.Category.ToString(),
        SortOrder = row.SortOrder,
        IsActive = row.IsActive,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class GeneralTypeRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string? Code { get; init; }
        public GeneralTypeCategory Category { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
