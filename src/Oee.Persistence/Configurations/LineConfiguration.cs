using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class LineConfiguration : IEntityTypeConfiguration<Line>
{
    public void Configure(EntityTypeBuilder<Line> builder)
    {
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Code).HasMaxLength(32).IsRequired();
        builder.Property(line => line.Name).HasMaxLength(128).IsRequired();

        // Line codes only need to be unique within their plant.
        builder.HasIndex(line => new { line.PlantId, line.Code }).IsUnique();

        builder.HasMany(line => line.Machines)
            .WithOne(machine => machine.Line)
            .HasForeignKey(machine => machine.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(line => line.Shifts)
            .WithOne(shift => shift.Line)
            .HasForeignKey(shift => shift.LineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
