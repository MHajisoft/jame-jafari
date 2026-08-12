using JameJafari.Infrastructure.Caching;
using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Security;
using JameJafari.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZiggyCreatures.Caching.Fusion;

namespace JameJafari.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddMemoryCache();
        services.AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = LookupCache.AccountsDuration;
                options.IsFailSafeEnabled = true;
                options.FailSafeMaxDuration = TimeSpan.FromHours(1);
                options.FactorySoftTimeout = TimeSpan.FromSeconds(2);
            });

        services.AddSingleton<IAppPasswordHasher, AppPasswordHasher>();

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<PersonService>();
        services.AddScoped<AccountService>();
        services.AddScoped<LookupService>();
        services.AddScoped<GeneralTypeService>();
        services.AddScoped<CostTypeService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<FoodService>();
        services.AddScoped<ReportService>();

        return services;
    }
}
