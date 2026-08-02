using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Services;

public class UserService(AppDbContext db)
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize)
    {
        var query = db.Users.Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .Include(u => u.CreatedBy).Include(u => u.UpdatedBy)
            .Where(u => !u.IsDeleted);

        var total = await query.CountAsync();
        var items = await query.OrderBy(u => u.Username).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<UserDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var u = await db.Users.Include(x => x.UserPermissions).ThenInclude(up => up.Permission)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return u is null ? null : Map(u);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, int userId)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username && !u.IsDeleted))
            throw new InvalidOperationException("نام کاربری تکراری است");

        var entity = new User
        {
            Username = request.Username,
            PasswordHash = AuthService.HashPassword(request.Password),
            Email = request.Email,
            Mobile = request.Mobile,
            IsActive = request.IsActive,
            CreatedById = userId
        };
        db.Users.Add(entity);
        await db.SaveChangesAsync();
        await SetPermissionsAsync(entity.Id, request.PermissionIds);
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserRequest request, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (entity is null) return null;

        entity.Email = request.Email;
        entity.Mobile = request.Mobile;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            entity.PasswordHash = AuthService.HashPassword(request.NewPassword);

        await SetPermissionsAsync(id, request.PermissionIds);
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> UpdateAvatarAsync(int id, string path, int userId)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (entity is null) return null;
        entity.AvatarPath = path;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    private async Task SetPermissionsAsync(int userId, IReadOnlyList<int> permissionIds)
    {
        var existing = await db.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
        db.UserPermissions.RemoveRange(existing);
        foreach (var permissionId in permissionIds.Distinct())
            db.UserPermissions.Add(new UserPermission { UserId = userId, PermissionId = permissionId });
        await db.SaveChangesAsync();
    }

    private static UserDto Map(User u) => new(
        u.Id, u.Username, u.Email, u.Mobile, u.AvatarPath, u.IsActive,
        u.UserPermissions.Select(up => up.Permission.Code).ToList(),
        AuditHelper.ToDto(u));
}

public class AccountService(AppDbContext db)
{
    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(bool activeOnly = false)
    {
        var query = db.Accounts.Include(a => a.CreatedBy).Include(a => a.UpdatedBy).Where(a => !a.IsDeleted);
        if (activeOnly) query = query.Where(a => a.IsActive);
        var items = await query.OrderBy(a => a.Name).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<AccountDto?> GetByIdAsync(int id)
    {
        var a = await db.Accounts.Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return a is null ? null : Map(a);
    }

    public async Task<AccountDto> CreateAsync(CreateAccountRequest request, int userId)
    {
        var entity = new Account { Name = request.Name, Description = request.Description, IsActive = request.IsActive, CreatedById = userId };
        db.Accounts.Add(entity);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<AccountDto?> UpdateAsync(int id, UpdateAccountRequest request, int userId)
    {
        var entity = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    private static AccountDto Map(Account a) => new(a.Id, a.Name, a.Description, a.IsActive, AuditHelper.ToDto(a));
}

public class GeneralTypeService(AppDbContext db)
{
    public async Task<IReadOnlyList<GeneralTypeDto>> GetByCategoryAsync(GeneralTypeCategory category)
    {
        var items = await db.GeneralTypes.Where(g => !g.IsDeleted && g.Category == category && g.IsActive)
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<GeneralTypeDto> CreateAsync(CreateGeneralTypeRequest request, int userId)
    {
        if (!Enum.TryParse<GeneralTypeCategory>(request.Category, true, out var category))
            throw new ArgumentException("Invalid category");

        var entity = new GeneralType
        {
            Name = request.Name, Code = request.Code, Category = category,
            SortOrder = request.SortOrder, IsActive = request.IsActive, CreatedById = userId
        };
        db.GeneralTypes.Add(entity);
        await db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<GeneralTypeDto?> UpdateAsync(int id, UpdateGeneralTypeRequest request, int userId)
    {
        var entity = await db.GeneralTypes.FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return Map(entity);
    }

    private static GeneralTypeDto Map(GeneralType g) =>
        new(g.Id, g.Name, g.Code, g.Category.ToString(), g.SortOrder, g.IsActive);
}

public class CostTypeService(AppDbContext db)
{
    public async Task<IReadOnlyList<CostTypeDto>> GetAllAsync(bool? isIngredient = null)
    {
        var query = db.CostTypes.Include(c => c.Unit).Include(c => c.CreatedBy).Include(c => c.UpdatedBy)
            .Where(c => !c.IsDeleted && c.IsActive);
        if (isIngredient.HasValue) query = query.Where(c => c.IsIngredient == isIngredient.Value);
        var items = await query.OrderBy(c => c.Name).ToListAsync();
        return items.Select(Map).ToList();
    }

    public async Task<CostTypeDto> CreateAsync(CreateCostTypeRequest request, int userId)
    {
        var entity = new CostType
        {
            Name = request.Name, Description = request.Description,
            IsIngredient = request.IsIngredient, UnitId = request.UnitId,
            IsActive = request.IsActive, CreatedById = userId
        };
        db.CostTypes.Add(entity);
        await db.SaveChangesAsync();
        return (await GetAllAsync()).First(c => c.Id == entity.Id);
    }

    public async Task<CostTypeDto?> UpdateAsync(int id, UpdateCostTypeRequest request, int userId)
    {
        var entity = await db.CostTypes.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null) return null;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.IsIngredient = request.IsIngredient;
        entity.UnitId = request.UnitId;
        entity.IsActive = request.IsActive;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return (await GetAllAsync()).FirstOrDefault(c => c.Id == id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.CostTypes.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    private static CostTypeDto Map(CostType c) => new(
        c.Id, c.Name, c.Description, c.IsIngredient, c.UnitId, c.Unit?.Name, c.IsActive, AuditHelper.ToDto(c));
}
