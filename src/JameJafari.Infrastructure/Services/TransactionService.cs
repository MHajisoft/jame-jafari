using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class TransactionService(AppDbContext db, IFusionCache cache)
{
    public async Task<PagedResult<IncomeTransactionResponse>> GetIncomePagedAsync(
        DateTime? from, DateTime? to, int? accountId, int page, int pageSize, int? createdByUserId = null)
    {
        var filter = db.IncomeTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);
        if (createdByUserId.HasValue) filter = filter.Where(t => t.CreatedById == createdByUserId.Value);

        var total = await filter.CountAsync();
        var rows = await ProjectIncomeRows(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<IncomeTransactionResponse>(rows.Select(ToIncomeDto).ToList(), total, page, pageSize);
    }

    public async Task<IncomeTransactionResponse> CreateIncomeAsync(
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

    public async Task<IncomeTransactionResponse?> UpdateIncomeAsync(
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

    public async Task<PagedResult<CostTransactionResponse>> GetCostPagedAsync(
        DateTime? from, DateTime? to, int? accountId, int page, int pageSize, int? createdByUserId = null)
    {
        var filter = db.CostTransactions.AsNoTracking().AsQueryable();
        if (from.HasValue) filter = filter.Where(t => t.TransactionDate >= from.Value);
        if (to.HasValue) filter = filter.Where(t => t.TransactionDate <= to.Value);
        if (accountId.HasValue) filter = filter.Where(t => t.AccountId == accountId.Value);
        if (createdByUserId.HasValue) filter = filter.Where(t => t.CreatedById == createdByUserId.Value);

        var total = await filter.CountAsync();
        var rows = await ProjectCostRows(
                filter.OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.Id)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<CostTransactionResponse>(rows.Select(ToCostDto).ToList(), total, page, pageSize);
    }

    public async Task<CostTransactionResponse> CreateCostAsync(
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

    public async Task<CostTransactionResponse?> UpdateCostAsync(
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

    private async Task<IncomeTransactionResponse?> GetIncomeByIdAsync(int id)
    {
        var row = await ProjectIncomeRows(db.IncomeTransactions.AsNoTracking().Where(t => t.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToIncomeDto(row);
    }

    private async Task<CostTransactionResponse?> GetCostByIdAsync(int id)
    {
        var row = await ProjectCostRows(db.CostTransactions.AsNoTracking().Where(t => t.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToCostDto(row);
    }

    private static IQueryable<IncomeTransactionRow> ProjectIncomeRows(IQueryable<IncomeTransaction> query) =>
        query.Select(t => new IncomeTransactionRow
        {
            Id = t.Id,
            PersonId = t.PersonId,
            PersonFirstName = t.Person.FirstName,
            PersonLastName = t.Person.LastName,
            PersonNamePrefixName = t.Person.NamePrefix != null ? t.Person.NamePrefix.Name : null,
            PersonNickName = t.Person.NickName,
            AccountId = t.AccountId,
            AccountName = t.Account.Name,
            Amount = t.Amount,
            PaymentType = t.PaymentType,
            CostTypeId = t.CostTypeId,
            CostTypeName = t.CostType.Name,
            Attachments = t.Attachments
                .OrderBy(a => a.Id)
                .Select(a => new TransactionAttachmentResponse(a.Id, a.Path))
                .ToList(),
            TrackingCode = t.TrackingCode,
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            CreatedAt = t.CreatedAt,
            CreatedByUsername = t.CreatedBy != null ? t.CreatedBy.Username : null,
            CreatedByAvatarPath = t.CreatedBy != null ? t.CreatedBy.AvatarPath : null,
            UpdatedAt = t.UpdatedAt,
            UpdatedByUsername = t.UpdatedBy != null ? t.UpdatedBy.Username : null,
            UpdatedByAvatarPath = t.UpdatedBy != null ? t.UpdatedBy.AvatarPath : null
        });

    private static IQueryable<CostTransactionRow> ProjectCostRows(IQueryable<CostTransaction> query) =>
        query.Select(t => new CostTransactionRow
        {
            Id = t.Id,
            AccountId = t.AccountId,
            AccountName = t.Account.Name,
            Amount = t.Amount,
            CostTypeId = t.CostTypeId,
            CostTypeName = t.CostType.Name,
            Attachments = t.Attachments
                .OrderBy(a => a.Id)
                .Select(a => new TransactionAttachmentResponse(a.Id, a.Path))
                .ToList(),
            TrackingCode = t.TrackingCode,
            Description = t.Description,
            TransactionDate = t.TransactionDate,
            CreatedAt = t.CreatedAt,
            CreatedByUsername = t.CreatedBy != null ? t.CreatedBy.Username : null,
            CreatedByAvatarPath = t.CreatedBy != null ? t.CreatedBy.AvatarPath : null,
            UpdatedAt = t.UpdatedAt,
            UpdatedByUsername = t.UpdatedBy != null ? t.UpdatedBy.Username : null,
            UpdatedByAvatarPath = t.UpdatedBy != null ? t.UpdatedBy.AvatarPath : null
        });

    private static IncomeTransactionResponse ToIncomeDto(IncomeTransactionRow row) => new()
    {
        Id = row.Id,
        PersonId = row.PersonId,
        PersonName = PersonDisplayNameHelper.Format(row.PersonFirstName, row.PersonLastName, row.PersonNamePrefixName),
        PersonNickName = row.PersonNickName,
        AccountId = row.AccountId,
        AccountName = row.AccountName,
        Amount = row.Amount,
        PaymentType = row.PaymentType,
        CostTypeId = row.CostTypeId,
        CostTypeName = row.CostTypeName,
        Attachments = row.Attachments,
        TrackingCode = row.TrackingCode,
        Description = row.Description,
        TransactionDate = row.TransactionDate,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private static CostTransactionResponse ToCostDto(CostTransactionRow row) => new()
    {
        Id = row.Id,
        AccountId = row.AccountId,
        AccountName = row.AccountName,
        Amount = row.Amount,
        CostTypeId = row.CostTypeId,
        CostTypeName = row.CostTypeName,
        Attachments = row.Attachments,
        TrackingCode = row.TrackingCode,
        Description = row.Description,
        TransactionDate = row.TransactionDate,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class IncomeTransactionRow
    {
        public int Id { get; init; }
        public int PersonId { get; init; }
        public string PersonFirstName { get; init; } = "";
        public string? PersonLastName { get; init; }
        public string? PersonNamePrefixName { get; init; }
        public string? PersonNickName { get; init; }
        public int AccountId { get; init; }
        public string AccountName { get; init; } = "";
        public decimal Amount { get; init; }
        public PaymentType PaymentType { get; init; }
        public int CostTypeId { get; init; }
        public string CostTypeName { get; init; } = "";
        public List<TransactionAttachmentResponse> Attachments { get; init; } = [];
        public string? TrackingCode { get; init; }
        public string? Description { get; init; }
        public DateTime TransactionDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }

    private sealed class CostTransactionRow
    {
        public int Id { get; init; }
        public int AccountId { get; init; }
        public string AccountName { get; init; } = "";
        public decimal Amount { get; init; }
        public int CostTypeId { get; init; }
        public string CostTypeName { get; init; } = "";
        public List<TransactionAttachmentResponse> Attachments { get; init; } = [];
        public string? TrackingCode { get; init; }
        public string? Description { get; init; }
        public DateTime TransactionDate { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
