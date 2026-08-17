using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Code).HasMaxLength(32).IsRequired();
        builder.Property(product => product.Name).HasMaxLength(128).IsRequired();
        builder.Property(product => product.IdealCycleTimeSec).IsRequired();

        builder.HasIndex(product => product.Code).IsUnique();

        // A zero or negative cycle time would divide by zero in the Performance factor.
        // Raw SQL, so it names the post-rename snake_case column.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_products_ideal_cycle_time_positive",
            "ideal_cycle_time_sec > 0"));

        // Computed from IdealCycleTimeSec — nothing to store.
        builder.Ignore(product => product.IdealCycleTime);
    }
}
