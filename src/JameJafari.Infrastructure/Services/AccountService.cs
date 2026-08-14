using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure.Services;

public class AccountService(AppDbContext db, IFusionCache cache)
{
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(bool activeOnly = false)
    {
        return await cache.GetOrSetAsync(
            CacheKeys.Accounts(activeOnly),
            async _ =>
            {
                var query = db.Accounts.AsNoTracking().AsQueryable();
                if (activeOnly) query = query.Where(a => a.IsActive);
                return await Project(query.OrderBy(a => a.Name)).ToListAsync();
            },
            options => options.SetDuration(LookupCache.AccountsDuration));
    }

    public async Task<AccountDto?> GetByIdAsync(int id)
    {
        return await Project(db.Accounts.AsNoTracking().Where(a => a.Id == id))
            .FirstOrDefaultAsync();
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request, int userId)
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

    public async Task<AccountDto?> UpdateAsync(int id, UpdateAccountRequest request, int userId)
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

    private static IQueryable<AccountDto> Project(IQueryable<Account> query) =>
        query.Select(a => new AccountDto(
            a.Id,
            a.Name,
            a.Description,
            a.IsActive,
            new AuditInfoDto(
                a.CreatedAt,
                a.CreatedBy != null ? a.CreatedBy.Username : null,
                a.CreatedBy != null ? a.CreatedBy.AvatarPath : null,
                a.UpdatedAt,
                a.UpdatedBy != null ? a.UpdatedBy.Username : null,
                a.UpdatedBy != null ? a.UpdatedBy.AvatarPath : null)));
}
