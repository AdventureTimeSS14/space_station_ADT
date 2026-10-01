using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Drake;

[RegisterComponent]
public sealed partial class ADTDrakeTempLavaComponent : Component
{
    [DataField]
    public List<ADTDrakeReplacedEntity> Replaced = new();
}

[DataDefinition]
public sealed partial class ADTDrakeReplacedEntity
{
    [DataField]
    public EntProtoId Prototype;

    [DataField]
    public Angle Rotation;
}
