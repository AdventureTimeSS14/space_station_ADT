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
    /// Список возможных трансформаций предметов на алтаре
    /// </summary>
    [DataField("possibleTransformations")]
    public List<TransformationData> PossibleTransformations = new();
}

[DataDefinition]
public sealed partial class TransformationData
{
    [DataField("requiredTag")]
    public string? RequiredTag;

    [DataField("requiredProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? RequiredProto;

    [DataField("requiredAmount")]
    public int RequiredAmount = 1;

    [DataField("powerCost")]
    public FixedPoint2 PowerCost = 0;

    [DataField("resultProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ResultProto;

    [DataField("effectProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? EffectProto;

    [DataField("soundPath")]
    public SoundSpecifier? SoundPath;
}