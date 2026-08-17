using Microsoft.EntityFrameworkCore;
using Oee.Domain.Entities;

namespace Oee.Persistence;

/// <summary>
/// The master-data model: plants, lines, machines, products, shifts and reason codes.
/// </summary>
/// <remarks>
/// Phase 1 stores configuration only. Signals, downtime events and shift aggregates arrive
/// in later phases and will live in their own tables.
/// </remarks>
public class OeeDbContext : DbContext
{
    public OeeDbContext(DbContextOptions<OeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Plant> Plants => Set<Plant>();

    public DbSet<Line> Lines => Set<Line>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<PlannedDowntime> PlannedDowntimes => Set<PlannedDowntime>();

    public DbSet<ReasonCode> ReasonCodes => Set<ReasonCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OeeDbContext).Assembly);
        SeedData.Apply(modelBuilder);

        NamingConventions.UseSnakeCase(modelBuilder);
    }
}
