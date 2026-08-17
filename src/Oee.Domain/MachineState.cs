namespace Oee.Domain;

/// <summary>
/// The operating state of a machine over a contiguous slice of time.
/// </summary>
/// <remarks>
/// The state set is deliberately small. Anything finer (which fault code, which
/// changeover) belongs on the event that caused the transition, not on the state
/// itself — OEE only ever needs to know which time bucket a slice falls into.
/// <para>
/// Note that "reduced speed" is <em>not</em> a state. Running slower than the ideal
/// cycle time is not something a machine reports; it is derived by comparing the
/// parts actually produced against the parts that could have been produced.
/// </para>
/// </remarks>
public enum MachineState
{
    /// <summary>
    /// State is not known — typically before the first signal of a window arrives.
    /// Treated as unscheduled time so that missing data never inflates OEE.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Outside the production schedule (unmanned shift, weekend, plant shutdown).
    /// Excluded from Planned Production Time, so it does not affect OEE.
    /// </summary>
    Off = 1,

    /// <summary>
    /// Scheduled, but deliberately not producing: breaks, meetings, planned
    /// maintenance, no demand. Excluded from Planned Production Time.
    /// </summary>
    PlannedStop = 2,

    /// <summary>
    /// Unplanned equipment failure. Availability loss (Big Loss 1).
    /// </summary>
    Breakdown = 3,

    /// <summary>
    /// Changeover, tooling swap, warm-up or adjustment. Availability loss (Big Loss 2).
    /// </summary>
    Setup = 4,

    /// <summary>
    /// Available and scheduled, but not producing: starved of material, blocked
    /// downstream, jammed, waiting on an operator. Big Loss 3.
    /// </summary>
    /// <remarks>
    /// Whether a given <see cref="Idle"/> slice costs Availability or Performance depends
    /// on the reason code the operator attached to it, not on how long it lasted.
    /// </remarks>
    Idle = 5,

    /// <summary>
    /// Producing parts.
    /// </summary>
    Running = 6
}

/// <summary>
/// Convenience classification of <see cref="MachineState"/> values.
/// </summary>
public static class MachineStateExtensions
{
    /// <summary>
    /// True when the state is excluded from Planned Production Time, and therefore
    /// cannot affect OEE at all.
    /// </summary>
    public static bool IsExcludedFromPlannedTime(this MachineState state) =>
        state is MachineState.Unknown or MachineState.Off or MachineState.PlannedStop;

    /// <summary>
    /// True when the machine is scheduled to produce but is not producing.
    /// </summary>
    public static bool IsScheduledStop(this MachineState state) =>
        state is MachineState.Breakdown or MachineState.Setup or MachineState.Idle;
}
