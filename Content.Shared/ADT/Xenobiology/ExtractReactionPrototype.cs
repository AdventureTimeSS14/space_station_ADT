using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Xenobiology;

/// <summary>
/// A set of reagent requirements and the effects that fire when they are met in a slime extract.
/// </summary>
[Prototype("extractReaction")]
public sealed partial class ExtractReactionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The minimum reagent requirements.
    /// </summary>
    [DataField("requirements", required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Requirements = default!;

    /// <summary>
    /// The effects caused when there is enough of the required reagents.
    /// </summary>
    [DataField("effects", required: true)]
    public List<ScaledEntityEffect> Effects = default!;

    /// <summary>
    /// Whether the extract should be deleted once this reaction runs out of uses.
    /// </summary>
    [DataField("shouldDelete", required: true)]
    public bool ShouldDelete = default!;

    /// <summary>
    /// If nonzero, how long until the effect actually occurs.
    /// </summary>
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.Zero;
}

/// <summary>
/// An entity effect combined with a scaling factor.
/// </summary>
/// <remarks>
/// The final factor is the minimum scaling factor found among the reagents
/// (amountInContainer / amountRequired) multiplied by <see cref="ScalingFactor"/>, plus <see cref="ScalingOffset"/>.
/// </remarks>
[DataDefinition]
public sealed partial class ScaledEntityEffect
{
    /// <summary>
    /// The effect.
    /// </summary>
    [DataField("effect", required: true)]
    public EntityEffect Effect = default!;

    /// <summary>
    /// Increases the scale in proportion to how much reagent was provided.
    /// </summary>
    [DataField("scalingFactor")]
    public FixedPoint2 ScalingFactor = 0;

    /// <summary>
    /// A flat modifier added at the end of calculating the scale.
    /// </summary>
    [DataField("scalingOffset")]
    public FixedPoint2 ScalingOffset = 1;
}
