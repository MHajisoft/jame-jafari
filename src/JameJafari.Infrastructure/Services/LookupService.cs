using System.Globalization;
using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class LookupService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<LookupItemResponse>> GetAccountsAsync(bool activeOnly = true) =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupAccounts(activeOnly),
            async _ =>
            {
                var query = db.Accounts.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(a => a.IsActive);
                return await query
                    .OrderBy(a => a.Name)
                    .Select(a => new LookupItemResponse(a.Id, a.Name))
                    .ToListAsync();
            },
            options => options.SetDuration(LookupCache.AccountsDuration));

    public async Task<IReadOnlyList<CostTypeLookupItemResponse>> GetCostTypesAsync(
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
                    .Select(c => new CostTypeLookupItemResponse(
                        c.Id,
                        c.Name,
                        c.Unit != null ? c.Unit.Name : null))
                    .ToListAsync();
            },
            options => options.SetDuration(LookupCache.CostTypesDuration));

    public async Task<IReadOnlyList<LookupItemResponse>> GetGeneralTypesAsync(GeneralTypeCategory category) =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupGeneralTypes(category),
            async _ => await db.GeneralTypes.AsNoTracking()
                .Where(g => g.Category == category && g.IsActive)
                .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
                .Select(g => new LookupItemResponse(g.Id, g.Name))
                .ToListAsync(),
            options => options.SetDuration(LookupCache.GeneralTypesDuration));

    public async Task<PagedResult<PersonLookupItemResponse>> SearchPersonsAsync(
        string? search,
        Gender? gender,
        int page,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(search))
            return new PagedResult<PersonLookupItemResponse>([], 0, page, pageSize);

        var tokens = Tokenize(search);
        if (tokens.Length == 0)
            return new PagedResult<PersonLookupItemResponse>([], 0, page, pageSize);

        var all = await GetPersonLookupCacheAsync();
        IEnumerable<PersonLookupCacheItem> filtered = all;
        if (gender.HasValue)
            filtered = filtered.Where(p => p.Gender == gender.Value);

        filtered = filtered.Where(p => tokens.All(t => p.SearchBlob.Contains(t, StringComparison.Ordinal)));

        var matched = filtered
            .OrderBy(p => p.FirstName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.LastName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = matched.Count;
        var items = matched
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PersonLookupItemResponse
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                NickName = p.NickName,
                Gender = p.Gender,
                PicturePath = p.PicturePath,
                FatherName = p.FatherName,
                MotherName = p.MotherName,
                FatherFirstName = p.FatherFirstName,
                MotherFirstName = p.MotherFirstName,
                IsDead = p.IsDead
            })
            .ToList();

        return new PagedResult<PersonLookupItemResponse>(items, total, page, pageSize);
    }

    private async Task<IReadOnlyList<PersonLookupCacheItem>> GetPersonLookupCacheAsync() =>
        await cache.GetOrSetAsync(
            CacheKeys.LookupPersons,
            async _ =>
            {
                var rows = await db.Persons.AsNoTracking()
                    .Select(p => new
                    {
                        p.Id,
                        p.FirstName,
                        p.LastName,
                        p.NickName,
                        p.Gender,
                        p.PicturePath,
                        p.IsDead,
                        FatherFirst = p.Father != null ? p.Father.FirstName : null,
                        FatherLast = p.Father != null ? p.Father.LastName : null,
                        FatherNick = p.Father != null ? p.Father.NickName : null,
                        FatherPrefix = p.Father != null && p.Father.NamePrefix != null ? p.Father.NamePrefix.Name : null,
                        MotherFirst = p.Mother != null ? p.Mother.FirstName : null,
                        MotherLast = p.Mother != null ? p.Mother.LastName : null,
                        MotherNick = p.Mother != null ? p.Mother.NickName : null,
                        MotherPrefix = p.Mother != null && p.Mother.NamePrefix != null ? p.Mother.NamePrefix.Name : null
                    })
                    .ToListAsync();

                return rows.Select(r =>
                {
                    var fatherName = PersonDisplayNameHelper.FormatOrNull(r.FatherFirst, r.FatherLast, r.FatherPrefix);
                    var motherName = PersonDisplayNameHelper.FormatOrNull(r.MotherFirst, r.MotherLast, r.MotherPrefix);
                    var blob = BuildSearchBlob(
                        r.FirstName, r.LastName, r.NickName,
                        r.FatherFirst, r.FatherLast, r.FatherNick,
                        r.MotherFirst, r.MotherLast, r.MotherNick);
                    return new PersonLookupCacheItem(
                        r.Id, r.FirstName, r.LastName, r.NickName, r.Gender, r.PicturePath, r.IsDead,
                        fatherName, motherName, r.FatherFirst, r.MotherFirst, blob);
                }).ToList();
            },
            options => options.SetDuration(LookupCache.PersonsDuration));

    private static string[] Tokenize(string search) =>
        search
            .Trim()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string BuildSearchBlob(
        string first, string? last, string? nick,
        string? fatherFirst, string? fatherLast, string? fatherNick,
        string? motherFirst, string? motherLast, string? motherNick)
    {
        var parts = new[]
        {
            first, last, nick,
            fatherFirst, fatherLast, fatherNick,
            motherFirst, motherLast, motherNick
        };
        return Normalize(string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x))));
    }

    private static string Normalize(string value)
    {
        var s = value.Trim().ToLower(CultureInfo.InvariantCulture);
        s = s.Replace((char)0x064A, (char)0x06CC).Replace((char)0x0643, (char)0x06A9);
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s;
    }

    private sealed record PersonLookupCacheItem(
        int Id,
        string FirstName,
        string? LastName,
        string? NickName,
        Gender Gender,
        string? PicturePath,
        bool IsDead,
        string? FatherName,
        string? MotherName,
        string? FatherFirstName,
        string? MotherFirstName,
        string SearchBlob);
}
