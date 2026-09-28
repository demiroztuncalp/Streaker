using Streaker.Application.Contracts;

namespace Streaker.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken ct = default);
}
