using Streaker.Application.Contracts;

namespace Streaker.Application.Habits;

public interface IHabitService
{
    Task<IReadOnlyList<HabitDto>> ListAsync(bool includeArchived, CancellationToken ct = default);
    Task<HabitDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<HabitDto> CreateAsync(CreateHabitRequest request, CancellationToken ct = default);
    Task<HabitDto> UpdateAsync(Guid id, UpdateHabitRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<HabitDto> SetArchivedAsync(Guid id, bool archived, CancellationToken ct = default);
    Task<HabitDto> CheckInAsync(Guid id, DateOnly? day, CancellationToken ct = default);
    Task<HabitDto> UndoCheckInAsync(Guid id, DateOnly day, CancellationToken ct = default);
    Task<HabitStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default);
}
