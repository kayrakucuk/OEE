namespace Oee.Domain;

/// <summary>
/// Turns a set of <see cref="OeeInput"/> measurements into an <see cref="OeeResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// Pure: no clock, no I/O, no state, no configuration. Every input is passed in, so every
/// interesting case can be pinned down by a test rather than argued about. This project
/// depends on nothing; everything else depends on it.
/// </para>
/// <para>
/// Degenerate inputs never throw and never produce <c>NaN</c>. A zero denominator yields
/// a zero factor and a flag on <see cref="OeeResult.DataQuality"/>, because a dashboard
/// showing "0% — no planned time" is useful and one showing <c>NaN</c> is not.
/// </para>
/// </remarks>
public static class OeeCalculator
{
    /// <summary>Computes Availability, Performance, Quality and OEE.</summary>
    public static OeeResult Calculate(in OeeInput input)
    {
        var flags = OeeDataQuality.Ok;

        TimeSpan plannedProductionTime = input.ShiftLength - input.PlannedDowntime;
        if (plannedProductionTime <= TimeSpan.Zero)
        {
            plannedProductionTime = TimeSpan.Zero;
            flags |= OeeDataQuality.NoPlannedTime;
        }

        TimeSpan runTime = plannedProductionTime - input.UnplannedDowntime;
        if (runTime < TimeSpan.Zero)
        {
            runTime = TimeSpan.Zero;

            // Only worth reporting when there was planned time to overrun in the first
            // place — otherwise NoPlannedTime already explains it.
            if (plannedProductionTime > TimeSpan.Zero)
            {
                flags |= OeeDataQuality.DowntimeExceedsPlanned;
            }
        }

        if (runTime == TimeSpan.Zero && plannedProductionTime > TimeSpan.Zero)
        {
            flags |= OeeDataQuality.NoRunTime;
        }

        if (input.TotalCount == 0)
        {
            flags |= OeeDataQuality.NoProduction;
        }

        TimeSpan netRunTime = input.IdealCycleTime * input.TotalCount;
        TimeSpan fullyProductiveTime = input.IdealCycleTime * input.GoodCount;

        double availability = Ratio(runTime, plannedProductionTime);
        double performance = Ratio(netRunTime, runTime);
        double quality = input.TotalCount == 0
            ? 0d
            : (double)input.GoodCount / input.TotalCount;

        // Reported raw, on purpose. See OeeResult.Performance.
        if (performance > 1d)
        {
            flags |= OeeDataQuality.PerformanceExceedsIdeal;
        }

        return new OeeResult
        {
            PlannedProductionTime = plannedProductionTime,
            RunTime = runTime,
            NetRunTime = netRunTime,
            FullyProductiveTime = fullyProductiveTime,
            Availability = availability,
            Performance = performance,
            Quality = quality,
            Oee = availability * performance * quality,
            DataQuality = flags,
            Input = input
        };
    }

    private static double Ratio(TimeSpan numerator, TimeSpan denominator) =>
        denominator <= TimeSpan.Zero ? 0d : numerator / denominator;
}
