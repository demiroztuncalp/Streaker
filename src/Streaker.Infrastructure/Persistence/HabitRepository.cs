using Microsoft.EntityFrameworkCore;
using Streaker.Application.Abstractions;
using Streaker.Domain.Entities;

namespace Streaker.Infrastructure.Persistence;

internal sealed class HabitRepository(StreakerDbContext db) : IHabitRepository
{
    public Task<Habit?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Habits.Include(h => h.Completions).FirstOrDefaultAsync(h => h.Id == id, ct);

    public async Task<IReadOnlyList<Habit>> ListAsync(bool includeArchived, CancellationToken ct = default)
    {
        var query = db.Habits.Include(h => h.Completions).AsSplitQuery();
        if (!includeArchived) query = query.Where(h => h.ArchivedAt == null);
        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Habit habit, CancellationToken ct = default) => await db.Habits.AddAsync(habit, ct);

    public void Remove(Habit habit) => db.Habits.Remove(habit);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
