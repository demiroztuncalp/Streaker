using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaker.Domain.Entities;

namespace Streaker.Infrastructure.Persistence.Configurations;

internal sealed class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.ToTable("Habits");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.Name).HasMaxLength(Habit.MaxNameLength).IsRequired();
        builder.Property(h => h.Description).HasMaxLength(Habit.MaxDescriptionLength);
        builder.Property(h => h.Emoji).HasMaxLength(16).IsRequired();
        builder.Ignore(h => h.IsArchived);

        // The aggregate exposes a read-only view; EF fills the private list directly.
        builder.HasMany(h => h.Completions).WithOne().HasForeignKey(c => c.HabitId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(h => h.Completions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CompletionConfiguration : IEntityTypeConfiguration<Completion>
{
    public void Configure(EntityTypeBuilder<Completion> builder)
    {
        builder.ToTable("Completions");
        builder.HasKey(c => new { c.HabitId, c.Date });
        builder.Property(c => c.HabitId).ValueGeneratedNever();
        builder.Property(c => c.Date).ValueGeneratedNever();
    }
}
