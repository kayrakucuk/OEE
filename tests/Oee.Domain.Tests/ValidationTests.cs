using Oee.Domain.Entities;

namespace Oee.Domain.Tests;

/// <summary>
/// Guards on the value types that survive from Phase 0 and on the entity behaviour that
/// is not just property storage.
/// </summary>
public class ValidationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_segment_cannot_end_before_it_starts()
    {
        Action act = () => _ = new StateSegment(MachineState.Running, T0, T0.AddMinutes(-1));

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("end");
    }

    [Fact]
    public void A_zero_length_segment_is_allowed()
    {
        new StateSegment(MachineState.Running, T0, T0).Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void A_segment_reports_its_duration()
    {
        StateSegment.For(MachineState.Running, T0, TimeSpan.FromMinutes(90))
            .End.Should().Be(T0.AddMinutes(90));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Production_counts_must_not_be_negative(long good, long defects, long rejects)
    {
        Action act = () => _ = new ProductionTally(good, defects, rejects);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ------------------------------------------------------------------ entities

    [Fact]
    public void A_shift_projects_to_the_definition_the_resolver_consumes()
    {
        var shift = new Shift
        {
            Id = 7,
            Code = "C",
            Name = "Night",
            StartLocal = new TimeOnly(22, 0),
            Duration = TimeSpan.FromHours(8),
            Days = WeekDays.Weekdays
        };

        shift.ToDefinition().Should()
            .Be(new ShiftDefinition(7, new TimeOnly(22, 0), TimeSpan.FromHours(8), WeekDays.Weekdays));
    }

    [Fact]
    public void A_product_exposes_its_cycle_time_as_a_timespan()
    {
        var product = new Product { Code = "PRD-100", Name = "Housing", IdealCycleTimeSec = 1.5 };

        product.IdealCycleTime.Should().Be(TimeSpan.FromSeconds(1.5));
    }

    [Fact]
    public void A_recurring_planned_downtime_applies_only_on_its_days()
    {
        var breakTime = new PlannedDowntime { Days = WeekDays.Weekdays };

        breakTime.AppliesOn(new DateOnly(2026, 7, 31)).Should().BeTrue("that is a Friday");
        breakTime.AppliesOn(new DateOnly(2026, 8, 1)).Should().BeFalse("that is a Saturday");
    }

    [Fact]
    public void A_dated_planned_downtime_applies_only_within_its_window()
    {
        var maintenance = new PlannedDowntime
        {
            Days = WeekDays.All,
            EffectiveFrom = new DateOnly(2026, 8, 10),
            EffectiveTo = new DateOnly(2026, 8, 12)
        };

        maintenance.AppliesOn(new DateOnly(2026, 8, 9)).Should().BeFalse();
        maintenance.AppliesOn(new DateOnly(2026, 8, 10)).Should().BeTrue();
        maintenance.AppliesOn(new DateOnly(2026, 8, 12)).Should().BeTrue();
        maintenance.AppliesOn(new DateOnly(2026, 8, 13)).Should().BeFalse();
    }

    [Fact]
    public void An_open_ended_planned_downtime_has_no_bounds()
    {
        var always = new PlannedDowntime { Days = WeekDays.All };

        always.AppliesOn(new DateOnly(2020, 1, 1)).Should().BeTrue();
        always.AppliesOn(new DateOnly(2099, 12, 31)).Should().BeTrue();
    }
}
