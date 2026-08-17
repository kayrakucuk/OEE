namespace Oee.Domain;

/// <summary>
/// The days a shift or a planned downtime applies to.
/// </summary>
/// <remarks>
/// A flags enum rather than <see cref="DayOfWeek"/> because a shift pattern is a set of
/// days, not one day — and storing it as a single integer keeps the schedule queryable
/// without a join table.
/// </remarks>
[Flags]
public enum WeekDays
{
    None = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 4,
    Thursday = 8,
    Friday = 16,
    Saturday = 32,
    Sunday = 64,

    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    All = Weekdays | Weekend
}

/// <summary>Bridges <see cref="WeekDays"/> and the BCL's <see cref="DayOfWeek"/>.</summary>
public static class WeekDaysExtensions
{
    /// <summary>Converts a <see cref="DayOfWeek"/> to its single-day flag.</summary>
    public static WeekDays ToFlag(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WeekDays.Monday,
        DayOfWeek.Tuesday => WeekDays.Tuesday,
        DayOfWeek.Wednesday => WeekDays.Wednesday,
        DayOfWeek.Thursday => WeekDays.Thursday,
        DayOfWeek.Friday => WeekDays.Friday,
        DayOfWeek.Saturday => WeekDays.Saturday,
        DayOfWeek.Sunday => WeekDays.Sunday,
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, "Not a day of the week.")
    };

    /// <summary>True when the set includes the given date's day of week.</summary>
    public static bool Includes(this WeekDays days, DateOnly date) =>
        days.HasFlag(date.DayOfWeek.ToFlag());
}
