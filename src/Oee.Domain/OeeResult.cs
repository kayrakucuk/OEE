namespace Oee.Domain;

/// <summary>
/// The computed OEE picture for one window: the derived time buckets, the three factors,
/// and whether the inputs can be trusted.
/// </summary>
public sealed record OeeResult
{
    internal OeeResult()
    {
    }

    /// <summary>
    /// <c>ShiftLength - PlannedDowntime</c>, floored at zero. The denominator of
    /// Availability, and the whole of what OEE holds the machine accountable for.
    /// </summary>
    public TimeSpan PlannedProductionTime { get; init; }

    /// <summary>
    /// <c>PlannedProductionTime - UnplannedDowntime</c>, floored at zero.
    /// </summary>
    public TimeSpan RunTime { get; init; }

    /// <summary>
    /// <c>IdealCycleTime × TotalCount</c> — how long the output should have taken,
    /// including the parts that turned out to be scrap.
    /// </summary>
    public TimeSpan NetRunTime { get; init; }

    /// <summary>
    /// <c>IdealCycleTime × GoodCount</c> — the only time in the window that produced
    /// saleable output. Equals <c>OEE × PlannedProductionTime</c>.
    /// </summary>
    public TimeSpan FullyProductiveTime { get; init; }

    /// <summary>Run Time / Planned Production Time. Zero when nothing was scheduled.</summary>
    public double Availability { get; init; }

    /// <summary>
    /// (Ideal Cycle Time × Total Count) / Run Time. Zero when Run Time is zero.
    /// </summary>
    /// <remarks>
    /// Reported raw and <em>not</em> clamped to 1. A value above 1 is impossible in
    /// reality, so the excess is a direct measure of how wrong the ideal cycle time is —
    /// clamping would throw that information away and leave a wrong configuration looking
    /// like a perfect machine. Callers that need a display value should clamp at the
    /// presentation layer and read <see cref="DataQuality"/> to know when they did.
    /// </remarks>
    public double Performance { get; init; }

    /// <summary>Good Count / Total Count. Zero when nothing was produced.</summary>
    public double Quality { get; init; }

    /// <summary>
    /// Availability × Performance × Quality. Can exceed 1 when
    /// <see cref="Performance"/> does — see the remarks there.
    /// </summary>
    public double Oee { get; init; }

    /// <summary>Problems found in the inputs. <see cref="OeeDataQuality.Ok"/> when clean.</summary>
    public OeeDataQuality DataQuality { get; init; }

    /// <summary>True when no data-quality problem was found.</summary>
    public bool IsTrustworthy => DataQuality == OeeDataQuality.Ok;

    /// <summary>The inputs the result was computed from.</summary>
    public OeeInput Input { get; init; }

    /// <summary>An all-zero result.</summary>
    public static OeeResult Empty { get; } = new();
}
