using JameJafari.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JameJafari.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<GeneralType> GeneralTypes => Set<GeneralType>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CostType> CostTypes => Set<CostType>();
    public DbSet<IncomeTransaction> IncomeTransactions => Set<IncomeTransaction>();
    public DbSet<CostTransaction> CostTransactions => Set<CostTransaction>();
    public DbSet<FoodGeneration> FoodGenerations => Set<FoodGeneration>();
    public DbSet<FoodIngredient> FoodIngredients => Set<FoodIngredient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Mobile).HasMaxLength(20);
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100);
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<UserPermission>(e =>
        {
            e.HasKey(x => new { x.UserId, x.PermissionId });
            e.HasOne(x => x.User).WithMany(x => x.UserPermissions).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Permission).WithMany(x => x.UserPermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<GeneralType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(50);
        });

        modelBuilder.Entity<Person>(e =>
        {
            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.NickName).HasMaxLength(100);
            e.Property(x => x.Mobile).HasMaxLength(20);
            e.HasOne(x => x.Father).WithMany(x => x.ChildrenAsFather).HasForeignKey(x => x.FatherId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Mother).WithMany(x => x.ChildrenAsMother).HasForeignKey(x => x.MotherId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NamePrefix).WithMany().HasForeignKey(x => x.NamePrefixId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<CostType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IncomeTransaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TrackingCode).HasMaxLength(100);
            e.HasOne(x => x.Person).WithMany(x => x.IncomeTransactions).HasForeignKey(x => x.PersonId);
            e.HasOne(x => x.Account).WithMany(x => x.IncomeTransactions).HasForeignKey(x => x.AccountId);
            e.HasOne(x => x.CostType).WithMany(x => x.IncomeTransactions).HasForeignKey(x => x.CostTypeId);
        });

        modelBuilder.Entity<CostTransaction>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.TrackingCode).HasMaxLength(100);
            e.HasOne(x => x.Account).WithMany(x => x.CostTransactions).HasForeignKey(x => x.AccountId);
            e.HasOne(x => x.CostType).WithMany(x => x.CostTransactions).HasForeignKey(x => x.CostTypeId);
        });

        modelBuilder.Entity<FoodGeneration>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.TotalCost).HasPrecision(18, 2);
            e.Property(x => x.CostPerUnit).HasPrecision(18, 4);
        });

        modelBuilder.Entity<FoodIngredient>(e =>
        {
            e.Property(x => x.Units).HasPrecision(18, 4);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.RecommendedPrice).HasPrecision(18, 2);
            e.HasOne(x => x.FoodGeneration).WithMany(x => x.Ingredients).HasForeignKey(x => x.FoodGenerationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CostType).WithMany(x => x.FoodIngredients).HasForeignKey(x => x.CostTypeId);
        });

        ConfigureAuditRelations<User>(modelBuilder);
        ConfigureAuditRelations<GeneralType>(modelBuilder);
        ConfigureAuditRelations<Person>(modelBuilder);
        ConfigureAuditRelations<Account>(modelBuilder);
        ConfigureAuditRelations<CostType>(modelBuilder);
        ConfigureAuditRelations<IncomeTransaction>(modelBuilder);
        ConfigureAuditRelations<CostTransaction>(modelBuilder);
        ConfigureAuditRelations<FoodGeneration>(modelBuilder);
    }

    private static void ConfigureAuditRelations<T>(ModelBuilder modelBuilder) where T : AuditableEntity
    {
        modelBuilder.Entity<T>().HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<T>().HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<T>().HasOne(x => x.DeletedBy).WithMany().HasForeignKey(x => x.DeletedById).OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
