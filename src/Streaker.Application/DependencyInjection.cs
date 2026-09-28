using Microsoft.Extensions.DependencyInjection;
using Streaker.Application.Dashboard;
using Streaker.Application.Habits;

namespace Streaker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IHabitService, HabitService>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
