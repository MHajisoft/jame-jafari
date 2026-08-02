using JameJafari.Core.Constants;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Services;
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
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        await db.Database.MigrateAsync();

        if (!await db.Permissions.AnyAsync())
        {
            var permissions = PermissionCodes.All.Select(code => new Permission
            {
                Code = code,
                Name = code,
                Description = code
            }).ToList();
            db.Permissions.AddRange(permissions);
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync())
        {
            var allPerms = await db.Permissions.ToListAsync();
            var admin = new User
            {
                Username = "admin",
                PasswordHash = AuthService.HashPassword("admin123"),
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
                new GeneralType { Name = "حاجی", Code = "haji", Category = GeneralTypeCategory.TravelPrefix, SortOrder = 1 },
                new GeneralType { Name = "کربلایی", Code = "karbalaee", Category = GeneralTypeCategory.TravelPrefix, SortOrder = 2 },
                new GeneralType { Name = "مشهدی", Code = "mashhady", Category = GeneralTypeCategory.TravelPrefix, SortOrder = 3 },
                new GeneralType { Name = "بدون پیشوند", Code = "none", Category = GeneralTypeCategory.TravelPrefix, SortOrder = 99 }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Accounts.AnyAsync())
        {
            db.Accounts.Add(new Account { Name = "صندوق اصلی", Description = "صندوق مرکزی موسسه", IsActive = true });
            await db.SaveChangesAsync();
        }
    }
}
