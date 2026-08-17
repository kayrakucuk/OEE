namespace Oee.Domain.Tests;

public class LossBreakdownTests
{
    [Theory]
    [InlineData(LossCategory.Breakdowns, OeeFactor.Availability)]
    [InlineData(LossCategory.SetupAndAdjustments, OeeFactor.Availability)]
    [InlineData(LossCategory.IdlingAndMinorStops, OeeFactor.Performance)]
    [InlineData(LossCategory.ReducedSpeed, OeeFactor.Performance)]
    [InlineData(LossCategory.ProcessDefects, OeeFactor.Quality)]
    [InlineData(LossCategory.StartupRejects, OeeFactor.Quality)]
    public void Each_loss_maps_to_its_oee_factor(LossCategory category, OeeFactor expected)
    {
        LossBreakdown.FactorFor(category).Should().Be(expected);
    }

    [Fact]
    public void Every_loss_category_is_mapped()
    {
        Action act = () =>
        {
            foreach (LossCategory category in Enum.GetValues<LossCategory>())
            {
                _ = LossBreakdown.FactorFor(category);
            }
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void The_indexer_agrees_with_the_properties()
    {
        var losses = new LossBreakdown(
            breakdowns: TimeSpan.FromMinutes(1),
            setupAndAdjustments: TimeSpan.FromMinutes(2),
            idlingAndMinorStops: TimeSpan.FromMinutes(3),
            reducedSpeed: TimeSpan.FromMinutes(4),
            processDefects: TimeSpan.FromMinutes(5),
            startupRejects: TimeSpan.FromMinutes(6));

        losses[LossCategory.Breakdowns].Should().Be(losses.Breakdowns);
        losses[LossCategory.SetupAndAdjustments].Should().Be(losses.SetupAndAdjustments);
        losses[LossCategory.IdlingAndMinorStops].Should().Be(losses.IdlingAndMinorStops);
        losses[LossCategory.ReducedSpeed].Should().Be(losses.ReducedSpeed);
        losses[LossCategory.ProcessDefects].Should().Be(losses.ProcessDefects);
        losses[LossCategory.StartupRejects].Should().Be(losses.StartupRejects);

        losses.Total.Should().Be(TimeSpan.FromMinutes(21));
    }

    [Fact]
    public void An_unknown_category_is_rejected()
    {
        var losses = new LossBreakdown(
            TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);

        Action indexer = () => _ = losses[(LossCategory)99];
        Action factor = () => _ = LossBreakdown.FactorFor((LossCategory)99);

        indexer.Should().Throw<ArgumentOutOfRangeException>();
        factor.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(MachineState.Unknown, true)]
    [InlineData(MachineState.Off, true)]
    [InlineData(MachineState.PlannedStop, true)]
    [InlineData(MachineState.Breakdown, false)]
    [InlineData(MachineState.Setup, false)]
    [InlineData(MachineState.Idle, false)]
    [InlineData(MachineState.Running, false)]
    public void States_know_whether_they_count_towards_planned_time(
        MachineState state,
        bool excluded)
    {
        state.IsExcludedFromPlannedTime().Should().Be(excluded);
    }

    [Theory]
    [InlineData(MachineState.Breakdown, true)]
    [InlineData(MachineState.Setup, true)]
    [InlineData(MachineState.Idle, true)]
    [InlineData(MachineState.Running, false)]
    [InlineData(MachineState.PlannedStop, false)]
    public void States_know_whether_they_are_a_scheduled_stop(MachineState state, bool isStop)
    {
        state.IsScheduledStop().Should().Be(isStop);
    }

    [Fact]
    public void Tallies_add_up_category_by_category()
    {
        var morning = new ProductionTally(100, 5, 2);
        var afternoon = new ProductionTally(80, 3, 1);

        ProductionTally shift = morning + afternoon;

        shift.GoodCount.Should().Be(180);
        shift.ProcessDefectCount.Should().Be(8);
        shift.StartupRejectCount.Should().Be(3);
        shift.ScrapCount.Should().Be(11);
        shift.TotalCount.Should().Be(191);
    }
}
