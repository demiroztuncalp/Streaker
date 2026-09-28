using Microsoft.AspNetCore.Mvc;
using Streaker.Application.Contracts;
using Streaker.Application.Habits;

namespace Streaker.Api.Controllers;

[ApiController]
[Route("api/habits")]
[Produces("application/json")]
public sealed class HabitsController(IHabitService habits) : ControllerBase
{
    /// <summary>List habits, longest current streak first.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<HabitDto>> List([FromQuery] bool includeArchived = false, CancellationToken ct = default)
        => await habits.ListAsync(includeArchived, ct);

    /// <summary>Get a single habit.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<HabitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<HabitDto> Get(Guid id, CancellationToken ct) => await habits.GetAsync(id, ct);

    /// <summary>Create a new habit.</summary>
    [HttpPost]
    [ProducesResponseType<HabitDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<CreatedAtActionResult> Create(CreateHabitRequest request, CancellationToken ct)
    {
        var habit = await habits.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = habit.Id }, habit);
    }

    /// <summary>Rename or re-describe a habit.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<HabitDto> Update(Guid id, UpdateHabitRequest request, CancellationToken ct)
        => await habits.UpdateAsync(id, request, ct);

    /// <summary>Permanently delete a habit and its history.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Delete(Guid id, CancellationToken ct)
    {
        await habits.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Check in for today (or a past day). Idempotent.</summary>
    [HttpPost("{id:guid}/check-ins")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<HabitDto> CheckIn(Guid id, [FromBody] CheckInRequest? request, CancellationToken ct)
        => await habits.CheckInAsync(id, request?.Date, ct);

    /// <summary>Remove the check-in of a given day.</summary>
    [HttpDelete("{id:guid}/check-ins/{date}")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<HabitDto> UndoCheckIn(Guid id, DateOnly date, CancellationToken ct)
        => await habits.UndoCheckInAsync(id, date, ct);

    /// <summary>Streaks, completion rates and a 4-week emoji heatmap.</summary>
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<HabitStatsDto> Stats(Guid id, CancellationToken ct) => await habits.GetStatsAsync(id, ct);

    /// <summary>Archive a habit; it stops counting on the dashboard.</summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<HabitDto> Archive(Guid id, CancellationToken ct) => await habits.SetArchivedAsync(id, true, ct);

    /// <summary>Bring an archived habit back.</summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<HabitDto> Restore(Guid id, CancellationToken ct) => await habits.SetArchivedAsync(id, false, ct);
}
