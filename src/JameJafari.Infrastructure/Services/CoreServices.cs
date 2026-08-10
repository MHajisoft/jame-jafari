using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JameJafari.Core.Constants;
using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace JameJafari.Infrastructure.Services;

public class AuthService(AppDbContext db, IConfiguration config)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted && u.IsActive);

        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        var permissions = user.UserPermissions
            .Select(up => up.Permission.Code)
            .Distinct()
            .ToList();

        var token = GenerateToken(user, permissions);
        return new LoginResponse(token, user.Id, user.Username, user.Email, user.Mobile, user.AvatarPath, permissions);
    }

    public async Task<ProfileDto?> GetProfileAsync(int userId)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        return user is null ? null : MapProfile(user);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        if (user is null) return null;

        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        user.UpdatedById = userId;
        await db.SaveChangesAsync();
        return MapProfile(user);
    }

    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        if (user is null) return (false, "کاربر یافت نشد");
        if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return (false, "رمز عبور فعلی اشتباه است");

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedById = userId;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<ProfileDto?> UpdateAvatarAsync(int userId, string? path)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.IsActive);
        if (user is null) return null;

        user.AvatarPath = path;
        user.UpdatedById = userId;
        await db.SaveChangesAsync();
        return MapProfile(user);
    }

    private static ProfileDto MapProfile(User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.Mobile,
        user.AvatarPath,
        user.UserPermissions.Select(up => up.Permission.Code).Distinct().ToList());

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(int userId)
    {
        return await db.UserPermissions
            .Where(up => up.UserId == userId)
            .Select(up => up.Permission.Code)
            .Distinct()
            .ToListAsync();
    }

    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) =>
        HashPassword(password) == hash;

    private string GenerateToken(User user, IReadOnlyList<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class AuditHelper
{
    public static AuditInfoDto ToDto(AuditableEntity entity) =>
        new(entity.CreatedAt, entity.CreatedBy?.Username, entity.UpdatedAt, entity.UpdatedBy?.Username);
}

public class PersonService(AppDbContext db)
{
    public async Task<PagedResult<PersonDto>> GetPagedAsync(string? search, Gender? gender, int page, int pageSize)
    {
        var query = db.Persons
            .Include(p => p.Father)
            .Include(p => p.Mother)
            .Include(p => p.NamePrefix)
            .Include(p => p.CreatedBy)
            .Include(p => p.UpdatedBy)
            .Where(p => !p.IsDeleted);

        if (gender.HasValue)
            query = query.Where(p => p.Gender == gender.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                p.FirstName.Contains(s) ||
                (p.LastName != null && p.LastName.Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query.OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PersonDto>(items.Select(Map).ToList(), total, page, pageSize);
    }

    public async Task<PersonDto?> GetByIdAsync(int id)
    {
        var p = await db.Persons
            .Include(x => x.Father).Include(x => x.Mother).Include(x => x.NamePrefix)
            .Include(x => x.CreatedBy).Include(x => x.UpdatedBy)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return p is null ? null : Map(p);
    }

    public async Task<PersonDto> CreateAsync(CreatePersonRequest request, int userId)
    {
        var entity = new Person
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            NickName = request.NickName,
            Gender = request.Gender,
            FatherId = request.FatherId,
            MotherId = request.MotherId,
            Mobile = request.Mobile,
            Address = request.Address,
            NamePrefixId = request.NamePrefixId,
            IsDead = request.IsDead,
            CreatedById = userId
        };
        db.Persons.Add(entity);
        await db.SaveChangesAsync();
        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PersonDto?> UpdateAsync(int id, UpdatePersonRequest request, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return null;

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.NickName = request.NickName;
        entity.Gender = request.Gender;
        entity.FatherId = request.FatherId;
        entity.MotherId = request.MotherId;
        entity.Mobile = request.Mobile;
        entity.Address = request.Address;
        entity.NamePrefixId = request.NamePrefixId;
        entity.IsDead = request.IsDead;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return false;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedById = userId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<PersonDto?> UpdatePictureAsync(int id, string path, int userId)
    {
        var entity = await db.Persons.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity is null) return null;
        entity.PicturePath = path;
        entity.UpdatedById = userId;
        await db.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public static string GetDisplayName(Person p)
    {
        var prefix = p.NamePrefix?.Name;
        var name = string.Join(" ", new[] { p.FirstName, p.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(prefix) ? name : $"{prefix} {name}";
    }

    private PersonDto Map(Person p) => new(
        p.Id, p.FirstName, p.LastName, p.NickName, p.Gender,
        p.FatherId, p.Father != null ? GetDisplayName(p.Father) : null,
        p.MotherId, p.Mother != null ? GetDisplayName(p.Mother) : null,
        p.PicturePath, p.Mobile, p.Address,
        p.NamePrefixId, p.NamePrefix?.Name, p.IsDead,
        GetDisplayName(p), AuditHelper.ToDto(p));
}
