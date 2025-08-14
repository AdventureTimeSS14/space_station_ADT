using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Collections.Generic;
using Robust.Shared.Audio;
using Content.Shared.FixedPoint;

namespace Content.ADT.Shared.Chaplain.Sacrifice;

[RegisterComponent]
public sealed partial class SacrificeComponent : Component
{
    /// <summary>
    /// List of possible variations of sacrafies->awards.
    /// </summary>
    [DataField("possibleTransformations")]
    public List<TransformationData> PossibleTransformations = new();
}

[DataDefinition]
public sealed partial class TransformationData
{
    /// <summary>
    /// Needed tag from entity to sacrafice.
    /// </summary>
    [DataField("requiredTag")]
    public string? RequiredTag;

    /// <summary>
    /// Needed entity prototype to sacrafice
    /// </summary>
    [DataField("requiredProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? RequiredProto;

    /// <summary>
    /// Needed amount of items in stack to sacrafice (Recomended to use tag, if you going to use it)
    /// </summary>
    [DataField("requiredAmount")]
    public int RequiredAmount = 1;

    /// <summary>
    /// Priority for transformation selection (higher = COOLER!!!)
    /// </summary>
    [DataField("priority")]
    public int Priority = 0;

    /// <summary>
    /// Needed amount of faith power to make sacrafice.
    /// </summary>
    [DataField("powerCost")]
    public FixedPoint2 PowerCost = 0;

    /// <summary>
    /// Entity prototype what you going to have after sacrafice.
    /// </summary>
    [DataField("resultProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ResultProto;

    /// <summary>
    /// Spawned effect prototype after sacrafice.
    /// </summary>
    [DataField("effectProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EffectProto;

    /// <summary>
    /// Spawned sound after sacrafice.
    /// </summary>
    [DataField("soundPath")]
    public SoundSpecifier? SoundPath;
}