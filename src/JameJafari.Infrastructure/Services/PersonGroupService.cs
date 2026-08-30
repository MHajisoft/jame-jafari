using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class PersonGroupService(AppDbContext db)
{
    public async Task<IReadOnlyList<PersonGroupResponse>> GetAllAsync(bool activeOnly = false)
    {
        var query = db.PersonGroups.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(g => g.IsActive);
        var rows = await ProjectRows(query.OrderBy(g => g.Name)).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task<PagedResult<PersonGroupResponse>> GetPagedAsync(bool activeOnly, int page, int pageSize)
    {
        var query = db.PersonGroups.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(g => g.IsActive);

        var total = await query.CountAsync();
        var rows = await ProjectRows(
                query.OrderBy(g => g.Name).Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<PersonGroupResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<PersonGroupResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.PersonGroups.AsNoTracking().Where(g => g.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<PersonGroupResponse> CreateAsync(CreatePersonGroupRequest request, int userId)
    {
        var personIds = NormalizePersonIds(request.PersonIds);
        await EnsurePersonsExistAsync(personIds);

        var entity = new PersonGroup
        {
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.PersonGroups.Add(entity);
        await db.SaveChangesAsync();

        await ReplaceMembersAsync(entity.Id, personIds);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PersonGroupResponse?> UpdateAsync(int id, UpdatePersonGroupRequest request, int userId)
    {
        var entity = await db.PersonGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null) return null;

        var personIds = NormalizePersonIds(request.PersonIds);
        await EnsurePersonsExistAsync(personIds);

        entity.Name = request.Name.Trim();
        entity.Description = Normalize(request.Description);
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();

        await ReplaceMembersAsync(id, personIds);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.PersonGroups.FirstOrDefaultAsync(g => g.Id == id);
        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<PersonGroupLookupItemResponse>> GetLookupAsync(bool activeOnly = true)
    {
        var query = db.PersonGroups.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(g => g.IsActive);

        return await query
            .OrderBy(g => g.Name)
            .Select(g => new PersonGroupLookupItemResponse(
                g.Id,
                g.Name,
                g.Members.Count))
            .ToListAsync();
    }

    async Task ReplaceMembersAsync(int groupId, IReadOnlyList<int> personIds)
    {
        var existing = await db.PersonGroupMembers.Where(m => m.PersonGroupId == groupId).ToListAsync();
        db.PersonGroupMembers.RemoveRange(existing);
        db.PersonGroupMembers.AddRange(personIds.Select(personId => new PersonGroupMember
        {
            PersonGroupId = groupId,
            PersonId = personId
        }));
        await db.SaveChangesAsync();
    }

    async Task EnsurePersonsExistAsync(IReadOnlyList<int> personIds)
    {
        if (personIds.Count == 0) return;

        var found = await db.Persons.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();

        if (found.Count != personIds.Count)
            throw new InvalidOperationException("برخی اشخاص انتخاب‌شده یافت نشدند");
    }

    static IReadOnlyList<int> NormalizePersonIds(IReadOnlyList<int>? personIds) =>
        personIds?.Where(id => id > 0).Distinct().ToList() ?? [];

    static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IQueryable<PersonGroupRow> ProjectRows(IQueryable<PersonGroup> query) =>
        query.Select(g => new PersonGroupRow
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            IsActive = g.IsActive,
            MemberCount = g.Members.Count,
            PersonIds = g.Members.Select(m => m.PersonId).ToList(),
            CreatedAt = g.CreatedAt,
            CreatedByUsername = g.CreatedBy != null ? g.CreatedBy.Username : null,
            CreatedByAvatarPath = g.CreatedBy != null ? g.CreatedBy.AvatarPath : null,
            UpdatedAt = g.UpdatedAt,
            UpdatedByUsername = g.UpdatedBy != null ? g.UpdatedBy.Username : null,
            UpdatedByAvatarPath = g.UpdatedBy != null ? g.UpdatedBy.AvatarPath : null
        });

    private static PersonGroupResponse ToDto(PersonGroupRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        IsActive = row.IsActive,
        MemberCount = row.MemberCount,
        PersonIds = row.PersonIds,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class PersonGroupRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public int MemberCount { get; init; }
        public List<int> PersonIds { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
