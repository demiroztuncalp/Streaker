using Streaker.Domain.ValueObjects;

namespace Streaker.Domain.Entities;

/// <summary>Pure streak maths, kept separate so it is trivial to test.</summary>
public static class StreakCalculator
{
    public static Streak Calculate(IEnumerable<DateOnly> completedDays, DateOnly today)
    {
        var days = completedDays.ToHashSet();
        if (days.Count == 0) return Streak.None;

        // Longest run: only start counting from days that begin a run.
        var longest = 0;
        foreach (var day in days.Where(d => !days.Contains(d.AddDays(-1))))
        {
            var length = 1;
            while (days.Contains(day.AddDays(length))) length++;
            longest = Math.Max(longest, length);
        }

        // Current run: alive if done today, or done yesterday and today is still open.
        var cursor = days.Contains(today) ? today
                   : days.Contains(today.AddDays(-1)) ? today.AddDays(-1)
                   : (DateOnly?)null;

        var current = 0;
        while (cursor is { } d && days.Contains(d))
        {
            current++;
            cursor = d.AddDays(-1);
        }

        return new Streak(current, longest);
    }
}
