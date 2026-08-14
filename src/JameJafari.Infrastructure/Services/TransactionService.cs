using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class TransactionService(AppDbContext db, IFusionCache cache)
{
    public async Task<PagedResult<IncomeTransactionDto>> GetIncomePagedAsync(
        DateTime? from, DateTime? to, int? accountId, int page, int pageSize, int? createdByUserId = null)
    {
        var filter = db.IncomeTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);
        if (createdByUserId.HasValue) filter = filter.Where(t => t.CreatedById == createdByUserId.Value);

        var total = await filter.CountAsync();
        var items = await ProjectIncome(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<IncomeTransactionDto>(items, total, page, pageSize);
    }

    public async Task<IncomeTransactionDto> CreateIncomeAsync(
        CreateIncomeTransactionRequest request,
        int userId,
        IReadOnlyList<string> documentPaths)
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
            CreatedById = userId
        };

        foreach (var path in documentPaths)
        {
            entity.Attachments.Add(new TransactionAttachment { Path = path });
        }

        db.IncomeTransactions.Add(entity);
        await db.SaveChangesAsync();
        return await GetIncomeByIdAsync(entity.Id)
            ?? throw new InvalidOperationException("تراکنش پس از ذخیره یافت نشد");
    }

    public async Task<IncomeTransactionDto?> UpdateIncomeAsync(
        int id,
        UpdateIncomeTransactionRequest request,
        int userId,
        IReadOnlyList<string> newDocumentPaths,
        int? requireCreatedByUserId = null)
    {
        var entity = await db.IncomeTransactions
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return null;
        if (requireCreatedByUserId.HasValue && entity.CreatedById != requireCreatedByUserId.Value)
            return null;

        entity.PersonId = request.PersonId;
        entity.AccountId = request.AccountId;
        entity.Amount = request.Amount;
        entity.PaymentType = request.PaymentType;
        entity.CostTypeId = request.CostTypeId;
        entity.TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim();
        entity.Description = request.Description;
        entity.TransactionDate = request.TransactionDate;
        entity.UpdatedById = userId;

        foreach (var path in newDocumentPaths)
        {
            entity.Attachments.Add(new TransactionAttachment { Path = path });
        }

        await db.SaveChangesAsync();
        return await GetIncomeByIdAsync(entity.Id);
    }

    public async Task<bool> DeleteIncomeAttachmentAsync(int transactionId, int attachmentId, int? requireCreatedByUserId = null)
    {
        if (requireCreatedByUserId.HasValue)
        {
            var owned = await db.IncomeTransactions.AsNoTracking()
                .AnyAsync(t => t.Id == transactionId && t.CreatedById == requireCreatedByUserId.Value);
            if (!owned) return false;
        }

        var attachment = await db.TransactionAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.IncomeTransactionId == transactionId);
        if (attachment is null) return false;

        db.TransactionAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteIncomeAsync(int id, int userId, int? requireCreatedByUserId = null)
    {
        var entity = await db.IncomeTransactions.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return false;
        if (requireCreatedByUserId.HasValue && entity.CreatedById != requireCreatedByUserId.Value)
            return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResult<CostTransactionDto>> GetCostPagedAsync(
        DateTime? from, DateTime? to, int? accountId, int page, int pageSize, int? createdByUserId = null)
    {
        var filter = db.CostTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);
        if (createdByUserId.HasValue) filter = filter.Where(t => t.CreatedById == createdByUserId.Value);

        var total = await filter.CountAsync();
        var items = await ProjectCost(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<CostTransactionDto>(items, total, page, pageSize);
    }

    public async Task<CostTransactionDto> CreateCostAsync(
        CreateCostTransactionRequest request,
        int userId,
        IReadOnlyList<string> documentPaths)
    {
        var entity = new CostTransaction
        {
            AccountId = request.AccountId,
            Amount = request.Amount,
            CostTypeId = request.CostTypeId,
            TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim(),
            Description = request.Description,
            TransactionDate = request.TransactionDate,
            CreatedById = userId
        };

        foreach (var path in documentPaths)
        {
            entity.Attachments.Add(new TransactionAttachment { Path = path });
        }

        db.CostTransactions.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return await GetCostByIdAsync(entity.Id)
            ?? throw new InvalidOperationException("تراکنش پس از ذخیره یافت نشد");
    }

    public async Task<CostTransactionDto?> UpdateCostAsync(
        int id,
        UpdateCostTransactionRequest request,
        int userId,
        IReadOnlyList<string> newDocumentPaths,
        int? requireCreatedByUserId = null)
    {
        var entity = await db.CostTransactions
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return null;
        if (requireCreatedByUserId.HasValue && entity.CreatedById != requireCreatedByUserId.Value)
            return null;

        entity.AccountId = request.AccountId;
        entity.Amount = request.Amount;
        entity.CostTypeId = request.CostTypeId;
        entity.TrackingCode = string.IsNullOrWhiteSpace(request.TrackingCode) ? null : request.TrackingCode.Trim();
        entity.Description = request.Description;
        entity.TransactionDate = request.TransactionDate;
        entity.UpdatedById = userId;

        foreach (var path in newDocumentPaths)
        {
            entity.Attachments.Add(new TransactionAttachment { Path = path });
        }

        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return await GetCostByIdAsync(entity.Id);
    }

    public async Task<bool> DeleteCostAttachmentAsync(int transactionId, int attachmentId, int? requireCreatedByUserId = null)
    {
        if (requireCreatedByUserId.HasValue)
        {
            var owned = await db.CostTransactions.AsNoTracking()
                .AnyAsync(t => t.Id == transactionId && t.CreatedById == requireCreatedByUserId.Value);
            if (!owned) return false;
        }

        var attachment = await db.TransactionAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CostTransactionId == transactionId);
        if (attachment is null) return false;

        db.TransactionAttachments.Remove(attachment);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCostAsync(int id, int userId, int? requireCreatedByUserId = null)
    {
        var entity = await db.CostTransactions.FirstOrDefaultAsync(t => t.Id == id);
        if (entity is null) return false;
        if (requireCreatedByUserId.HasValue && entity.CreatedById != requireCreatedByUserId.Value)
            return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateIngredientRecsAsync(cache);
        return true;
    }

    public async Task<string?> GetIncomeAttachmentPathAsync(int transactionId, int attachmentId, int? requireCreatedByUserId = null)
    {
        if (requireCreatedByUserId.HasValue)
        {
            var owned = await db.IncomeTransactions.AsNoTracking()
                .AnyAsync(t => t.Id == transactionId && t.CreatedById == requireCreatedByUserId.Value);
            if (!owned) return null;
        }
        return await db.TransactionAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId && a.IncomeTransactionId == transactionId)
            .Select(a => a.Path)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetCostAttachmentPathAsync(int transactionId, int attachmentId, int? requireCreatedByUserId = null)
    {
        if (requireCreatedByUserId.HasValue)
        {
            var owned = await db.CostTransactions.AsNoTracking()
                .AnyAsync(t => t.Id == transactionId && t.CreatedById == requireCreatedByUserId.Value);
            if (!owned) return null;
        }
        return await db.TransactionAttachments.AsNoTracking()
            .Where(a => a.Id == attachmentId && a.CostTransactionId == transactionId)
            .Select(a => a.Path)
            .FirstOrDefaultAsync();
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
            t.Person.NickName,
            t.AccountId,
            t.Account.Name,
            t.Amount,
            t.PaymentType,
            t.CostTypeId,
            t.CostType.Name,
            t.Attachments
                .OrderBy(a => a.Id)
                .Select(a => new TransactionAttachmentDto(a.Id, a.Path))
                .ToList(),
            t.TrackingCode,
            t.Description,
            t.TransactionDate,
            new AuditInfoDto(
                t.CreatedAt,
                t.CreatedBy != null ? t.CreatedBy.Username : null,
                t.CreatedBy != null ? t.CreatedBy.AvatarPath : null,
                t.UpdatedAt,
                t.UpdatedBy != null ? t.UpdatedBy.Username : null,
                t.UpdatedBy != null ? t.UpdatedBy.AvatarPath : null)));

    private static IQueryable<CostTransactionDto> ProjectCost(IQueryable<CostTransaction> query) =>
        query.Select(t => new CostTransactionDto(
            t.Id,
            t.AccountId,
            t.Account.Name,
            t.Amount,
            t.CostTypeId,
            t.CostType.Name,
            t.Attachments
                .OrderBy(a => a.Id)
                .Select(a => new TransactionAttachmentDto(a.Id, a.Path))
                .ToList(),
            t.TrackingCode,
            t.Description,
            t.TransactionDate,
            new AuditInfoDto(
                t.CreatedAt,
                t.CreatedBy != null ? t.CreatedBy.Username : null,
                t.CreatedBy != null ? t.CreatedBy.AvatarPath : null,
                t.UpdatedAt,
                t.UpdatedBy != null ? t.UpdatedBy.Username : null,
                t.UpdatedBy != null ? t.UpdatedBy.AvatarPath : null)));
}
