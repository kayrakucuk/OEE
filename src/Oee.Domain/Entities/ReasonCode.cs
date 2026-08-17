namespace Oee.Domain.Entities;

/// <summary>
/// Why a machine stopped, as recorded by an operator.
/// </summary>
/// <remarks>
/// This is what makes the Six Big Losses attributable. Inferring the loss category from a
/// stop's duration is a guess; reading it off the reason the operator selected is a fact.
/// </remarks>
public class ReasonCode
{
    public int Id { get; set; }

    /// <summary>Short business key, e.g. <c>MECH</c>.</summary>
    public required string Code { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// Which of the Six Big Losses this reason rolls up to.
    /// </summary>
    /// <remarks>
    /// Null exactly when <see cref="IsPlanned"/> is true: planned downtime is subtracted
    /// before OEE is calculated, so it is not one of the losses OEE explains.
    /// </remarks>
    public LossCategory? SixBigLossCategory { get; set; }

    /// <summary>
    /// True when this stop reduces Planned Production Time rather than Availability.
    /// </summary>
    public bool IsPlanned { get; set; }
}
