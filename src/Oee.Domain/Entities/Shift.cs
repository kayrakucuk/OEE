namespace Oee.Domain.Entities;

/// <summary>A recurring shift pattern for one line.</summary>
public class Shift
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public Line Line { get; set; } = null!;

    /// <summary>Short business key, unique within the line, e.g. <c>A</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Wall-clock start time in the plant's local time.</summary>
    public TimeOnly StartLocal { get; set; }

    /// <summary>
    /// Wall-clock length of the shift.
    /// </summary>
    /// <remarks>
    /// A duration rather than an end time, so a night shift running 22:00–06:00 stays one
    /// row instead of splitting at midnight.
    /// </remarks>
    public TimeSpan Duration { get; set; }

    /// <summary>Which days the pattern runs on, judged by the local start date.</summary>
    public WeekDays Days { get; set; }

    public ICollection<PlannedDowntime> PlannedDowntimes { get; set; } = [];

    /// <summary>Projects to the pure form the <see cref="ShiftResolver"/> consumes.</summary>
    public ShiftDefinition ToDefinition() => new(Id, StartLocal, Duration, Days);
}
