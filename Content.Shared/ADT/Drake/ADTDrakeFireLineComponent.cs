using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Drake;

[RegisterComponent]
public sealed partial class ADTDrakeFireLineComponent : Component
{
    [ViewVariables]
    public EntityUid Grid;

    [ViewVariables]
    public List<Vector2i> Tiles = new();

    [ViewVariables]
    public int Index;

    [ViewVariables]
    public TimeSpan NextStepAt;

    [ViewVariables]
    public TimeSpan StepDelay = TimeSpan.FromSeconds(0.15);

    [ViewVariables]
    public EntityUid? Source;

    [ViewVariables]
    public HashSet<EntityUid> HitList = new();

    [DataField]
    public float Damage = 20f;

    [DataField]
    public float MechDamage = 45f;

    [DataField]
    public EntProtoId FireProto = "ADTDrakeFire";
}
