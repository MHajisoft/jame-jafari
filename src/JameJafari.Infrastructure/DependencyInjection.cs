using JameJafari.Infrastructure.Data;
using JameJafari.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JameJafari.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<PersonService>();
        services.AddScoped<AccountService>();
        services.AddScoped<GeneralTypeService>();
        services.AddScoped<CostTypeService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<FoodService>();
        services.AddScoped<ReportService>();

        return services;
    }
}
