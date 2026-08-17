namespace Oee.Domain.Tests;

public class OeeCalculatorTests
{
    /// <summary>
    /// The locked reference case. Every number in this test is a fixed point — if a change
    /// moves any of them, the change is wrong until proven otherwise.
    /// </summary>
    /// <remarks>
    /// 480 minute shift, 60 minutes planned downtime, 47 minutes unplanned downtime,
    /// 1.0 second ideal cycle time, 19,271 pieces produced, 423 rejected.
    /// </remarks>
    public static OeeInput ReferenceCase => new(
        shiftLength: TimeSpan.FromMinutes(480),
        plannedDowntime: TimeSpan.FromMinutes(60),
        unplannedDowntime: TimeSpan.FromMinutes(47),
        idealCycleTime: TimeSpan.FromSeconds(1),
        totalCount: 19_271,
        rejectCount: 423);

    [Fact]
    public void Reference_case_matches_the_published_figures()
    {
        OeeResult result = OeeCalculator.Calculate(ReferenceCase);

        result.Availability.Should().BeApproximately(0.888, 0.0005);
        result.Performance.Should().BeApproximately(0.861, 0.0005);
        result.Quality.Should().BeApproximately(0.978, 0.0005);
        result.Oee.Should().BeApproximately(0.748, 0.0005);
    }

    [Fact]
    public void Reference_case_derives_the_expected_time_buckets()
    {
        OeeResult result = OeeCalculator.Calculate(ReferenceCase);

        result.PlannedProductionTime.Should().Be(TimeSpan.FromMinutes(420));
        result.RunTime.Should().Be(TimeSpan.FromMinutes(373));
        result.NetRunTime.Should().Be(TimeSpan.FromSeconds(19_271));
        result.FullyProductiveTime.Should().Be(TimeSpan.FromSeconds(18_848));
    }

    [Fact]
    public void Reference_case_is_clean_data()
    {
        OeeResult result = OeeCalculator.Calculate(ReferenceCase);

        result.DataQuality.Should().Be(OeeDataQuality.Ok);
        result.IsTrustworthy.Should().BeTrue();
    }

    /// <summary>
    /// The three factors should telescope: A × P × Q reduces to fully productive time over
    /// planned production time. Worth pinning separately, because it is the identity that
    /// breaks first if a denominator is wrong.
    /// </summary>
    [Fact]
    public void Oee_equals_fully_productive_time_over_planned_production_time()
    {
        OeeResult result = OeeCalculator.Calculate(ReferenceCase);

        double expected = result.FullyProductiveTime / result.PlannedProductionTime;

        result.Oee.Should().BeApproximately(expected, 1e-12);
    }

    [Fact]
    public void A_perfect_shift_scores_one()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.Zero,
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 28_800,
            rejectCount: 0));

        result.Availability.Should().Be(1d);
        result.Performance.Should().Be(1d);
        result.Quality.Should().Be(1d);
        result.Oee.Should().Be(1d);
        result.DataQuality.Should().Be(OeeDataQuality.Ok);
    }

    // ---------------------------------------------------------------- edge cases

    [Fact]
    public void Zero_production_scores_zero_and_is_flagged()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(60),
            unplannedDowntime: TimeSpan.FromMinutes(47),
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0));

        // The machine was up — availability is real — but nothing came off it.
        result.Availability.Should().BeApproximately(373d / 420d, 1e-9);
        result.Performance.Should().Be(0d);
        result.Quality.Should().Be(0d);
        result.Oee.Should().Be(0d);

        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoProduction);
    }

    [Fact]
    public void A_full_shift_of_downtime_scores_zero_and_is_flagged()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(60),
            unplannedDowntime: TimeSpan.FromMinutes(420),
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0));

        result.PlannedProductionTime.Should().Be(TimeSpan.FromMinutes(420));
        result.RunTime.Should().Be(TimeSpan.Zero);
        result.Availability.Should().Be(0d);
        result.Oee.Should().Be(0d);

        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoRunTime);
    }

    [Fact]
    public void Zero_planned_time_does_not_divide_by_zero()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(480),
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0));

        result.PlannedProductionTime.Should().Be(TimeSpan.Zero);
        result.Availability.Should().Be(0d);
        result.Performance.Should().Be(0d);
        result.Quality.Should().Be(0d);
        result.Oee.Should().Be(0d);

        result.Availability.Should().NotBe(double.NaN);
        result.Oee.Should().NotBe(double.NaN);
        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoPlannedTime);
    }

    [Fact]
    public void Planned_downtime_exceeding_the_shift_clamps_rather_than_going_negative()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(600),
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0));

        result.PlannedProductionTime.Should().Be(TimeSpan.Zero);
        result.RunTime.Should().Be(TimeSpan.Zero);
        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoPlannedTime);
    }

    [Fact]
    public void Unplanned_downtime_exceeding_planned_time_clamps_and_is_flagged()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(60),
            unplannedDowntime: TimeSpan.FromMinutes(500),
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 100,
            rejectCount: 0));

        // Availability would otherwise come out negative, which is worse than useless.
        result.RunTime.Should().Be(TimeSpan.Zero);
        result.Availability.Should().Be(0d);
        result.DataQuality.Should().HaveFlag(OeeDataQuality.DowntimeExceedsPlanned);
    }

    // ------------------------------------------------- performance data quality

    [Fact]
    public void Performance_above_one_is_reported_raw_and_flagged_not_thrown()
    {
        // Twice as many parts as a one-second cycle allows: the ideal cycle time is wrong.
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(60),
            plannedDowntime: TimeSpan.Zero,
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 7_200,
            rejectCount: 0));

        result.Performance.Should().BeApproximately(2d, 1e-9);
        result.Performance.Should().BeGreaterThan(1d, "the raw value carries the size of the error");
        result.Oee.Should().BeApproximately(2d, 1e-9);

        result.DataQuality.Should().HaveFlag(OeeDataQuality.PerformanceExceedsIdeal);
        result.IsTrustworthy.Should().BeFalse();
    }

    [Fact]
    public void Performance_of_exactly_one_is_not_flagged()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(60),
            plannedDowntime: TimeSpan.Zero,
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 3_600,
            rejectCount: 0));

        result.Performance.Should().Be(1d);
        result.DataQuality.Should().NotHaveFlag(OeeDataQuality.PerformanceExceedsIdeal);
    }

    [Fact]
    public void Several_data_quality_problems_can_be_reported_at_once()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(480),
            plannedDowntime: TimeSpan.FromMinutes(480),
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0));

        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoPlannedTime);
        result.DataQuality.Should().HaveFlag(OeeDataQuality.NoProduction);
    }

    // ------------------------------------------------------------- input guards

    [Fact]
    public void All_scrap_scores_zero_quality()
    {
        OeeResult result = OeeCalculator.Calculate(new OeeInput(
            shiftLength: TimeSpan.FromMinutes(60),
            plannedDowntime: TimeSpan.Zero,
            unplannedDowntime: TimeSpan.Zero,
            idealCycleTime: TimeSpan.FromSeconds(1),
            totalCount: 3_600,
            rejectCount: 3_600));

        result.Quality.Should().Be(0d);
        result.Oee.Should().Be(0d);
        result.FullyProductiveTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Good_count_is_derived_from_total_and_rejects()
    {
        var input = new OeeInput(
            TimeSpan.FromMinutes(60), TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.FromSeconds(1), totalCount: 1_000, rejectCount: 40);

        input.GoodCount.Should().Be(960);
    }

    [Theory]
    [InlineData(-1, 0, 0, "shiftLength")]
    [InlineData(0, -1, 0, "plannedDowntime")]
    [InlineData(0, 0, -1, "unplannedDowntime")]
    public void Negative_durations_are_rejected(
        int shift, int planned, int unplanned, string expectedParameter)
    {
        Action act = () => _ = new OeeInput(
            TimeSpan.FromMinutes(shift),
            TimeSpan.FromMinutes(planned),
            TimeSpan.FromMinutes(unplanned),
            TimeSpan.FromSeconds(1),
            totalCount: 0,
            rejectCount: 0);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(expectedParameter);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_ideal_cycle_time_is_rejected(int seconds)
    {
        Action act = () => _ = new OeeInput(
            TimeSpan.FromMinutes(480), TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.FromSeconds(seconds), totalCount: 0, rejectCount: 0);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("idealCycleTime");
    }

    [Fact]
    public void Negative_counts_are_rejected()
    {
        Action total = () => _ = new OeeInput(
            TimeSpan.FromMinutes(480), TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.FromSeconds(1), totalCount: -1, rejectCount: 0);

        Action rejects = () => _ = new OeeInput(
            TimeSpan.FromMinutes(480), TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.FromSeconds(1), totalCount: 10, rejectCount: -1);

        total.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("totalCount");
        rejects.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("rejectCount");
    }

    [Fact]
    public void More_rejects_than_parts_is_a_programmer_error_not_a_data_quality_flag()
    {
        Action act = () => _ = new OeeInput(
            TimeSpan.FromMinutes(480), TimeSpan.Zero, TimeSpan.Zero,
            TimeSpan.FromSeconds(1), totalCount: 100, rejectCount: 101);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("rejectCount");
    }
}
