namespace Oee.Domain;

/// <summary>
/// A machine held in one <see cref="MachineState"/> over a half-open time interval
/// <c>[Start, End)</c>.
/// </summary>
/// <remarks>
/// Segments are the durable form of what arrives as a stream of state-change signals:
/// the ingestion pipeline closes the previous segment when the next transition lands.
/// </remarks>
public readonly record struct StateSegment
{
    /// <summary>Creates a segment, validating that it does not run backwards.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="end"/> is earlier than <paramref name="start"/>.
    /// </exception>
    public StateSegment(MachineState state, DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                end,
                $"Segment end must not precede its start ({start:O}).");
        }

        State = state;
        Start = start;
        End = end;
    }

    /// <summary>The state the machine held for the whole segment.</summary>
    public MachineState State { get; }

    /// <summary>Inclusive start of the interval.</summary>
    public DateTimeOffset Start { get; }

    /// <summary>Exclusive end of the interval.</summary>
    public DateTimeOffset End { get; }

    /// <summary>How long the machine held <see cref="State"/>.</summary>
    public TimeSpan Duration => End - Start;

    /// <summary>Creates a segment from a start instant and a duration.</summary>
    public static StateSegment For(MachineState state, DateTimeOffset start, TimeSpan duration) =>
        new(state, start, start + duration);
}
