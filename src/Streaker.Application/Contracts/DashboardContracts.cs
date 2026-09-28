namespace Streaker.Application.Contracts;

public sealed record LeaderboardEntryDto(Guid HabitId, string Name, string Emoji, int CurrentStreak, int LongestStreak);

public sealed record DashboardDto(
    DateOnly Today,
    int ActiveHabits,
    int CompletedToday,
    int PendingToday,
    int TodayProgressPercent,
    string Message,
    IReadOnlyList<LeaderboardEntryDto> Leaderboard);
