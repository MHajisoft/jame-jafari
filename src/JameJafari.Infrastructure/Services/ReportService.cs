using JameJafari.Core.DTOs;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class ReportService(AppDbContext db)
{
    public async Task<IReadOnlyList<AccountBalanceReportDto>> GetAccountBalancesAsync(DateTime? from, DateTime? to)
    {
        var accounts = await db.Accounts
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new { a.Id, a.Name })
            .ToListAsync();

        var incomeQuery = db.IncomeTransactions.AsNoTracking().AsQueryable();
        var costQuery = db.CostTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.TransactionDate >= from.Value);
            costQuery = costQuery.Where(t => t.TransactionDate >= from.Value);
        }
        if (to.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.TransactionDate <= to.Value);
            costQuery = costQuery.Where(t => t.TransactionDate <= to.Value);
        }

        var incomes = await incomeQuery
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total);

        var costs = await costQuery
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total);

        return accounts.Select(a =>
        {
            incomes.TryGetValue(a.Id, out var income);
            costs.TryGetValue(a.Id, out var cost);
            return new AccountBalanceReportDto(a.Id, a.Name, income, cost, income - cost);
        }).ToList();
    }

    public async Task<IReadOnlyList<CostTypeReportDto>> GetCostTypeReportAsync(DateTime? from, DateTime? to)
    {
        var costTypes = await db.CostTypes
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        var incomeQuery = db.IncomeTransactions.AsNoTracking().AsQueryable();
        var costQuery = db.CostTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.TransactionDate >= from.Value);
            costQuery = costQuery.Where(t => t.TransactionDate >= from.Value);
        }
        if (to.HasValue)
        {
            incomeQuery = incomeQuery.Where(t => t.TransactionDate <= to.Value);
            costQuery = costQuery.Where(t => t.TransactionDate <= to.Value);
        }

        var incomes = await incomeQuery
            .GroupBy(t => t.CostTypeId)
            .Select(g => new { CostTypeId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CostTypeId, x => x.Total);

        var costs = await costQuery
            .GroupBy(t => t.CostTypeId)
            .Select(g => new { CostTypeId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CostTypeId, x => x.Total);

        return costTypes
            .Select(ct =>
            {
                incomes.TryGetValue(ct.Id, out var income);
                costs.TryGetValue(ct.Id, out var cost);
                return new CostTypeReportDto(ct.Id, ct.Name, income, cost, income - cost);
            })
            .OrderByDescending(r => r.TotalCost + r.TotalIncome)
            .ToList();
    }

    public async Task<DateRangeReportDto> GetSummaryAsync(DateTime from, DateTime to)
    {
        var income = await db.IncomeTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .SumAsync(t => t.Amount);
        var cost = await db.CostTransactions
            .AsNoTracking()
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .SumAsync(t => t.Amount);
        return new DateRangeReportDto(from, to, income, cost, income - cost);
    }

    public async Task<IReadOnlyList<PersonIncomeReportDto>> GetPersonIncomeReportAsync(DateTime? from, DateTime? to)
    {
        var query = db.IncomeTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value);

        var aggregates = await query
            .GroupBy(t => t.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToListAsync();

        if (aggregates.Count == 0)
            return Array.Empty<PersonIncomeReportDto>();

        var personIds = aggregates.Select(a => a.PersonId).ToList();
        var people = await db.Persons
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FirstName, p.LastName, p.NickName })
            .ToDictionaryAsync(p => p.Id);

        return aggregates
            .Select(a =>
            {
                people.TryGetValue(a.PersonId, out var person);
                var name = person is null
                    ? ""
                    : ((person.FirstName ?? "") + " " + (person.LastName ?? "")).Trim();
                return new PersonIncomeReportDto(
                    a.PersonId,
                    name,
                    person?.NickName,
                    a.TotalAmount,
                    a.TransactionCount);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<FoodCostReportDto>> GetFoodCostReportAsync(DateTime? from, DateTime? to)
    {
        var query = db.FoodGenerations.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(f => f.CookDate >= from.Value);
        if (to.HasValue) query = query.Where(f => f.CookDate <= to.Value);

        return await query.OrderByDescending(f => f.CookDate)
            .Select(f => new FoodCostReportDto(f.Id, f.Name, f.CookDate, f.TotalCount, f.CostPerUnit, f.TotalCost))
            .ToListAsync();
    }

    public async Task<DeathAnniversaryReportDto> GetDeathAnniversaryReportAsync(
        DeathAnniversaryScope scope,
        DateTime? referenceDate = null)
    {
        var refDate = (referenceDate ?? DateTime.UtcNow).Date;
        var reference = JalaliCalendarHelper.ToParts(refDate);

        var rows = await db.Persons
            .AsNoTracking()
            .Where(p => p.IsDead && p.DeathDate != null)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.NickName,
                p.PicturePath,
                DeathDate = p.DeathDate!.Value,
                Prefix = p.NamePrefix != null ? p.NamePrefix.Name : null
            })
            .ToListAsync();

        var items = rows
            .Select(p =>
            {
                var death = JalaliCalendarHelper.ToParts(p.DeathDate);
                return (Person: p, Death: death);
            })
            .Where(x => JalaliCalendarHelper.MatchesDeathAnniversary(scope, x.Death, reference, refDate))
            .Select(x =>
            {
                var p = x.Person;
                var death = x.Death;
                var prefix = p.Prefix;
                var name = string.Join(" ", new[] { p.FirstName, p.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var displayName = string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix} {name}";
                return new DeathAnniversaryPersonDto(
                    p.Id,
                    displayName,
                    p.NickName,
                    p.PicturePath,
                    p.DeathDate.Date,
                    death.Year,
                    death.Month,
                    death.Day,
                    Math.Max(0, reference.Year - death.Year));
            })
            .OrderBy(x => x.JalaliDeathMonth)
            .ThenBy(x => x.JalaliDeathDay)
            .ThenBy(x => x.DisplayName)
            .ToList();

        return new DeathAnniversaryReportDto(
            scope.ToString(),
            refDate,
            reference.Year,
            reference.Month,
            reference.Day,
            JalaliCalendarHelper.Season(reference.Month),
            BuildScopeLabelFa(scope, reference, refDate),
            items);
    }

    static string BuildScopeLabelFa(DeathAnniversaryScope scope, JalaliParts reference, DateTime refDate)
    {
        var month = JalaliCalendarHelper.MonthNameFa(reference.Month);
        return scope switch
        {
            DeathAnniversaryScope.Day => $"{reference.Day} {month} {reference.Year}",
            DeathAnniversaryScope.Week => JalaliCalendarHelper.WeekRangeLabelFa(refDate),
            DeathAnniversaryScope.Month => $"{month} {reference.Year}",
            DeathAnniversaryScope.Season =>
                $"{JalaliCalendarHelper.SeasonNameFa(JalaliCalendarHelper.Season(reference.Month))} {reference.Year}",
            _ => ""
        };
    }
}
