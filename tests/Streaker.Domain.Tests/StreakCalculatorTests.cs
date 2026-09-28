using Streaker.Domain.Entities;

namespace Streaker.Domain.Tests;

public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 5, 20);

    private static DateOnly Ago(int days) => Today.AddDays(-days);

    [Fact]
    public void No_check_ins_means_no_streak()
    {
        var streak = StreakCalculator.Calculate([], Today);

        Assert.Equal(0, streak.Current);
        Assert.Equal(0, streak.Longest);
    }

    [Fact]
    public void Consecutive_days_ending_today_count_fully()
    {
        var streak = StreakCalculator.Calculate([Ago(0), Ago(1), Ago(2)], Today);

        Assert.Equal(3, streak.Current);
        Assert.Equal(3, streak.Longest);
    }

    [Fact]
    public void Streak_stays_alive_when_today_is_still_open()
    {
        var streak = StreakCalculator.Calculate([Ago(1), Ago(2), Ago(3)], Today);

        Assert.Equal(3, streak.Current);
    }

    [Fact]
    public void Missing_yesterday_and_today_breaks_the_current_streak()
    {
        var streak = StreakCalculator.Calculate([Ago(2), Ago(3), Ago(4)], Today);

        Assert.Equal(0, streak.Current);
        Assert.Equal(3, streak.Longest);
    }

    [Fact]
    public void Longest_streak_is_remembered_after_a_gap()
    {
        DateOnly[] days = [Ago(0), Ago(1), Ago(5), Ago(6), Ago(7), Ago(8)];

        var streak = StreakCalculator.Calculate(days, Today);

        Assert.Equal(2, streak.Current);
        Assert.Equal(4, streak.Longest);
    }

    [Fact]
    public void Duplicate_days_are_counted_once()
    {
        var streak = StreakCalculator.Calculate([Ago(0), Ago(0), Ago(1)], Today);

        Assert.Equal(2, streak.Current);
    }
}
