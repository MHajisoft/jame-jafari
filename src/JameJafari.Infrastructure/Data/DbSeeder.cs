using JameJafari.Core.Constants;
using JameJafari.Core.Entities;
using JameJafari.Core.Enums;
using JameJafari.Core.Helpers;
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

        try
        {
            logger.LogInformation("Applying EF migrations...");
            await db.Database.MigrateAsync();
            logger.LogInformation("EF migrations complete.");
            await EnsureChatIdColumnsAreNvarcharAsync(db);

            logger.LogInformation("Syncing permissions...");
            await SyncPermissionsAsync(db, logger);

            if (!await db.Users.AnyAsync())
            {
                logger.LogInformation("Seeding default admin user...");
                var allPerms = await db.Permissions.ToListAsync();
                var admin = new User
                {
                    Username = SystemUsers.AdminUsername,
                    PasswordHash = passwordHasher.Hash("admin@123"),
                    IsActive = true,
                    Email = "admin@jame-jafari.local"
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();

                db.UserPermissions.AddRange(allPerms.Select(p => new UserPermission { UserId = admin.Id, PermissionId = p.Id }));
                await db.SaveChangesAsync();
                logger.LogInformation("Default admin user created: {Username} (all permissions)", SystemUsers.AdminUsername);
            }

            if (!await db.GeneralTypes.AnyAsync())
            {
                logger.LogInformation("Seeding general types...");
                db.GeneralTypes.AddRange(
                    new GeneralType { Name = "کیلوگرم", Code = "kg", Category = GeneralTypeCategory.Unit, SortOrder = 1 },
                    new GeneralType { Name = "گرم", Code = "g", Category = GeneralTypeCategory.Unit, SortOrder = 2 },
                    new GeneralType { Name = "لیتر", Code = "l", Category = GeneralTypeCategory.Unit, SortOrder = 3 },
                    new GeneralType { Name = "عدد", Code = "pcs", Category = GeneralTypeCategory.Unit, SortOrder = 4 },
                    new GeneralType { Name = "مثقال", Code = "msgh", Category = GeneralTypeCategory.Unit, SortOrder = 5 },
                    new GeneralType { Name = "حاج", Code = "haj", Category = GeneralTypeCategory.NamePrefix, SortOrder = 1 },
                    new GeneralType { Name = "حاجیه", Code = "hajie", Category = GeneralTypeCategory.NamePrefix, SortOrder = 2 },
                    new GeneralType { Name = "کربلایی", Code = "karbalaee", Category = GeneralTypeCategory.NamePrefix, SortOrder = 3 },
                    new GeneralType { Name = "مشهدی", Code = "mashhady", Category = GeneralTypeCategory.NamePrefix, SortOrder = 4 },
                    new GeneralType { Name = "دکتر", Code = "doctor", Category = GeneralTypeCategory.NamePrefix, SortOrder = 5 },
                    new GeneralType { Name = "مهندس", Code = "engeener", Category = GeneralTypeCategory.NamePrefix, SortOrder = 6 }
                );
                await db.SaveChangesAsync();
            }

            if (!await db.Accounts.AnyAsync())
            {
                logger.LogInformation("Seeding default account...");
                db.Accounts.Add(new Account { Name = "صندوق اصلی", Description = "صندوق مرکزی موسسه", IsActive = true });
                await db.SaveChangesAsync();
            }

            logger.LogInformation("Seeding default message channel...");
            await SeedDefaultMessageChannelAsync(scope.ServiceProvider, db, logger);
            logger.LogInformation("Database seed complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration/seed failed: {Message}", ex.Message);
            throw;
        }
    }

    static async Task SeedDefaultMessageChannelAsync(IServiceProvider services, AppDbContext db, ILogger logger)
    {
        if (await db.MessageChannels.AnyAsync())
            return;

        var configuration = services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var raw = configuration["BALE_DEFAULT_GROUP_CHAT_ID"]
            ?? configuration["Bale:DefaultGroupChatId"];
        if (string.IsNullOrWhiteSpace(raw))
            return;

        string externalChatId;
        try
        {
            externalChatId = BaleChatTargetHelper.Normalize(raw);
        }
        catch
        {
            return;
        }

        db.MessageChannels.Add(new MessageChannel
        {
            Name = "گروه پیش‌فرض",
            MessengerKind = MessengerKind.Bale,
            ExternalChatId = externalChatId,
            IsActive = true
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded default Bale message channel from BALE_DEFAULT_GROUP_CHAT_ID");
    }

    /// <summary>
    /// Keeps the Permissions table aligned with <see cref="PermissionCodes.All"/>.
    /// Adds new codes; removes obsolete codes and their user assignments.
    /// New catalog permissions are assigned only to the system admin user.
    /// </summary>
    private static async Task SyncPermissionsAsync(AppDbContext db, ILogger logger)
    {
        var desired = PermissionCodes.All.ToHashSet(StringComparer.Ordinal);
        var existing = await db.Permissions.ToListAsync();

        var missingCodes = desired.Except(existing.Select(p => p.Code), StringComparer.Ordinal).ToList();
        var addedPermissionIds = new List<int>();

        if (missingCodes.Count > 0)
        {
            foreach (var code in missingCodes)
            {
                var permission = new Permission { Code = code, Name = code, Description = code };
                db.Permissions.Add(permission);
                existing.Add(permission);
            }
            await db.SaveChangesAsync();
            addedPermissionIds = existing
                .Where(p => missingCodes.Contains(p.Code, StringComparer.Ordinal))
                .Select(p => p.Id)
                .ToList();
            logger.LogInformation("Added permissions: {Codes}", string.Join(", ", missingCodes));
        }

        var obsolete = existing.Where(p => !desired.Contains(p.Code)).ToList();
        if (obsolete.Count > 0)
        {
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

        await EnsureAdminHasAllPermissionsAsync(db, logger, addedPermissionIds);
    }

    private static async Task EnsureAdminHasAllPermissionsAsync(
        AppDbContext db,
        ILogger logger,
        IReadOnlyList<int> newlyAddedPermissionIds)
    {
        var admin = await db.Users
            .FirstOrDefaultAsync(u => u.Username == SystemUsers.AdminUsername);
        if (admin is null) return;

        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();
        var adminPermissionIds = await db.UserPermissions
            .Where(up => up.UserId == admin.Id)
            .Select(up => up.PermissionId)
            .ToListAsync();

        var missingForAdmin = allPermissionIds.Except(adminPermissionIds).ToList();
        if (missingForAdmin.Count == 0) return;

        db.UserPermissions.AddRange(missingForAdmin.Select(permissionId => new UserPermission
        {
            UserId = admin.Id,
            PermissionId = permissionId
        }));
        await db.SaveChangesAsync();

        if (newlyAddedPermissionIds.Count > 0)
        {
            var syncedNew = missingForAdmin.Intersect(newlyAddedPermissionIds).ToList();
            if (syncedNew.Count > 0)
            {
                logger.LogInformation(
                    "Assigned {Count} new permissions to system admin only",
                    syncedNew.Count);
            }
        }

        var backfilled = missingForAdmin.Except(newlyAddedPermissionIds).ToList();
        if (backfilled.Count > 0)
        {
            logger.LogInformation(
                "Backfilled {Count} permissions for system admin",
                backfilled.Count);
        }
    }

    static async Task EnsureChatIdColumnsAreNvarcharAsync(AppDbContext db)
    {
        const string sql = """
            SELECT c.name AS ColumnName, t.name AS TypeName
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE (c.object_id = OBJECT_ID('dbo.MessageChannels') AND c.name = 'ExternalChatId')
               OR (c.object_id = OBJECT_ID('dbo.BaleMessages') AND c.name = 'ChatId');
            """;

        var rows = await db.Database.SqlQueryRaw<ChatIdColumnTypeRow>(sql).ToListAsync();
        var invalid = rows.Where(r => !string.Equals(r.TypeName, "nvarchar", StringComparison.OrdinalIgnoreCase)).ToList();
        if (invalid.Count == 0)
            return;

        var details = string.Join(", ", invalid.Select(r => $"{r.ColumnName}={r.TypeName}"));
        throw new InvalidOperationException(
            $"ستون‌های شناسه گفتگو باید nvarchar باشند؛ مقدار فعلی: {details}. dotnet ef database update را اجرا کنید.");
    }

    sealed class ChatIdColumnTypeRow
    {
        public string ColumnName { get; set; } = "";
        public string TypeName { get; set; } = "";
    }
}
