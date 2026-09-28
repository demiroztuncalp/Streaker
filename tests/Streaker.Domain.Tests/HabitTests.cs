using Streaker.Domain.Entities;
using Streaker.Domain.Exceptions;

namespace Streaker.Domain.Tests;

public class HabitTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 5, 20);

    private static Habit NewHabit() => Habit.Create("Read", null, "📚", Now);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_habit_requires_a_name(string name)
        => Assert.Throws<DomainException>(() => Habit.Create(name, null, null, Now));

    [Fact]
    public void Name_is_trimmed_and_emoji_has_a_default()
    {
        var habit = Habit.Create("  Stretch  ", "  ", null, Now);

        Assert.Equal("Stretch", habit.Name);
        Assert.Null(habit.Description);
        Assert.Equal("✨", habit.Emoji);
    }

    [Fact]
    public void Checking_in_twice_on_the_same_day_is_idempotent()
    {
        var habit = NewHabit();

        Assert.True(habit.CheckIn(Today, Today));
        Assert.False(habit.CheckIn(Today, Today));
        Assert.Single(habit.Completions);
    }

    [Fact]
    public void Future_check_ins_are_rejected()
        => Assert.Throws<DomainException>(() => NewHabit().CheckIn(Today.AddDays(1), Today));

    [Fact]
    public void Archived_habits_cannot_be_checked_in_until_restored()
    {
        var habit = NewHabit();
        habit.Archive(Now);

        Assert.Throws<DomainException>(() => habit.CheckIn(Today, Today));

        habit.Restore();
        Assert.True(habit.CheckIn(Today, Today));
    }

    [Fact]
    public void Undo_removes_only_the_requested_day()
    {
        var habit = NewHabit();
        habit.CheckIn(Today, Today);
        habit.CheckIn(Today.AddDays(-1), Today);

        Assert.True(habit.UndoCheckIn(Today));
        Assert.False(habit.UndoCheckIn(Today));
        Assert.True(habit.IsDone(Today.AddDays(-1)));
    }

    [Fact]
    public void Completion_rate_ignores_days_before_the_habit_existed()
    {
        var habit = NewHabit(); // created today
        habit.CheckIn(Today, Today);

        Assert.Equal(1.0, habit.CompletionRate(Today, 30));
    }
}
