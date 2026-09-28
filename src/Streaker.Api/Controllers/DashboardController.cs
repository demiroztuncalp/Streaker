using Microsoft.AspNetCore.Mvc;
using Streaker.Application.Contracts;
using Streaker.Application.Dashboard;

namespace Streaker.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Today's progress and the top-3 streak leaderboard.</summary>
    [HttpGet]
    public async Task<DashboardDto> Get(CancellationToken ct) => await dashboard.GetAsync(ct);
}
