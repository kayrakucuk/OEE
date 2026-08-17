using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Oee.Domain;
using Oee.Domain.Entities;

namespace Oee.Persistence.Tests;

/// <summary>
/// Checks the seed by inspecting the EF model, which is built without ever opening a
/// connection — so these run in CI with no database.
/// </summary>
public class SeedDataTests : IDisposable
{
    private readonly OeeDbContext _context;
    private readonly IModel _model;

    public SeedDataTests()
    {
        // A syntactically valid connection string is enough to build the model; nothing
        // here connects.
        DbContextOptions<OeeDbContext> options = new DbContextOptionsBuilder<OeeDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only;Username=none;Password=none")
            .Options;

        _context = new OeeDbContext(options);

        // The runtime model is read-optimised and drops seed data; the design-time model
        // is the one migrations are scaffolded from, so it is also the one worth asserting.
        _model = _context.GetService<IDesignTimeModel>().Model;
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private IReadOnlyList<IDictionary<string, object?>> SeedFor<TEntity>()
        where TEntity : class
    {
        IEntityType entityType = _model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not in the model.");

        return [.. entityType.GetSeedData()];
    }

    [Theory]
    [InlineData(typeof(Plant), 1)]
    [InlineData(typeof(Line), 2)]
    [InlineData(typeof(Machine), 6)]
    [InlineData(typeof(Shift), 3)]
    [InlineData(typeof(Product), 4)]
    [InlineData(typeof(ReasonCode), 12)]
    public void The_seed_has_the_agreed_row_counts(Type entityClrType, int expected)
    {
        IEntityType entityType = _model.FindEntityType(entityClrType)!;

        entityType.GetSeedData().Should().HaveCount(expected);
    }

    [Fact]
    public void Every_planned_reason_code_has_no_loss_category()
    {
        var planned = SeedFor<ReasonCode>()
            .Where(row => (bool)row["IsPlanned"]!)
            .ToList();

        planned.Should().NotBeEmpty();
        planned.Should().OnlyContain(row => row["SixBigLossCategory"] == null,
            "planned downtime is subtracted before OEE, so it is not one of the six losses");
    }

    [Fact]
    public void Every_unplanned_reason_code_has_a_loss_category()
    {
        var unplanned = SeedFor<ReasonCode>()
            .Where(row => !(bool)row["IsPlanned"]!)
            .ToList();

        unplanned.Should().NotBeEmpty();
        unplanned.Should().OnlyContain(row => row["SixBigLossCategory"] != null,
            "an uncategorised stop would vanish from the Pareto chart");
    }

    [Fact]
    public void All_six_big_losses_have_at_least_one_reason_code()
    {
        var covered = SeedFor<ReasonCode>()
            .Select(row => row["SixBigLossCategory"])
            .OfType<LossCategory>()
            .Distinct()
            .ToList();

        covered.Should().BeEquivalentTo(Enum.GetValues<LossCategory>());
    }

    [Fact]
    public void Reason_code_business_keys_are_unique()
    {
        var codes = SeedFor<ReasonCode>().Select(row => (string)row["Code"]!).ToList();

        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Each_line_has_exactly_one_bottleneck_machine()
    {
        var byLine = SeedFor<Machine>()
            .GroupBy(row => (int)row["LineId"]!)
            .ToList();

        byLine.Should().HaveCount(2);
        byLine.Should().OnlyContain(line => line.Count(row => (bool)row["IsBottleneck"]!) == 1);
    }

    [Fact]
    public void Machines_are_sequenced_from_one_within_each_line()
    {
        foreach (var line in SeedFor<Machine>().GroupBy(row => (int)row["LineId"]!))
        {
            line.Select(row => (int)row["SequenceInLine"]!)
                .OrderBy(sequence => sequence)
                .Should().Equal(1, 2, 3);
        }
    }

    [Fact]
    public void Every_product_has_a_positive_ideal_cycle_time()
    {
        SeedFor<Product>().Should()
            .OnlyContain(row => (double)row["IdealCycleTimeSec"]! > 0,
                "it is the denominator of Performance");
    }

    [Fact]
    public void The_seeded_shifts_tile_a_full_day_without_overlapping()
    {
        var shifts = SeedFor<Shift>()
            .Select(row => new
            {
                Start = (TimeOnly)row["StartLocal"]!,
                Duration = (TimeSpan)row["Duration"]!
            })
            .OrderBy(shift => shift.Start)
            .ToList();

        shifts.Sum(shift => shift.Duration.TotalHours).Should().Be(24);

        for (int i = 1; i < shifts.Count; i++)
        {
            shifts[i - 1].Start.Add(shifts[i - 1].Duration).Should().Be(shifts[i].Start,
                "each shift should hand over to the next with no gap");
        }
    }

    [Fact]
    public void The_seeded_shifts_are_resolvable_and_round_trip_through_the_resolver()
    {
        var definitions = SeedFor<Shift>()
            .Select(row => new ShiftDefinition(
                (int)row["Id"]!,
                (TimeOnly)row["StartLocal"]!,
                (TimeSpan)row["Duration"]!,
                (WeekDays)row["Days"]!))
            .ToList();

        string timeZoneId = (string)SeedFor<Plant>().Single()["TimeZoneId"]!;
        var resolver = new ShiftResolver(definitions, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));

        // Friday 09:00 local should land in the morning shift.
        DateTimeOffset instant =
            new DateTimeOffset(2026, 7, 31, 9, 0, 0, TimeSpan.FromHours(3)).ToUniversalTime();

        ShiftAssignment? assignment = resolver.Resolve(instant);

        assignment.Should().NotBeNull();
        assignment!.Value.ShiftDate.Should().Be(new DateOnly(2026, 7, 31));
        assignment.Value.ActualLength.Should().Be(TimeSpan.FromHours(8));
    }

    [Fact]
    public void The_plant_time_zone_is_a_real_time_zone()
    {
        string timeZoneId = (string)SeedFor<Plant>().Single()["TimeZoneId"]!;

        Action act = () => TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        act.Should().NotThrow("shift resolution is impossible without it");
    }

    [Fact]
    public void Every_planned_downtime_points_at_a_planned_reason_code()
    {
        var plannedReasonIds = SeedFor<ReasonCode>()
            .Where(row => (bool)row["IsPlanned"]!)
            .Select(row => (int)row["Id"]!)
            .ToHashSet();

        var downtimes = SeedFor<PlannedDowntime>();

        downtimes.Should().NotBeEmpty();
        downtimes.Should().OnlyContain(row => plannedReasonIds.Contains((int)row["ReasonCodeId"]!),
            "a break charged to an unplanned code would hit Availability instead of planned time");
    }

    [Fact]
    public void Tables_and_columns_are_snake_case()
    {
        IEntityType downtime = _model.FindEntityType(typeof(PlannedDowntime))!;

        downtime.GetTableName().Should().Be("planned_downtimes");
        downtime.GetProperty(nameof(PlannedDowntime.ReasonCodeId))
            .GetColumnName().Should().Be("reason_code_id");
        downtime.GetProperty(nameof(PlannedDowntime.EffectiveFrom))
            .GetColumnName().Should().Be("effective_from");
    }
}
