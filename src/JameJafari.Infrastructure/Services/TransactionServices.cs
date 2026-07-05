using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class TransactionService(AppDbContext db)
{
    public async Task<PagedResult<IncomeTransactionDto>> GetIncomePagedAsync(DateTime? from, DateTime? to, int? accountId, int page, int pageSize)
    {
        var query = db.IncomeTransactions
            .Include(t => t.Person).Include(t => t.Account).Include(t => t.CostType)
            .Include(t => t.CreatedBy).Include(t => t.UpdatedBy)
            .Where(t => !t.IsDeleted);

        if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<IncomeTransactionDto>(items.Select(MapIncome).ToList(), total, page, pageSize);
    }

    public async Task<IncomeTransactionDto> CreateIncomeAsync(CreateIncomeTransactionRequest request, int userId, string? documentPath)
    {
        var entity = new IncomeTransaction
        {
            PersonId = request.PersonId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            PaymentType = request.PaymentType,
            CostTypeId = request.CostTypeId,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            DocumentPath = documentPath,
            CreatedById = userId
        };
        db.IncomeTransactions.Add(entity);
        await db.SaveChangesAsync();
        return await GetIncomeByIdAsync(entity.Id) ?? throw new InvalidOperationException();
    }

    public async Task<bool> DeleteIncomeAsync(int id, int userId)
    {
        var entity = await db.IncomeTransactions.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<CostTransactionDto>> GetCostPagedAsync(DateTime? from, DateTime? to, int? accountId, int page, int pageSize)
    {
        var query = db.CostTransactions
            .Include(t => t.Account).Include(t => t.CostType)
            .Include(t => t.CreatedBy).Include(t => t.UpdatedBy)
            .Where(t => !t.IsDeleted);

        if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<CostTransactionDto>(items.Select(MapCost).ToList(), total, page, pageSize);
    }

    public async Task<CostTransactionDto> CreateCostAsync(CreateCostTransactionRequest request, int userId, string? documentPath)
    {
        var entity = new CostTransaction
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            CostTypeId = request.CostTypeId,
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            DocumentPath = documentPath,
            CreatedById = userId
        };
        db.CostTransactions.Add(entity);
        await db.SaveChangesAsync();
        return await GetCostByIdAsync(entity.Id) ?? throw new InvalidOperationException();
    }

    public async Task<bool> DeleteCostAsync(int id, int userId)
    {
        var entity = await db.CostTransactions.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<IncomeTransactionDto?> GetIncomeByIdAsync(int id)
    {
        var t = await db.IncomeTransactions
            .Include(x => x.Person).Include(x => x.Account).Include(x => x.CostType)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? null : MapIncome(t);
    }

    private async Task<CostTransactionDto?> GetCostByIdAsync(int id)
    {
        var t = await db.CostTransactions
            .Include(x => x.Account).Include(x => x.CostType)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? null : MapCost(t);
    }

    private static IncomeTransactionDto MapIncome(IncomeTransaction t) => new(
        t.Id, t.PersonId, PersonService.GetDisplayName(t.Person), t.AccountId, t.Account.Name,
        t.Amount, t.PaymentType, t.CostTypeId, t.CostType.Name,
        t.DocumentPath, t.Description, t.TransactionDate, AuditHelper.ToDto(t));

    private static CostTransactionDto MapCost(CostTransaction t) => new(
        t.Id, t.AccountId, t.Account.Name, t.Amount, t.CostTypeId, t.CostType.Name,
        t.DocumentPath, t.Description, t.TransactionDate, AuditHelper.ToDto(t));
}

public class FoodService(AppDbContext db)
{
    public async Task<IReadOnlyList<IngredientPriceRecommendationDto>> GetRecommendationsAsync()
    {
        var ingredientIds = await db.CostTypes.Where(c => !c.IsDeleted && c.IsIngredient && c.IsActive)
            .Select(c => c.Id).ToListAsync();

        var recommendations = new List<IngredientPriceRecommendationDto>();
        foreach (var costTypeId in ingredientIds)
        {
            var costType = await db.CostTypes.Include(c => c.Unit).FirstAsync(c => c.Id == costTypeId);

            var foodPrices = await db.FoodIngredients
                .Where(fi => fi.CostTypeId == costTypeId)
                .Select(fi => fi.Price / (fi.Units == 0 ? 1 : fi.Units))
                .ToListAsync();

            var costPrices = await db.CostTransactions
                .Where(ct => !ct.IsDeleted && ct.CostTypeId == costTypeId)
                .Select(ct => ct.Amount)
                .ToListAsync();

            var allPrices = foodPrices.Concat(costPrices).ToList();
            var avg = allPrices.Count > 0 ? allPrices.Average() : 0;

            recommendations.Add(new IngredientPriceRecommendationDto(
                costTypeId, costType.Name, costType.Unit?.Name, Math.Round(avg, 2)));
        }
        return recommendations;
    }

    public async Task<IReadOnlyList<FoodGenerationDto>> GetByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var items = await db.FoodGenerations
            .Include(f => f.Ingredients).ThenInclude(i => i.CostType).ThenInclude(c => c.Unit)
            .Include(f => f.CreatedBy).Include(f => f.UpdatedBy)
            .Where(f => !f.IsDeleted && f.CookDate >= start && f.CookDate < end)
            .OrderBy(f => f.Name).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<FoodGenerationDto> CreateAsync(CreateFoodGenerationRequest request, int userId)
    {
        var totalCost = request.Ingredients.Sum(i => i.Units * i.Price);
        var costPerUnit = request.TotalCount > 0 ? totalCost / request.TotalCount : 0;

        var recommendations = await GetRecommendationsAsync();
        var recDict = recommendations.ToDictionary(r => r.CostTypeId, r => r.RecommendedPrice);

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
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<FoodGenerationDto?> GetByIdAsync(int id)
    {
        var f = await db.FoodGenerations
            .Include(x => x.Ingredients).ThenInclude(i => i.CostType).ThenInclude(c => c.Unit)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return f is null ? null : Map(f);
    }

    private static FoodGenerationDto Map(FoodGeneration f) => new(
        f.Id, f.Name, f.CookDate, f.TotalCount, f.TotalCost, f.CostPerUnit, f.Description,
        f.Ingredients.Select(i => new FoodIngredientDto(
            i.Id, i.CostTypeId, i.CostType.Name, i.CostType.Unit?.Name,
            i.Units, i.Price, i.RecommendedPrice)).ToList(),
        AuditHelper.ToDto(f));
}

public class ReportService(AppDbContext db)
{
    public async Task<IReadOnlyList<AccountBalanceReportDto>> GetAccountBalancesAsync(DateTime? from, DateTime? to)
    {
        var accounts = await db.Accounts.Where(a => !a.IsDeleted && a.IsActive).ToListAsync();
        var result = new List<AccountBalanceReportDto>();

        foreach (var account in accounts)
        {
            var incomeQuery = db.IncomeTransactions.Where(t => !t.IsDeleted && t.AccountId == account.Id);
            var costQuery = db.CostTransactions.Where(t => !t.IsDeleted && t.AccountId == account.Id);
            if (from.HasValue) { incomeQuery = incomeQuery.Where(t => t.TransactionDate >= from.Value); costQuery = costQuery.Where(t => t.TransactionDate >= from.Value); }
            if (to.HasValue) { incomeQuery = incomeQuery.Where(t => t.TransactionDate <= to.Value); costQuery = costQuery.Where(t => t.TransactionDate <= to.Value); }

            var income = await incomeQuery.SumAsync(t => t.Amount);
            var cost = await costQuery.SumAsync(t => t.Amount);
            result.Add(new AccountBalanceReportDto(account.Id, account.Name, income, cost, income - cost));
        }
        return result;
    }

    public async Task<IReadOnlyList<CostTypeReportDto>> GetCostTypeReportAsync(DateTime? from, DateTime? to)
    {
        var costTypes = await db.CostTypes.Where(c => !c.IsDeleted).ToListAsync();
        var result = new List<CostTypeReportDto>();

        foreach (var ct in costTypes)
        {
            var incomeQuery = db.IncomeTransactions.Where(t => !t.IsDeleted && t.CostTypeId == ct.Id);
            var costQuery = db.CostTransactions.Where(t => !t.IsDeleted && t.CostTypeId == ct.Id);
            if (from.HasValue) { incomeQuery = incomeQuery.Where(t => t.TransactionDate >= from.Value); costQuery = costQuery.Where(t => t.TransactionDate >= from.Value); }
            if (to.HasValue) { incomeQuery = incomeQuery.Where(t => t.TransactionDate <= to.Value); costQuery = costQuery.Where(t => t.TransactionDate <= to.Value); }

            var income = await incomeQuery.SumAsync(t => t.Amount);
            var cost = await costQuery.SumAsync(t => t.Amount);
            result.Add(new CostTypeReportDto(ct.Id, ct.Name, income, cost, income - cost));
        }
        return result.OrderByDescending(r => r.TotalCost + r.TotalIncome).ToList();
    }

    public async Task<DateRangeReportDto> GetSummaryAsync(DateTime from, DateTime to)
    {
        var income = await db.IncomeTransactions
            .Where(t => !t.IsDeleted && t.TransactionDate >= from && t.TransactionDate <= to)
            .SumAsync(t => t.Amount);
        var cost = await db.CostTransactions
            .Where(t => !t.IsDeleted && t.TransactionDate >= from && t.TransactionDate <= to)
            .SumAsync(t => t.Amount);
        return new DateRangeReportDto(from, to, income, cost, income - cost);
    }

    public async Task<IReadOnlyList<PersonIncomeReportDto>> GetPersonIncomeReportAsync(DateTime? from, DateTime? to)
    {
        var query = db.IncomeTransactions.Include(t => t.Person).Where(t => !t.IsDeleted);
        if (from.HasValue) query = query.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDate <= to.Value);

        return await query.GroupBy(t => new { t.PersonId, t.Person.FirstName, t.Person.LastName, t.Person.TravelPrefixId })
            .Select(g => new PersonIncomeReportDto(
                g.Key.PersonId,
                g.Key.FirstName + " " + (g.Key.LastName ?? ""),
                g.Sum(t => t.Amount),
                g.Count()))
            .OrderByDescending(r => r.TotalAmount)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<FoodCostReportDto>> GetFoodCostReportAsync(DateTime? from, DateTime? to)
    {
        var query = db.FoodGenerations.Where(f => !f.IsDeleted);
        if (from.HasValue) query = query.Where(f => f.CookDate >= from.Value);
        if (to.HasValue) query = query.Where(f => f.CookDate <= to.Value);

        return await query.OrderByDescending(f => f.CookDate)
            .Select(f => new FoodCostReportDto(f.Id, f.Name, f.CookDate, f.TotalCount, f.CostPerUnit, f.TotalCost))
            .ToListAsync();
    }
}
