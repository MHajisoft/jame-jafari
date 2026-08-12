using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class UserService(AppDbContext db, IAppPasswordHasher passwordHasher)
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize)
    {
        var filter = db.Users.AsNoTracking();
        var total = await filter.CountAsync();
        var items = await Project(
                filter.OrderBy(u => u.Username)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<UserDto>(items, total, page, pageSize);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        return await Project(db.Users.AsNoTracking().Where(u => u.Id == id))
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, int userId)
    {
        if (SystemUsers.IsSystemAdmin(request.Username))
            throw new InvalidOperationException("این نام کاربری رزرو شده است");

        if (await db.Users.AnyAsync(u => u.Username == request.Username))
            throw new InvalidOperationException("نام کاربری تکراری است");

        await using var tx = await db.Database.BeginTransactionAsync();

        var entity = new User
        {
            Username = request.Username,
            PasswordHash = passwordHasher.Hash(request.Password),
            Email = request.Email,
            Mobile = request.Mobile,
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        await ReplacePermissionsAsync(entity.Id, request.PermissionIds);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return null;
        if (SystemUsers.IsSystemAdmin(entity.Username))
            throw new InvalidOperationException("مدیر اصلی فقط از صفحه پروفایل قابل ویرایش است");

        await using var tx = await db.Database.BeginTransactionAsync();

        entity.Email = request.Email;
        entity.Mobile = request.Mobile;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            entity.PasswordHash = passwordHasher.Hash(request.NewPassword);

        await ReplacePermissionsAsync(id, request.PermissionIds);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return false;
        if (SystemUsers.IsSystemAdmin(entity.Username))
            throw new InvalidOperationException("مدیر اصلی قابل حذف نیست");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UpdateAvatarAsync(int id, string? path, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return null;
        if (SystemUsers.IsSystemAdmin(entity.Username))
            throw new InvalidOperationException("تصویر مدیر اصلی فقط از صفحه پروفایل قابل تغییر است");

        entity.AvatarPath = path;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private async Task ReplacePermissionsAsync(int userId, IReadOnlyList<int> permissionIds)
    {
        var existing = await db.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
        db.UserPermissions.RemoveRange(existing);
        foreach (var permissionId in permissionIds.Distinct())
            db.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId });
    }

    private static IQueryable<UserDto> Project(IQueryable<User> query) =>
        query.Select(u => new UserDto(
            u.Id,
            u.Username,
            u.Email,
            u.Mobile,
            u.AvatarPath,
            u.IsActive,
            u.Username.ToLower() == SystemUsers.AdminUsername,
            u.UserPermissions.Select(up => up.Permission.Code).ToList(),
            new AuditInfoDto(
                u.CreatedAt,
                u.CreatedBy != null ? u.CreatedBy.Username : null,
                u.UpdatedAt,
                u.UpdatedBy != null ? u.UpdatedBy.Username : null)));
}
