namespace Oee.Domain.Entities;

/// <summary>A production line: an ordered set of machines sharing a shift schedule.</summary>
public class Line
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public Plant Plant { get; set; } = null!;

    /// <summary>Short business key, unique within the plant, e.g. <c>LINE-A</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    public ICollection<Machine> Machines { get; set; } = [];

    public ICollection<Shift> Shifts { get; set; } = [];
}
