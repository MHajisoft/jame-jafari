using JameJafari.Core.Constants;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JameJafari.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IAppPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await db.Database.MigrateAsync();
        await SyncPermissionsAsync(db, logger);

        if (!await db.Users.AnyAsync())
        {
            var allPerms = await db.Permissions.ToListAsync();
            var admin = new User
            {
                Username = "admin",
                PasswordHash = passwordHasher.Hash("admin@123"),
                IsActive = true,
                Email = "admin@jame-jafari.local"
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();

            db.UserPermissions.AddRange(allPerms.Select(p => new UserPermission { UserId = admin.Id, PermissionId = p.Id }));
            await db.SaveChangesAsync();
            logger.LogInformation("Default admin user created: admin / admin123 (all permissions)");
        }

        if (!await db.GeneralTypes.AnyAsync())
        {
            db.GeneralTypes.AddRange(
                new GeneralType { Name = "کیلوگرم", Code = "kg", Category = GeneralTypeCategory.Unit, SortOrder = 1 },
                new GeneralType { Name = "گرم", Code = "g", Category = GeneralTypeCategory.Unit, SortOrder = 2 },
                new GeneralType { Name = "لیتر", Code = "l", Category = GeneralTypeCategory.Unit, SortOrder = 3 },
                new GeneralType { Name = "عدد", Code = "pcs", Category = GeneralTypeCategory.Unit, SortOrder = 4 },
                new GeneralType { Name = "حاج", Code = "haj", Category = GeneralTypeCategory.NamePrefix, SortOrder = 1 },
                new GeneralType { Name = "کربلایی", Code = "karbalaee", Category = GeneralTypeCategory.NamePrefix, SortOrder = 2 },
                new GeneralType { Name = "مشهدی", Code = "mashhady", Category = GeneralTypeCategory.NamePrefix, SortOrder = 3 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Accounts.AnyAsync())
        {
            db.Accounts.Add(new Account { Name = "صندوق اصلی", Description = "صندوق مرکزی موسسه", IsActive = true });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Keeps the Permissions table aligned with <see cref="PermissionCodes.All"/>.
    /// Adds new codes; removes obsolete codes and their user assignments.
    /// Does not auto-assign new permissions to existing users.
    /// </summary>
    private static async Task SyncPermissionsAsync(AppDbContext db, ILogger logger)
    {
        var desired = PermissionCodes.All.ToHashSet(StringComparer.Ordinal);
        var existing = await db.Permissions.ToListAsync();

        var missing = desired.Except(existing.Select(p => p.Code), StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            db.Permissions.AddRange(missing.Select(code => new Permission
            {
                Code = code,
                Name = code,
                Description = code
            }));
            await db.SaveChangesAsync();
            logger.LogInformation("Added permissions: {Codes}", string.Join(", ", missing));
        }

        var obsolete = existing.Where(p => !desired.Contains(p.Code)).ToList();
        if (obsolete.Count == 0) return;

        var obsoleteIds = obsolete.Select(p => p.Id).ToList();
        var assignments = await db.UserPermissions
            .Where(up => obsoleteIds.Contains(up.PermissionId))
            .ToListAsync();
        if (assignments.Count > 0)
        {
            db.UserPermissions.RemoveRange(assignments);
            logger.LogInformation(
                "Removed {Count} user-permission assignments for obsolete permissions",
                assignments.Count);
        }

        db.Permissions.RemoveRange(obsolete);
        await db.SaveChangesAsync();
        logger.LogInformation(
            "Removed obsolete permissions: {Codes}",
            string.Join(", ", obsolete.Select(p => p.Code)));
    }
}
