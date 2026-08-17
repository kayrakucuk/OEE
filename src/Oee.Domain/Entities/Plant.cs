namespace Oee.Domain.Entities;

/// <summary>A manufacturing site.</summary>
public class Plant
{
    public int Id { get; set; }

    /// <summary>Short business key, e.g. <c>IST-01</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// IANA time zone id, e.g. <c>Europe/Istanbul</c>.
    /// </summary>
    /// <remarks>
    /// Shifts are defined in wall-clock time, so resolving a UTC signal to a shift is
    /// impossible without knowing which clock the plant reads. Stored per plant rather
    /// than per server for the obvious reason.
    /// </remarks>
    public required string TimeZoneId { get; set; }

    public ICollection<Line> Lines { get; set; } = [];
}
