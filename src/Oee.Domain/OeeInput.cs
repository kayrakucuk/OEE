namespace Oee.Domain;

/// <summary>
/// Everything the OEE calculation needs, passed in explicitly.
/// </summary>
/// <remarks>
/// Deliberately aggregate rather than a timeline: how the durations and counts were
/// arrived at — folding machine state segments, summing downtime events by reason code —
/// is the ingestion layer's problem. Keeping this type dumb is what makes the calculator
/// a pure function of six numbers.
/// </remarks>
public readonly record struct OeeInput
{
    /// <summary>Creates a set of inputs, rejecting values that cannot be measurements.</summary>
    /// <param name="shiftLength">Total scheduled length of the shift.</param>
    /// <param name="plannedDowntime">Breaks, planned maintenance, no demand.</param>
    /// <param name="unplannedDowntime">Breakdowns, setup, starvation.</param>
    /// <param name="idealCycleTime">
    /// Fastest possible time per part. The nameplate rate — not the budgeted rate, and not
    /// the historically-achieved rate, either of which silently hides Performance loss.
    /// </param>
    /// <param name="totalCount">Every part produced, good or not.</param>
    /// <param name="rejectCount">Parts rejected, of <paramref name="totalCount"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A duration or count is negative, the ideal cycle time is not positive, or there are
    /// more rejects than parts. These are caller bugs, not data-quality issues.
    /// </exception>
    public OeeInput(
        TimeSpan shiftLength,
        TimeSpan plannedDowntime,
        TimeSpan unplannedDowntime,
        TimeSpan idealCycleTime,
        long totalCount,
        long rejectCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(shiftLength, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(plannedDowntime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(unplannedDowntime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idealCycleTime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);
        ArgumentOutOfRangeException.ThrowIfNegative(rejectCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rejectCount, totalCount);

        ShiftLength = shiftLength;
        PlannedDowntime = plannedDowntime;
        UnplannedDowntime = unplannedDowntime;
        IdealCycleTime = idealCycleTime;
        TotalCount = totalCount;
        RejectCount = rejectCount;
    }

    /// <summary>Total scheduled length of the shift.</summary>
    public TimeSpan ShiftLength { get; }

    /// <summary>Breaks, planned maintenance, no demand.</summary>
    public TimeSpan PlannedDowntime { get; }

    /// <summary>Breakdowns, setup, starvation — everything unplanned.</summary>
    public TimeSpan UnplannedDowntime { get; }

    /// <summary>The theoretical fastest time to produce one part.</summary>
    public TimeSpan IdealCycleTime { get; }

    /// <summary>Every part produced, good or not.</summary>
    public long TotalCount { get; }

    /// <summary>Parts rejected.</summary>
    public long RejectCount { get; }

    /// <summary>Parts produced to specification, first time through.</summary>
    public long GoodCount => TotalCount - RejectCount;
}
