namespace Oee.Domain.Entities;

/// <summary>A single piece of equipment. The unit OEE is measured against.</summary>
public class Machine
{
    public int Id { get; set; }

    public int LineId { get; set; }

    public Line Line { get; set; } = null!;

    /// <summary>Short business key, unique within the line, e.g. <c>M-A1</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Position in the line's process order, starting at 1.</summary>
    public int SequenceInLine { get; set; }

    /// <summary>
    /// Marks the constraint machine, whose OEE is conventionally reported as the line's.
    /// </summary>
    /// <remarks>
    /// Averaging OEE across a line is meaningless — the slowest machine sets the line's
    /// output, so improving anything else changes nothing. Exactly one machine per line
    /// should carry this flag.
    /// </remarks>
    public bool IsBottleneck { get; set; }
}
