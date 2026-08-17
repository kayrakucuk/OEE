namespace Oee.Domain.Entities;

/// <summary>
/// Time inside a shift when the line is deliberately not producing: breaks, planned
/// maintenance, no demand.
/// </summary>
/// <remarks>
/// Subtracted from shift length to give Planned Production Time, so it never counts
/// against Availability — which is the whole point of distinguishing it from a breakdown.
/// <para>
/// One entity covers both the recurring case (a meal break every weekday, via
/// <see cref="Days"/>) and the one-off case (maintenance next Tuesday, via
/// <see cref="EffectiveFrom"/> and <see cref="EffectiveTo"/>), because they differ only in
/// how long they apply for.
/// </para>
/// </remarks>
public class PlannedDowntime
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public Shift Shift { get; set; } = null!;

    /// <summary>The machine affected, or <c>null</c> when the whole line stops.</summary>
    public int? MachineId { get; set; }

    public Machine? Machine { get; set; }

    public int ReasonCodeId { get; set; }

    public ReasonCode ReasonCode { get; set; } = null!;

    /// <summary>Wall-clock start in the plant's local time.</summary>
    public TimeOnly StartLocal { get; set; }

    public TimeSpan Duration { get; set; }

    /// <summary>Which days this applies on.</summary>
    public WeekDays Days { get; set; }

    /// <summary>First date this applies, or <c>null</c> for no lower bound.</summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>Last date this applies, or <c>null</c> for no upper bound.</summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>True when this downtime applies on the given shift date.</summary>
    public bool AppliesOn(DateOnly shiftDate) =>
        Days.Includes(shiftDate)
        && (EffectiveFrom is null || shiftDate >= EffectiveFrom)
        && (EffectiveTo is null || shiftDate <= EffectiveTo);
}
