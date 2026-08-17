namespace Oee.Domain;

/// <summary>
/// A shift pattern, stripped of everything the resolver does not need.
/// </summary>
/// <param name="ShiftId">Identifies the shift this pattern came from.</param>
/// <param name="StartLocal">Wall-clock start time in the plant's local time.</param>
/// <param name="Duration">
/// Wall-clock length. Kept as a duration rather than an end time so a night shift crossing
/// midnight stays one row.
/// </param>
/// <param name="Days">Which days the pattern runs on, by local start date.</param>
public readonly record struct ShiftDefinition(
    int ShiftId,
    TimeOnly StartLocal,
    TimeSpan Duration,
    WeekDays Days);

/// <summary>
/// One concrete occurrence of a shift, resolved to absolute time.
/// </summary>
/// <param name="ShiftId">The shift pattern that produced this occurrence.</param>
/// <param name="ShiftDate">
/// The local date the shift <em>started</em>. A night shift running 22:00–06:00 belongs to
/// the date it began, so it stays a single reporting unit rather than splitting at
/// midnight.
/// </param>
/// <param name="StartUtc">Absolute start.</param>
/// <param name="EndUtc">Absolute end.</param>
public readonly record struct ShiftAssignment(
    int ShiftId,
    DateOnly ShiftDate,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc)
{
    /// <summary>
    /// The real elapsed length of this occurrence.
    /// </summary>
    /// <remarks>
    /// Not the same as the pattern's nominal duration. A shift spanning a daylight-saving
    /// transition is genuinely an hour shorter or longer, and this is the value that must
    /// feed <see cref="OeeInput.ShiftLength"/> — using the nominal 8 hours would misstate
    /// Availability twice a year.
    /// </remarks>
    public TimeSpan ActualLength => EndUtc - StartUtc;
}

/// <summary>
/// Maps an instant to the shift occurrence that contains it.
/// </summary>
/// <remarks>
/// Pure: definitions and time zone are injected, nothing is loaded here. Mapping
/// <c>Shift</c> entities to <see cref="ShiftDefinition"/> is the persistence layer's job.
/// </remarks>
public sealed class ShiftResolver
{
    private readonly ShiftDefinition[] _shifts;
    private readonly TimeZoneInfo _plantTimeZone;

    /// <summary>Creates a resolver for one plant's shift patterns.</summary>
    /// <exception cref="ArgumentException">
    /// A definition has a duration that is not positive or is 24 hours or longer.
    /// </exception>
    public ShiftResolver(IReadOnlyCollection<ShiftDefinition> shifts, TimeZoneInfo plantTimeZone)
    {
        ArgumentNullException.ThrowIfNull(shifts);
        ArgumentNullException.ThrowIfNull(plantTimeZone);

        foreach (ShiftDefinition shift in shifts)
        {
            if (shift.Duration <= TimeSpan.Zero || shift.Duration >= TimeSpan.FromHours(24))
            {
                throw new ArgumentException(
                    $"Shift {shift.ShiftId} has a duration of {shift.Duration}; shifts must be " +
                    "longer than zero and shorter than 24 hours.",
                    nameof(shifts));
            }
        }

        _shifts = [.. shifts];
        _plantTimeZone = plantTimeZone;
    }

    /// <summary>
    /// Finds the shift occurrence containing <paramref name="instant"/>, or <c>null</c>
    /// when the instant falls outside every pattern (a weekend, or a gap between shifts).
    /// </summary>
    /// <remarks>
    /// When patterns overlap — which they should not, but data is data — the occurrence
    /// that started most recently wins.
    /// </remarks>
    public ShiftAssignment? Resolve(DateTimeOffset instant)
    {
        DateTime local = TimeZoneInfo.ConvertTime(instant, _plantTimeZone).DateTime;
        DateOnly today = DateOnly.FromDateTime(local);

        ShiftAssignment? best = null;

        // Yesterday is in scope because a night shift that started before midnight can
        // still be running now. Durations are capped below 24 hours, so one day back is
        // always enough.
        foreach (DateOnly date in (ReadOnlySpan<DateOnly>)[today.AddDays(-1), today])
        {
            foreach (ShiftDefinition shift in _shifts)
            {
                if (!shift.Days.Includes(date))
                {
                    continue;
                }

                ShiftAssignment occurrence = Occurrence(shift, date);

                if (instant < occurrence.StartUtc || instant >= occurrence.EndUtc)
                {
                    continue;
                }

                if (best is null || occurrence.StartUtc > best.Value.StartUtc)
                {
                    best = occurrence;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Builds the occurrence of a pattern on a given local date.
    /// </summary>
    private ShiftAssignment Occurrence(ShiftDefinition shift, DateOnly date)
    {
        DateTime startLocal = date.ToDateTime(shift.StartLocal, DateTimeKind.Unspecified);

        // Wall-clock arithmetic, not absolute: a shift is defined as "22:00 until 06:00",
        // so across a daylight-saving transition the occurrence really is 7 or 9 hours.
        DateTime endLocal = startLocal + shift.Duration;

        return new ShiftAssignment(
            shift.ShiftId,
            date,
            ToUtc(startLocal),
            ToUtc(endLocal));
    }

    /// <summary>
    /// Converts a local wall-clock time to an absolute instant, handling both daylight
    /// saving edges.
    /// </summary>
    private DateTimeOffset ToUtc(DateTime local)
    {
        if (_plantTimeZone.IsInvalidTime(local))
        {
            // Spring forward: the clock never showed this time. Skip to the instant it
            // jumped to, which is when the shift actually began.
            local += DaylightDeltaAt(local);
        }

        TimeSpan offset = _plantTimeZone.IsAmbiguousTime(local)
            // Fall back: the clock shows this time twice. Take the first occurrence, which
            // is the one still on the larger (daylight) offset.
            ? _plantTimeZone.GetAmbiguousTimeOffsets(local).Max()
            : _plantTimeZone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    private TimeSpan DaylightDeltaAt(DateTime local)
    {
        foreach (TimeZoneInfo.AdjustmentRule rule in _plantTimeZone.GetAdjustmentRules())
        {
            if (local >= rule.DateStart && local <= rule.DateEnd)
            {
                return rule.DaylightDelta;
            }
        }

        return TimeSpan.FromHours(1);
    }
}
