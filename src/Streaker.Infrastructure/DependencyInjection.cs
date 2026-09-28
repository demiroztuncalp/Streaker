using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Streaker.Application.Abstractions;
using Streaker.Infrastructure.Persistence;

namespace Streaker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=streaker.db";

        services.AddDbContext<StreakerDbContext>(o => o.UseSqlite(connectionString));
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
