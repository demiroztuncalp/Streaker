namespace Streaker.Application.Contracts;

public sealed record CreateHabitRequest(string Name, string? Description, string? Emoji);

public sealed record UpdateHabitRequest(string Name, string? Description, string? Emoji);

/// <param name="Date">Day to check in for. Defaults to today.</param>
public sealed record CheckInRequest(DateOnly? Date);

public sealed record HabitDto(
    Guid Id,
    string Name,
    string? Description,
    string Emoji,
    int CurrentStreak,
    int LongestStreak,
    bool DoneToday,
    int TotalCheckIns,
    bool IsArchived,
    DateTimeOffset CreatedAt);

public sealed record DayStatusDto(DateOnly Date, bool Done);

public sealed record HabitStatsDto(
    Guid HabitId,
    string Name,
    int CurrentStreak,
    int LongestStreak,
    int TotalCheckIns,
    double CompletionRateLast7Days,
    double CompletionRateLast30Days,
    IReadOnlyList<DayStatusDto> Last30Days,
    string Heatmap);
