using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MechOverloadComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MechOverloadActionEntity;

    [DataField]
    public EntProtoId MechOverloadAction = "ActionMechOverload";

    [DataField, AutoNetworkedField]
    public bool Overload;

    [DataField("damage", required: true)]
    public DamageSpecifier DamagePerSpeed = default!;

    [DataField]
    public TimeSpan DamageInterval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextDamage;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinIng;

    [DataField]
    public float WalkSpeedMultiplier = 2.5f;

    [DataField]
    public float SprintSpeedMultiplier = 1.25f;

    [DataField]
    public float AccelerationMultiplier = 2f;
}
