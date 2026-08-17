namespace Oee.Domain;

/// <summary>
/// Planned Production Time that did not become good parts, attributed to the
/// Six Big Losses and expressed in time.
/// </summary>
/// <remarks>
/// Everything is in time units on purpose. Scrap is naturally a count and reduced speed
/// is naturally a rate, but converting both into "time you will not get back" is what
/// makes the losses comparable — and it is what lets
/// <see cref="OeeResult.PlannedProductionTime"/> reconcile exactly against
/// <see cref="OeeResult.FullyProductiveTime"/> plus <see cref="Total"/>.
/// </remarks>
public readonly record struct LossBreakdown
{
    /// <summary>Creates a breakdown from the six loss durations.</summary>
    public LossBreakdown(
        TimeSpan breakdowns,
        TimeSpan setupAndAdjustments,
        TimeSpan idlingAndMinorStops,
        TimeSpan reducedSpeed,
        TimeSpan processDefects,
        TimeSpan startupRejects)
    {
        Breakdowns = breakdowns;
        SetupAndAdjustments = setupAndAdjustments;
        IdlingAndMinorStops = idlingAndMinorStops;
        ReducedSpeed = reducedSpeed;
        ProcessDefects = processDefects;
        StartupRejects = startupRejects;
    }

    /// <summary>Big Loss 1 — unplanned stops from equipment failure.</summary>
    public TimeSpan Breakdowns { get; }

    /// <summary>Big Loss 2 — changeover and adjustment time.</summary>
    public TimeSpan SetupAndAdjustments { get; }

    /// <summary>
    /// Big Loss 3 — all idle time, both the short stops that sit inside Run Time and
    /// the long ones that were pulled out of it.
    /// </summary>
    public TimeSpan IdlingAndMinorStops { get; }

    /// <summary>Big Loss 4 — the gap between actual and ideal cycle time while running.</summary>
    public TimeSpan ReducedSpeed { get; }

    /// <summary>Big Loss 5 — time spent making parts that were scrapped in stable running.</summary>
    public TimeSpan ProcessDefects { get; }

    /// <summary>Big Loss 6 — time spent making parts scrapped during start-up.</summary>
    public TimeSpan StartupRejects { get; }

    /// <summary>Every loss added together.</summary>
    public TimeSpan Total =>
        Breakdowns
        + SetupAndAdjustments
        + IdlingAndMinorStops
        + ReducedSpeed
        + ProcessDefects
        + StartupRejects;

    /// <summary>Looks a single loss up by category.</summary>
    public TimeSpan this[LossCategory category] => category switch
    {
        LossCategory.Breakdowns => Breakdowns,
        LossCategory.SetupAndAdjustments => SetupAndAdjustments,
        LossCategory.IdlingAndMinorStops => IdlingAndMinorStops,
        LossCategory.ReducedSpeed => ReducedSpeed,
        LossCategory.ProcessDefects => ProcessDefects,
        LossCategory.StartupRejects => StartupRejects,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown loss category.")
    };

    /// <summary>Enumerates all six losses, largest first — i.e. in Pareto order.</summary>
    public IEnumerable<KeyValuePair<LossCategory, TimeSpan>> InParetoOrder()
    {
        // Copied to a local because a lambda in a struct cannot capture 'this'.
        LossBreakdown losses = this;

        return Enum.GetValues<LossCategory>()
            .Select(category => new KeyValuePair<LossCategory, TimeSpan>(category, losses[category]))
            .OrderByDescending(entry => entry.Value)
            .ThenBy(entry => entry.Key);
    }

    /// <summary>Which OEE factor a given loss degrades.</summary>
    public static OeeFactor FactorFor(LossCategory category) => category switch
    {
        LossCategory.Breakdowns or LossCategory.SetupAndAdjustments => OeeFactor.Availability,
        LossCategory.IdlingAndMinorStops or LossCategory.ReducedSpeed => OeeFactor.Performance,
        LossCategory.ProcessDefects or LossCategory.StartupRejects => OeeFactor.Quality,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown loss category.")
    };
}
