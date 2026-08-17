namespace Oee.Domain;

/// <summary>
/// Problems found in the inputs while calculating OEE.
/// </summary>
/// <remarks>
/// None of these throw. They describe data that is wrong or degenerate but still
/// calculable, and the calculation still returns a number — because on a shop floor the
/// dashboard has to show something, and "0% with a reason" is far more useful than an
/// exception nobody sees.
/// <para>
/// The distinction from a thrown <see cref="ArgumentOutOfRangeException"/> is intent: a
/// negative duration is a bug in the caller, whereas a shift with no production is a
/// perfectly ordinary Tuesday.
/// </para>
/// </remarks>
[Flags]
public enum OeeDataQuality
{
    /// <summary>Nothing wrong with the inputs.</summary>
    Ok = 0,

    /// <summary>
    /// Planned Production Time came out at zero or below — the shift was entirely planned
    /// downtime. Availability is reported as zero rather than dividing by zero.
    /// </summary>
    NoPlannedTime = 1,

    /// <summary>
    /// Nothing was produced, so Quality and Performance have no meaningful denominator and
    /// are reported as zero.
    /// </summary>
    NoProduction = 2,

    /// <summary>
    /// The machine was scheduled but never ran: downtime consumed the whole of Planned
    /// Production Time.
    /// </summary>
    NoRunTime = 4,

    /// <summary>
    /// Performance came out above 1 — the counts say the machine beat its own ideal cycle
    /// time. Almost always a mis-configured ideal cycle time, occasionally an
    /// over-reporting counter. The raw value is preserved so the size of the error is
    /// visible.
    /// </summary>
    PerformanceExceedsIdeal = 8,

    /// <summary>
    /// Unplanned downtime exceeded Planned Production Time, which would make Run Time
    /// negative. Run Time is clamped to zero instead.
    /// </summary>
    DowntimeExceedsPlanned = 16
}
