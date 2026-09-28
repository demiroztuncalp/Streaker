using Streaker.Application.Abstractions;
using Streaker.Application.Contracts;
using Streaker.Application.Exceptions;
using Streaker.Domain.Entities;

namespace Streaker.Application.Habits;

internal sealed class HabitService(IHabitRepository habits, TimeProvider clock) : IHabitService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<IReadOnlyList<HabitDto>> ListAsync(bool includeArchived, CancellationToken ct = default)
    {
        var today = Today;
        var all = await habits.ListAsync(includeArchived, ct);
        return all.Select(h => h.ToDto(today)).OrderByDescending(h => h.CurrentStreak).ThenBy(h => h.Name).ToList();
    }

    public async Task<HabitDto> GetAsync(Guid id, CancellationToken ct = default)
        => (await Load(id, ct)).ToDto(Today);

    public async Task<HabitDto> CreateAsync(CreateHabitRequest request, CancellationToken ct = default)
    {
        var habit = Habit.Create(request.Name, request.Description, request.Emoji, clock.GetUtcNow());
        await habits.AddAsync(habit, ct);
        await habits.SaveChangesAsync(ct);
        return habit.ToDto(Today);
    }

    public async Task<HabitDto> UpdateAsync(Guid id, UpdateHabitRequest request, CancellationToken ct = default)
    {
        var habit = await Load(id, ct);
        habit.Update(request.Name, request.Description, request.Emoji);
        await habits.SaveChangesAsync(ct);
        return habit.ToDto(Today);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        habits.Remove(await Load(id, ct));
        await habits.SaveChangesAsync(ct);
    }

    public async Task<HabitDto> SetArchivedAsync(Guid id, bool archived, CancellationToken ct = default)
    {
        var habit = await Load(id, ct);
        if (archived) habit.Archive(clock.GetUtcNow()); else habit.Restore();
        await habits.SaveChangesAsync(ct);
        return habit.ToDto(Today);
    }

    public async Task<HabitDto> CheckInAsync(Guid id, DateOnly? day, CancellationToken ct = default)
    {
        var today = Today;
        var habit = await Load(id, ct);
        habit.CheckIn(day ?? today, today);
        await habits.SaveChangesAsync(ct);
        return habit.ToDto(today);
    }

    public async Task<HabitDto> UndoCheckInAsync(Guid id, DateOnly day, CancellationToken ct = default)
    {
        var habit = await Load(id, ct);
        habit.UndoCheckIn(day);
        await habits.SaveChangesAsync(ct);
        return habit.ToDto(Today);
    }

    public async Task<HabitStatsDto> GetStatsAsync(Guid id, CancellationToken ct = default)
        => (await Load(id, ct)).ToStats(Today);

    private async Task<Habit> Load(Guid id, CancellationToken ct)
        => await habits.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Habit), id);
}
