using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.HasKey(machine => machine.Id);

        builder.Property(machine => machine.Code).HasMaxLength(32).IsRequired();
        builder.Property(machine => machine.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(machine => new { machine.LineId, machine.Code }).IsUnique();
        builder.HasIndex(machine => new { machine.LineId, machine.SequenceInLine });

        // A line has exactly one constraint machine. Enforced in the database because the
        // reported line OEE is meaningless if two machines claim the title.
        builder.HasIndex(machine => machine.LineId)
            .IsUnique()
            .HasFilter("is_bottleneck")
            .HasDatabaseName("ux_machines_one_bottleneck_per_line");
    }
}
