using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(shift => shift.Id);

        builder.Property(shift => shift.Code).HasMaxLength(16).IsRequired();
        builder.Property(shift => shift.Name).HasMaxLength(64).IsRequired();

        // TimeOnly maps to `time`, TimeSpan to `interval` — both native in PostgreSQL.
        builder.Property(shift => shift.StartLocal).IsRequired();
        builder.Property(shift => shift.Duration).IsRequired();

        // Stored as the flags integer, which keeps day filtering a bitwise test rather
        // than a join to a day table.
        builder.Property(shift => shift.Days)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(shift => new { shift.LineId, shift.Code }).IsUnique();

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_shifts_duration_under_a_day",
            "duration > interval '0' AND duration < interval '24 hours'"));

        builder.HasMany(shift => shift.PlannedDowntimes)
            .WithOne(downtime => downtime.Shift)
            .HasForeignKey(downtime => downtime.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(nameof(Shift.ToDefinition));
    }
}
