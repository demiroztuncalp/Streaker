namespace Streaker.Domain.Entities;

/// <summary>A single day on which a habit was checked in. Identity is (HabitId, Date).</summary>
public sealed class Completion
{
    private Completion() { } // EF Core

    internal Completion(Guid habitId, DateOnly date)
    {
        HabitId = habitId;
        Date = date;
    }

    public Guid HabitId { get; private set; }
    public DateOnly Date { get; private set; }
}
