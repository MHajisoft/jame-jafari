using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class TransactionService(AppDbContext db, IFusionCache cache)
{
    public async Task<PagedResult<IncomeTransactionDto>> GetIncomePagedAsync(DateTime? from, DateTime? to, int? accountId, int page, int pageSize)
    {
        var filter = db.IncomeTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);

        var total = await filter.CountAsync();
        var items = await ProjectIncome(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<IncomeTransactionDto>(items, total, page, pageSize);
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
            TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim(),
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
        var entity = await db.IncomeTransactions.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<CostTransactionDto>> GetCostPagedAsync(DateTime? from, DateTime? to, int? accountId, int page, int pageSize)
    {
        var filter = db.CostTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);

        var total = await filter.CountAsync();
        var items = await ProjectCost(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<CostTransactionDto>(items, total, page, pageSize);
    }

    public async Task<CostTransactionDto> CreateCostAsync(CreateCostTransactionRequest request, int userId, string? documentPath)
    {
        var entity = new CostTransaction
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            CostTypeId = request.CostTypeId,
            TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim(),
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            DocumentPath = documentPath,
            CreatedById = userId
        };
        db.CostTransactions.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return await GetCostByIdAsync(entity.Id) ?? throw new InvalidOperationException();
    }

    public async Task<bool> DeleteCostAsync(int id, int userId)
    {
        var entity = await db.CostTransactions.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return true;
    }

    private async Task<IncomeTransactionDto?> GetIncomeByIdAsync(int id) =>
        await ProjectIncome(db.IncomeTransactions.AsNoTracking().Where(t => t.Id == id))
            .FirstOrDefaultAsync();

    private async Task<CostTransactionDto?> GetCostByIdAsync(int id) =>
        await ProjectCost(db.CostTransactions.AsNoTracking().Where(t => t.Id == id))
            .FirstOrDefaultAsync();

    private static IQueryable<IncomeTransactionDto> ProjectIncome(IQueryable<IncomeTransaction> query) =>
        query.Select(t => new IncomeTransactionDto(
            t.Id,
            t.PersonId,
            (t.Person.NamePrefix != null ? t.Person.NamePrefix.Name + " " : "")
            + t.Person.FirstName
            + (t.Person.LastName != null ? " " + t.Person.LastName : ""),
            t.AccountId,
            t.Account.Name,
            t.Amount,
            t.PaymentType,
            t.CostTypeId,
            t.CostType.Name,
            t.DocumentPath,
            t.TrackingCode,
            t.Description,
            t.TransactionDate,
            new AuditInfoDto(
                t.CreatedAt,
                t.CreatedBy != null ? t.CreatedBy.Username : null,
                t.UpdatedAt,
                t.UpdatedBy != null ? t.UpdatedBy.Username : null)));

    private static IQueryable<CostTransactionDto> ProjectCost(IQueryable<CostTransaction> query) =>
        query.Select(t => new CostTransactionDto(
            t.Id,
            t.AccountId,
            t.Account.Name,
            t.Amount,
            t.CostTypeId,
            t.CostType.Name,
            t.DocumentPath,
            t.TrackingCode,
            t.Description,
            t.TransactionDate,
            new AuditInfoDto(
                t.CreatedAt,
                t.CreatedBy != null ? t.CreatedBy.Username : null,
                t.UpdatedAt,
                t.UpdatedBy != null ? t.UpdatedBy.Username : null)));
}
