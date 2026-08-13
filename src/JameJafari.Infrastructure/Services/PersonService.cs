using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class PersonService(AppDbContext db, IFusionCache cache)
{
    public async Task<PagedResult<PersonDto>> GetPagedAsync(string? search, Gender? gender, int page, int pageSize)
    {
        var filter = db.Persons.AsNoTracking().AsQueryable();

        if (gender.HasValue)
            filter = filter.Where(p => p.Gender == gender.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filter = filter.Where(p =>
                p.FirstName.Contains(s) ||
                (p.LastName != null && p.LastName.Contains(s)) ||
                (p.NickName != null && p.NickName.Contains(s)) ||
                (p.Mobile != null && p.Mobile.Contains(s)));
        }

        var total = await filter.CountAsync();
        var items = await Project(
                filter.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return new PagedResult<PersonDto>(items, total, page, pageSize);
    }

    public async Task<PersonDto?> GetByIdAsync(int id)
    {
        return await Project(db.Persons.AsNoTracking().Where(p => p.Id == id))
            .FirstOrDefaultAsync();
    }

    public async Task<PersonDto> CreateAsync(CreatePersonRequest request, int userId)
    {
        var entity = new Person
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            NickName = request.NickName,
            Gender = request.Gender,
            FatherId = request.FatherId,
            MotherId = request.MotherId,
            Mobile = request.Mobile,
            Address = request.Address,
            NamePrefixId = request.NamePrefixId,
            IsDead = request.IsDead,
            CreatedById = userId
        };
        db.Persons.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PersonDto?> UpdateAsync(int id, UpdatePersonRequest request, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.NickName = request.NickName;
        entity.Gender = request.Gender;
        entity.FatherId = request.FatherId;
        entity.MotherId = request.MotherId;
        entity.Mobile = request.Mobile;
        entity.Address = request.Address;
        entity.NamePrefixId = request.NamePrefixId;
        entity.IsDead = request.IsDead;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return true;
    }

    public async Task<PersonDto?> UpdatePictureAsync(int id, string? path, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;
        entity.PicturePath = path;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return await GetByIdAsync(id);
    }

    public static string GetDisplayName(Person p)
    {
        var prefix = p.NamePrefix?.Name;
        var name = string.Join(" ", new[] { p.FirstName, p.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix} {name}";
    }

    private static IQueryable<PersonDto> Project(IQueryable<Person> query) =>
        query.Select(p => new PersonDto(
            p.Id,
            p.FirstName,
            p.LastName,
            p.NickName,
            p.Gender,
            p.FatherId,
            p.Father == null
                ? null
                : (p.Father.NamePrefix != null ? p.Father.NamePrefix.Name + " " : "")
                  + p.Father.FirstName
                  + (p.Father.LastName != null ? " " + p.Father.LastName : ""),
            p.MotherId,
            p.Mother == null
                ? null
                : (p.Mother.NamePrefix != null ? p.Mother.NamePrefix.Name + " " : "")
                  + p.Mother.FirstName
                  + (p.Mother.LastName != null ? " " + p.Mother.LastName : ""),
            p.Father != null ? p.Father.FirstName : null,
            p.Mother != null ? p.Mother.FirstName : null,
            p.PicturePath,
            p.Mobile,
            p.Address,
            p.NamePrefixId,
            p.NamePrefix != null ? p.NamePrefix.Name : null,
            p.IsDead,
            (p.NamePrefix != null ? p.NamePrefix.Name + " " : "")
            + p.FirstName
            + (p.LastName != null ? " " + p.LastName : ""),
            new AuditInfoDto(
                p.CreatedAt,
                p.CreatedBy != null ? p.CreatedBy.Username : null,
                p.UpdatedAt,
                p.UpdatedBy != null ? p.UpdatedBy.Username : null)));
}
