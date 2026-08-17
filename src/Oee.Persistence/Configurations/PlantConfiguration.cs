using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.HasKey(plant => plant.Id);

        builder.Property(plant => plant.Code).HasMaxLength(32).IsRequired();
        builder.Property(plant => plant.Name).HasMaxLength(128).IsRequired();
        builder.Property(plant => plant.TimeZoneId).HasMaxLength(64).IsRequired();

        builder.HasIndex(plant => plant.Code).IsUnique();

        builder.HasMany(plant => plant.Lines)
            .WithOne(line => line.Plant)
            .HasForeignKey(line => line.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
