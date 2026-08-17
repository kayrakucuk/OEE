namespace Oee.Domain.Entities;

/// <summary>Something the line makes, and how fast it can be made.</summary>
public class Product
{
    public int Id { get; set; }

    /// <summary>Short business key, e.g. <c>PRD-100</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The fastest possible time to produce one unit, in seconds.
    /// </summary>
    /// <remarks>
    /// The single most consequential number in the whole model: it is the denominator of
    /// Performance, and setting it to the budgeted or historically-achieved rate rather
    /// than the nameplate rate makes the machine look perfect while hiding real losses.
    /// <para>
    /// Strictly this belongs on the <em>(product, machine)</em> pair — a slow machine
    /// running a fast product will look like a permanent Performance loss. Kept per
    /// product until that actually bites.
    /// </para>
    /// </remarks>
    public double IdealCycleTimeSec { get; set; }

    /// <summary>The ideal cycle time as a <see cref="TimeSpan"/>, for the calculator.</summary>
    public TimeSpan IdealCycleTime => TimeSpan.FromSeconds(IdealCycleTimeSec);
}
