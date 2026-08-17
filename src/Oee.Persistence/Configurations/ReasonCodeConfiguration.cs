using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oee.Domain;
using Oee.Domain.Entities;

namespace Oee.Persistence.Configurations;

internal sealed class ReasonCodeConfiguration : IEntityTypeConfiguration<ReasonCode>
{
    public void Configure(EntityTypeBuilder<ReasonCode> builder)
    {
        builder.HasKey(reason => reason.Id);

        builder.Property(reason => reason.Code).HasMaxLength(32).IsRequired();
        builder.Property(reason => reason.Description).HasMaxLength(256).IsRequired();

        builder.Property(reason => reason.SixBigLossCategory).HasConversion<int?>();

        builder.HasIndex(reason => reason.Code).IsUnique();

        // The invariant that keeps loss attribution coherent: planned stops are subtracted
        // before OEE is calculated, so they are not one of the losses OEE explains — and
        // an unplanned stop with no category would vanish from the Pareto chart.
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_reason_codes_category_matches_planned_flag",
            @"(is_planned AND six_big_loss_category IS NULL)
              OR (NOT is_planned AND six_big_loss_category IS NOT NULL)"));
    }
}
