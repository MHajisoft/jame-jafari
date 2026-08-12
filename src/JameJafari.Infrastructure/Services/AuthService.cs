using System.Security.Claims;
using JameJafari.Core.DTOs;
using JameJafari.Core.Entities;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace JameJafari.Infrastructure.Services;

public class AuthService(AppDbContext db, IConfiguration config, IAppPasswordHasher passwordHasher)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash, out var needsRehash))
            return null;

        if (needsRehash)
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
            await db.SaveChangesAsync();
        }

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
            .AsNoTracking()
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        return user is null ? null : MapProfile(user);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user is null) return null;

        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        user.UpdatedById = userId;
        await db.SaveChangesAsync();
        return MapProfile(user);
    }

    public async Task<(bool Ok, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user is null) return (false, "کاربر یافت نشد");
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash, out _))
            return (false, "رمز عبور فعلی اشتباه است");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedById = userId;
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<ProfileDto?> UpdateAvatarAsync(int userId, string? path)
    {
        var user = await db.Users
            .Include(u => u.UserPermissions).ThenInclude(up => up.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
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
