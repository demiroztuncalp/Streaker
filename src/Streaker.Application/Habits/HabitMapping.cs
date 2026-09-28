using Streaker.Application.Contracts;
using Streaker.Domain.Entities;

namespace Streaker.Application.Habits;

internal static class HabitMapping
{
    public static HabitDto ToDto(this Habit habit, DateOnly today)
    {
        var streak = habit.GetStreak(today);
        return new HabitDto(
            habit.Id, habit.Name, habit.Description, habit.Emoji,
            streak.Current, streak.Longest,
            habit.IsDone(today), habit.Completions.Count,
            habit.IsArchived, habit.CreatedAt);
    }

    public static HabitStatsDto ToStats(this Habit habit, DateOnly today)
    {
        var streak = habit.GetStreak(today);
        var last30 = Enumerable.Range(0, 30)
            .Select(offset => today.AddDays(-(29 - offset)))
            .Select(day => new DayStatusDto(day, habit.IsDone(day)))
            .ToList();

        // GitHub-contribution-graph style strip of the last 4 weeks.
        var heatmap = string.Concat(last30.TakeLast(28).Select(d => d.Done ? "🟩" : "⬜"));

        return new HabitStatsDto(
            habit.Id, habit.Name, streak.Current, streak.Longest, habit.Completions.Count,
            Math.Round(habit.CompletionRate(today, 7), 2),
            Math.Round(habit.CompletionRate(today, 30), 2),
            last30, heatmap);
    }
}
