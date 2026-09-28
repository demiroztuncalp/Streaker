using Streaker.Domain.Exceptions;
using Streaker.Domain.ValueObjects;

namespace Streaker.Domain.Entities;

/// <summary>
/// Aggregate root. Owns its check-ins and guarantees every invariant around them.
/// </summary>
public sealed class Habit
{
    public const int MaxNameLength = 60;
    public const int MaxDescriptionLength = 240;

    private readonly List<Completion> _completions = [];

    private Habit() { } // EF Core

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Emoji { get; private set; } = "✨";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    public IReadOnlyCollection<Completion> Completions => _completions;
    public bool IsArchived => ArchivedAt is not null;

    public static Habit Create(string name, string? description, string? emoji, DateTimeOffset now)
    {
        var habit = new Habit { Id = Guid.NewGuid(), CreatedAt = now };
        habit.Update(name, description, emoji);
        return habit;
    }

    public void Update(string name, string? description, string? emoji)
    {
        name = name?.Trim() ?? string.Empty;
        if (name.Length == 0)
            throw new DomainException("A habit needs a name.");
        if (name.Length > MaxNameLength)
            throw new DomainException($"Habit name cannot exceed {MaxNameLength} characters.");

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > MaxDescriptionLength)
            throw new DomainException($"Description cannot exceed {MaxDescriptionLength} characters.");

        Name = name;
        Description = description;
        Emoji = string.IsNullOrWhiteSpace(emoji) ? "✨" : emoji.Trim();
    }

    public void Archive(DateTimeOffset now) => ArchivedAt ??= now;

    public void Restore() => ArchivedAt = null;

    /// <returns><c>true</c> if a new check-in was recorded, <c>false</c> if the day was already done.</returns>
    public bool CheckIn(DateOnly date, DateOnly today)
    {
        if (IsArchived)
            throw new DomainException("Archived habits cannot be checked in. Restore it first.");
        if (date > today)
            throw new DomainException("You cannot check in for a day in the future.");
        if (date < DateOnly.FromDateTime(CreatedAt.UtcDateTime).AddDays(-30))
            throw new DomainException("You cannot check in more than 30 days before the habit was created.");
        if (IsDone(date))
            return false;

        _completions.Add(new Completion(Id, date));
        return true;
    }

    /// <returns><c>true</c> if a check-in was removed.</returns>
    public bool UndoCheckIn(DateOnly date)
    {
        var existing = _completions.Find(c => c.Date == date);
        return existing is not null && _completions.Remove(existing);
    }

    public bool IsDone(DateOnly date) => _completions.Any(c => c.Date == date);

    public Streak GetStreak(DateOnly today) => StreakCalculator.Calculate(_completions.Select(c => c.Date), today);

    /// <summary>Share of days completed in the last <paramref name="days"/> days (0..1).</summary>
    public double CompletionRate(DateOnly today, int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);

        var windowStart = today.AddDays(-(days - 1));
        var createdOn = DateOnly.FromDateTime(CreatedAt.UtcDateTime);
        var effectiveStart = createdOn > windowStart ? createdOn : windowStart;
        var span = today.DayNumber - effectiveStart.DayNumber + 1;
        if (span <= 0) return 0;

        var done = _completions.Count(c => c.Date >= effectiveStart && c.Date <= today);
        return Math.Min(1.0, (double)done / span);
    }
}
