namespace Oee.Domain;

/// <summary>
/// The parts a machine produced over a window, split by outcome.
/// </summary>
/// <remarks>
/// Scrap is split at the source rather than inferred later, because the two Quality
/// losses have completely different remedies: startup rejects are a changeover problem,
/// process defects are a stability problem.
/// </remarks>
public readonly record struct ProductionTally
{
    /// <summary>Creates a tally, validating that no count is negative.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Any count is negative.</exception>
    public ProductionTally(long goodCount, long processDefectCount = 0, long startupRejectCount = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(goodCount);
        ArgumentOutOfRangeException.ThrowIfNegative(processDefectCount);
        ArgumentOutOfRangeException.ThrowIfNegative(startupRejectCount);

        GoodCount = goodCount;
        ProcessDefectCount = processDefectCount;
        StartupRejectCount = startupRejectCount;
    }

    /// <summary>Parts produced to specification, first time through.</summary>
    public long GoodCount { get; }

    /// <summary>
    /// Parts scrapped or reworked during stable production (Big Loss 5).
    /// </summary>
    public long ProcessDefectCount { get; }

    /// <summary>
    /// Parts scrapped between start-up and stable production, typically right after a
    /// changeover (Big Loss 6).
    /// </summary>
    public long StartupRejectCount { get; }

    /// <summary>All parts rejected, regardless of cause.</summary>
    public long ScrapCount => ProcessDefectCount + StartupRejectCount;

    /// <summary>Every part the machine produced, good or not.</summary>
    public long TotalCount => GoodCount + ScrapCount;

    /// <summary>An empty tally.</summary>
    public static ProductionTally Empty => default;

    /// <summary>Adds two tallies together, category by category.</summary>
    public static ProductionTally operator +(ProductionTally left, ProductionTally right) =>
        new(
            left.GoodCount + right.GoodCount,
            left.ProcessDefectCount + right.ProcessDefectCount,
            left.StartupRejectCount + right.StartupRejectCount);
}
