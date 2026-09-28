namespace Streaker.Domain.ValueObjects;

/// <summary>Snapshot of a habit's momentum.</summary>
/// <param name="Current">Consecutive days ending today (or yesterday, if today is still open).</param>
/// <param name="Longest">Best run of consecutive days ever achieved.</param>
public readonly record struct Streak(int Current, int Longest)
{
    public static Streak None => new(0, 0);
}
