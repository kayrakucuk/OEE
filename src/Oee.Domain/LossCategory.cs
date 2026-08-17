namespace Oee.Domain;

/// <summary>
/// The Six Big Losses — the standard taxonomy that explains where Planned Production
/// Time goes when it does not turn into good parts.
/// </summary>
/// <remarks>
/// Each loss maps to exactly one OEE factor, which is what makes the taxonomy useful:
/// an OEE number tells you that you lost time, the loss category tells you where to go
/// looking.
/// </remarks>
public enum LossCategory
{
    /// <summary>Unplanned stops from equipment failure. Hits Availability.</summary>
    Breakdowns = 1,

    /// <summary>Changeovers, tooling changes, warm-up, adjustment. Hits Availability.</summary>
    SetupAndAdjustments = 2,

    /// <summary>
    /// Short stops and starvation — jams, blocked conveyors, missing material.
    /// Hits Performance when short, Availability once a stop runs long.
    /// </summary>
    IdlingAndMinorStops = 3,

    /// <summary>Running slower than the ideal cycle time. Hits Performance.</summary>
    ReducedSpeed = 4,

    /// <summary>Scrap and rework produced during stable running. Hits Quality.</summary>
    ProcessDefects = 5,

    /// <summary>Scrap produced before the process stabilises after a start. Hits Quality.</summary>
    StartupRejects = 6
}

/// <summary>
/// Which OEE factor a <see cref="LossCategory"/> degrades.
/// </summary>
public enum OeeFactor
{
    /// <summary>Availability = Run Time / Planned Production Time.</summary>
    Availability = 1,

    /// <summary>Performance = Ideal Cycle Time × Total Count / Run Time.</summary>
    Performance = 2,

    /// <summary>Quality = Good Count / Total Count.</summary>
    Quality = 3
}
