using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class UserService(AppDbContext db, IAppPasswordHasher passwordHasher)
{
    public async Task<PagedResult<UserResponse>> GetPagedAsync(int page, int pageSize)
    {
        var filter = db.Users.AsNoTracking();
        var total = await filter.CountAsync();
        var rows = await ProjectRows(
                filter.OrderBy(u => u.Username)
                    .Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();
        return new PagedResult<UserResponse>(rows.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var row = await ProjectRows(db.Users.AsNoTracking().Where(u => u.Id == id))
            .FirstOrDefaultAsync();
        return row is null ? null : ToDto(row);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, int userId)
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

    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest request, int userId)
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

        await ReplacePermissionsAsync(id, request.PermissionIds);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<UserResponse?> ChangePasswordAsync(int id, ChangeUserPasswordRequest request, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (entity is null) return null;
        if (SystemUsers.IsSystemAdmin(entity.Username))
            throw new InvalidOperationException("رمز مدیر اصلی فقط از صفحه پروفایل قابل تغییر است");

        entity.PasswordHash = passwordHasher.Hash(request.NewPassword);
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
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

    public async Task<UserResponse?> UpdateAvatarAsync(int id, string? path, int userId)
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

    private static IQueryable<UserRow> ProjectRows(IQueryable<User> query) =>
        query.Select(u => new UserRow
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Mobile = u.Mobile,
            AvatarPath = u.AvatarPath,
            IsActive = u.IsActive,
            Permissions = u.UserPermissions.Select(up => up.Permission.Code).ToList(),
            CreatedAt = u.CreatedAt,
            CreatedByUsername = u.CreatedBy != null ? u.CreatedBy.Username : null,
            CreatedByAvatarPath = u.CreatedBy != null ? u.CreatedBy.AvatarPath : null,
            UpdatedAt = u.UpdatedAt,
            UpdatedByUsername = u.UpdatedBy != null ? u.UpdatedBy.Username : null,
            UpdatedByAvatarPath = u.UpdatedBy != null ? u.UpdatedBy.AvatarPath : null
        });

    private static UserResponse ToDto(UserRow row) => new()
    {
        Id = row.Id,
        Username = row.Username,
        Email = row.Email,
        Mobile = row.Mobile,
        AvatarPath = row.AvatarPath,
        IsActive = row.IsActive,
        IsSystemAdmin = row.Username == SystemUsers.AdminUsername,
        Permissions = row.Permissions,
        Audit = AuditHelper.FromProjection(
            row.CreatedAt,
            row.CreatedByUsername,
            row.CreatedByAvatarPath,
            row.UpdatedAt,
            row.UpdatedByUsername,
            row.UpdatedByAvatarPath)
    };

    private sealed class UserRow
    {
        public int Id { get; init; }
        public string Username { get; init; } = "";
        public string? Email { get; init; }
        public string? Mobile { get; init; }
        public string? AvatarPath { get; init; }
        public bool IsActive { get; init; }
        public List<string> Permissions { get; init; } = [];
        public DateTime CreatedAt { get; init; }
        public string? CreatedByUsername { get; init; }
        public string? CreatedByAvatarPath { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? UpdatedByUsername { get; init; }
        public string? UpdatedByAvatarPath { get; init; }
    }
}
