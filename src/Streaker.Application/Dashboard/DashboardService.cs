using Streaker.Application.Abstractions;
using Streaker.Application.Contracts;

namespace Streaker.Application.Dashboard;

internal sealed class DashboardService(IHabitRepository habits, TimeProvider clock) : IDashboardService
{
    public async Task<DashboardDto> GetAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var active = await habits.ListAsync(includeArchived: false, ct);

        var done = active.Count(h => h.IsDone(today));
        var pending = active.Count - done;
        var progress = active.Count == 0 ? 0 : (int)Math.Round(100.0 * done / active.Count);

        var leaderboard = active
            .Select(h => (Habit: h, Streak: h.GetStreak(today)))
            .OrderByDescending(x => x.Streak.Current)
            .ThenByDescending(x => x.Streak.Longest)
            .Take(3)
            .Select(x => new LeaderboardEntryDto(x.Habit.Id, x.Habit.Name, x.Habit.Emoji, x.Streak.Current, x.Streak.Longest))
            .ToList();

        return new DashboardDto(today, active.Count, done, pending, progress, MessageFor(active.Count, progress), leaderboard);
    }

    private static string MessageFor(int habitCount, int progress) => (habitCount, progress) switch
    {
        (0, _)    => "No habits yet — create your first one and start a streak! 🌱",
        (_, 100)  => "Perfect day! Every habit is done. 🏆",
        (_, >= 50) => "Over halfway there, keep the momentum going. 🔥",
        (_, > 0)  => "Good start — a few more to go. 💪",
        _         => "A fresh day. Pick one habit and get the streak rolling. ☀️"
    };
}
