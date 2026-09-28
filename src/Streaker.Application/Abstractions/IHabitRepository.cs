using Streaker.Domain.Entities;

namespace Streaker.Application.Abstractions;

/// <summary>Port implemented by the Infrastructure layer.</summary>
public interface IHabitRepository
{
    Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Habit>> ListAsync(bool includeArchived, CancellationToken ct = default);
    Task AddAsync(Habit habit, CancellationToken ct = default);
    void Remove(Habit habit);
    Task SaveChangesAsync(CancellationToken ct = default);
}
