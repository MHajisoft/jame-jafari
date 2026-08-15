using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class AccountService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync(bool activeOnly = false)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.Accounts(activeOnly),
            async _ =>
            {
                var query = db.Accounts.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(a => a.IsActive);
                var rows = await ProjectRows(query.OrderBy(a => a.Name)).ToListAsync();
                return (IReadOnlyList<AccountResponse>)rows.Select(ToDto).ToList();
            },
            options => options.SetDuration(LookupCache.AccountsDuration));
    }

    public async Task<AccountResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.Accounts.AsNoTracking().Where(a => a.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request, int userId)
    {
        var entity = new Account
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.Accounts.Add(entity);
        await db.SaveChangesAsync();
        await LookupCache.InvalidateAccountsAsync(cache);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<AccountResponse?> UpdateAsync(int id, UpdateAccountRequest request, int userId)
    {
        var entity = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateAccountsAsync(cache);
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        await LookupCache.InvalidateAccountsAsync(cache);
        return true;
    }

    private static IQueryable<AccountRow> ProjectRows(IQueryable<Account> query) =>
        query.Select(a => new AccountRow
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            CreatedByUsername = a.CreatedBy != null ? a.CreatedBy.Username : null,
            CreatedByAvatarPath = a.CreatedBy != null ? a.CreatedBy.AvatarPath : null,
            UpdatedAt = a.UpdatedAt,
            UpdatedByUsername = a.UpdatedBy != null ? a.UpdatedBy.Username : null,
            UpdatedByAvatarPath = a.UpdatedBy != null ? a.UpdatedBy.AvatarPath : null
        });

    private static AccountResponse ToDto(AccountRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        IsActive = row.IsActive,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class AccountRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
