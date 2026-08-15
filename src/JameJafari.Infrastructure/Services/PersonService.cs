using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class PersonService(AppDbContext db, IFusionCache cache)
{
    public async Task<PagedResult<PersonResponse>> GetPagedAsync(string? search, Gender? gender, int page, int pageSize)
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
        var rows = await ProjectRows(
                filter.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        var items = rows.Select(ToDto).ToList();
        return new PagedResult<PersonResponse>(items, total, page, pageSize);
    }

    public async Task<PersonResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.Persons.AsNoTracking().Where(p => p.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<PersonResponse> CreateAsync(CreatePersonRequest request, int userId)
    {
        ValidateLifeStatus(request.IsDead, request.DeathDate);

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
            DeathDate = request.IsDead ? ToDateOnly(request.DeathDate) : null,
            CreatedById = userId
        };
        db.Persons.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PersonResponse?> UpdateAsync(int id, UpdatePersonRequest request, int userId)
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
        ValidateLifeStatus(request.IsDead, request.DeathDate);
        entity.IsDead = request.IsDead;
        entity.DeathDate = request.IsDead ? ToDateOnly(request.DeathDate) : null;
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

    public async Task<PersonResponse?> UpdatePictureAsync(int id, string? path, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id);
        if (entity is null) return null;
        entity.PicturePath = path;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidatePersonsAsync(cache);
        return await GetByIdAsync(id);
    }

    public static string GetDisplayName(Person p) =>
        PersonDisplayNameHelper.Format(p.FirstName, p.LastName, p.NamePrefix?.Name);

    static void ValidateLifeStatus(bool isDead, DateTime? deathDate)
    {
        if (isDead && deathDate is null)
            throw new InvalidOperationException("تاریخ وفات الزامی است");
        if (!isDead && deathDate is not null)
            throw new InvalidOperationException("تاریخ وفات فقط برای اشخاص درگذشته مجاز است");
    }

    static DateTime? ToDateOnly(DateTime? value) =>
        value is null ? null : value.Value.Date;

    private static IQueryable<PersonRow> ProjectRows(IQueryable<Person> query) =>
        query.Select(p => new PersonRow
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            NickName = p.NickName,
            Gender = p.Gender,
            FatherId = p.FatherId,
            MotherId = p.MotherId,
            FatherFirstName = p.Father != null ? p.Father.FirstName : null,
            FatherLastName = p.Father != null ? p.Father.LastName : null,
            FatherNickName = p.Father != null ? p.Father.NickName : null,
            FatherPicturePath = p.Father != null ? p.Father.PicturePath : null,
            FatherIsDead = p.Father != null && p.Father.IsDead,
            FatherNamePrefixName = p.Father != null && p.Father.NamePrefix != null ? p.Father.NamePrefix.Name : null,
            MotherFirstName = p.Mother != null ? p.Mother.FirstName : null,
            MotherLastName = p.Mother != null ? p.Mother.LastName : null,
            MotherNickName = p.Mother != null ? p.Mother.NickName : null,
            MotherPicturePath = p.Mother != null ? p.Mother.PicturePath : null,
            MotherIsDead = p.Mother != null && p.Mother.IsDead,
            MotherNamePrefixName = p.Mother != null && p.Mother.NamePrefix != null ? p.Mother.NamePrefix.Name : null,
            PicturePath = p.PicturePath,
            Mobile = p.Mobile,
            Address = p.Address,
            NamePrefixId = p.NamePrefixId,
            NamePrefixName = p.NamePrefix != null ? p.NamePrefix.Name : null,
            IsDead = p.IsDead,
            DeathDate = p.DeathDate,
            CreatedAt = p.CreatedAt,
            CreatedByUsername = p.CreatedBy != null ? p.CreatedBy.Username : null,
            CreatedByAvatarPath = p.CreatedBy != null ? p.CreatedBy.AvatarPath : null,
            UpdatedAt = p.UpdatedAt,
            UpdatedByUsername = p.UpdatedBy != null ? p.UpdatedBy.Username : null,
            UpdatedByAvatarPath = p.UpdatedBy != null ? p.UpdatedBy.AvatarPath : null
        });

    private static PersonResponse ToDto(PersonRow row) => new()
    {
        Id = row.Id,
        FirstName = row.FirstName,
        LastName = row.LastName,
        NickName = row.NickName,
        Gender = row.Gender,
        FatherId = row.FatherId,
        FatherName = PersonDisplayNameHelper.FormatOrNull(row.FatherFirstName, row.FatherLastName, row.FatherNamePrefixName),
        MotherId = row.MotherId,
        MotherName = PersonDisplayNameHelper.FormatOrNull(row.MotherFirstName, row.MotherLastName, row.MotherNamePrefixName),
        FatherFirstName = row.FatherFirstName,
        MotherFirstName = row.MotherFirstName,
        PicturePath = row.PicturePath,
        Mobile = row.Mobile,
        Address = row.Address,
        NamePrefixId = row.NamePrefixId,
        NamePrefixName = row.NamePrefixName,
        IsDead = row.IsDead,
        DeathDate = row.DeathDate,
        DisplayName = PersonDisplayNameHelper.Format(row.FirstName, row.LastName, row.NamePrefixName),
        FatherSummary = ToParentSummary(row.FatherId, row.FatherFirstName, row.FatherLastName, row.FatherNamePrefixName,
            row.FatherNickName, row.FatherPicturePath, row.FatherIsDead),
        MotherSummary = ToParentSummary(row.MotherId, row.MotherFirstName, row.MotherLastName, row.MotherNamePrefixName,
            row.MotherNickName, row.MotherPicturePath, row.MotherIsDead),
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private static PersonSummaryResponse? ToParentSummary(
        int? id,
        string? firstName,
        string? lastName,
        string? namePrefixName,
        string? nickName,
        string? picturePath,
        bool isDead)
    {
        if (id is null) return null;
        return new PersonSummaryResponse
        {
            Id = id.Value,
            DisplayName = PersonDisplayNameHelper.Format(firstName, lastName, namePrefixName),
            NickName = nickName,
            PicturePath = picturePath,
            IsDead = isDead
        };
    }

    private sealed class PersonRow
    {
        public int Id { get; init; }
        public string FirstName { get; init; } = "";
        public string? LastName { get; init; }
        public string? NickName { get; init; }
        public Gender Gender { get; init; }
        public int? FatherId { get; init; }
        public int? MotherId { get; init; }
        public string? FatherFirstName { get; init; }
        public string? FatherLastName { get; init; }
        public string? FatherNickName { get; init; }
        public string? FatherPicturePath { get; init; }
        public bool FatherIsDead { get; init; }
        public string? FatherNamePrefixName { get; init; }
        public string? MotherFirstName { get; init; }
        public string? MotherLastName { get; init; }
        public string? MotherNickName { get; init; }
        public string? MotherPicturePath { get; init; }
        public bool MotherIsDead { get; init; }
        public string? MotherNamePrefixName { get; init; }
        public string? PicturePath { get; init; }
        public string? Mobile { get; init; }
        public string? Address { get; init; }
        public int? NamePrefixId { get; init; }
        public string? NamePrefixName { get; init; }
        public bool IsDead { get; init; }
        public DateTime? DeathDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
