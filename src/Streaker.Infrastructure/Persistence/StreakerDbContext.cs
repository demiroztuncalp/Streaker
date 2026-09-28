using Microsoft.EntityFrameworkCore;
using Streaker.Domain.Entities;

namespace Streaker.Infrastructure.Persistence;

public sealed class StreakerDbContext(DbContextOptions<StreakerDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits => Set<Habit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(StreakerDbContext).Assembly);
}
