using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Streaker.Domain.Entities;

namespace Streaker.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Creates the schema and, optionally, fills an empty database with demo data.</summary>
    public static async Task InitializeAsync(IServiceProvider services, bool seed)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StreakerDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        await db.Database.EnsureCreatedAsync();

        if (seed && !await db.Habits.AnyAsync())
        {
            db.Habits.AddRange(DemoData.Create(clock.GetUtcNow()));
            await db.SaveChangesAsync();
        }
    }
}

/// <summary>A believable set of habits so a fresh clone has something to look at.</summary>
internal static class DemoData
{
    public static IEnumerable<Habit> Create(DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        // (name, description, emoji, "is done N days ago?")
        var plans = new (string Name, string Description, string Emoji, Func<int, bool> Done)[]
        {
            ("Read 20 pages", "One chapter a day keeps the doomscroll away.", "📚", ago => ago is < 12 or (> 14 and < 26)),
            ("Morning run", "5 km before breakfast.", "🏃", ago => ago % 3 != 0 && ago > 0),
            ("Write code", "Commit something, however small.", "💻", ago => ago is < 21),
            ("Drink 2L of water", "Hydrate or diedrate.", "💧", ago => ago % 5 != 4),
            ("Meditate", "Ten quiet minutes.", "🧘", ago => ago is > 0 and < 5 or (> 9 and < 13)),
        };

        foreach (var (name, description, emoji, done) in plans)
        {
            var habit = Habit.Create(name, description, emoji, now.AddDays(-30));
            for (var ago = 29; ago >= 0; ago--)
                if (done(ago)) habit.CheckIn(today.AddDays(-ago), today);
            yield return habit;
        }
    }
}
