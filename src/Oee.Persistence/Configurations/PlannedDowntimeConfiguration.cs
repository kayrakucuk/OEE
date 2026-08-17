using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class PlannedDowntimeConfiguration : IEntityTypeConfiguration<PlannedDowntime>
{
    public void Configure(EntityTypeBuilder<PlannedDowntime> builder)
    {
        builder.HasKey(downtime => downtime.Id);

        builder.Property(downtime => downtime.StartLocal).IsRequired();
        builder.Property(downtime => downtime.Duration).IsRequired();
        builder.Property(downtime => downtime.Days).HasConversion<int>().IsRequired();

        builder.HasOne(downtime => downtime.Machine)
            .WithMany()
            .HasForeignKey(downtime => downtime.MachineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(downtime => downtime.ReasonCode)
            .WithMany()
            .HasForeignKey(downtime => downtime.ReasonCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(downtime => downtime.ShiftId);

        builder.ToTable(table => table.HasCheckConstraint(
            "ck_planned_downtimes_duration_positive",
            "duration > interval '0'"));

        // A window that ends before it starts would silently subtract nothing.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_planned_downtimes_effective_window_ordered",
            @"effective_from IS NULL OR effective_to IS NULL OR effective_from <= effective_to"));
    }
}
