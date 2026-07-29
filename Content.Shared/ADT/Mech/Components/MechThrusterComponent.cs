using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechThrusterComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? MechThrusterActionEntity;

    [DataField]
    public EntProtoId MechThrusterAction = "ADTActionMechThruster";

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Active = false;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Fuel = 300f;

    [DataField]
    public float MaxFuel = 300f;

    [DataField]
    public float FuelDrain = 1f;

    [DataField]
    public float FuelPerSheet = 30f;

    [DataField]
    public ProtoId<StackPrototype> FuelStackType = "Plasma";

    [DataField]
    public float WeightlessAcceleration = 3f;

    [DataField]
    public float WeightlessModifier = 2f;

    [DataField]
    public float WeightlessFriction = 0.4f;

    [DataField]
    public float WeightlessFrictionNoInput = 0.15f;

    [DataField]
    public string? FlightState;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/shuttle_thruster.ogg");

    [DataField]
    public EntProtoId Effect = "JetpackEffect";

    [DataField]
    public float EffectCooldown = 0.3f;

    [DataField]
    public float EffectMaxDistance = 0.7f;

    public EntityCoordinates EffectLastCoordinates;

    public TimeSpan EffectTargetTime = TimeSpan.Zero;

    public float Accumulator = 0f;
}

[Serializable, NetSerializable]
public enum MechThrusterVisuals : byte
{
    Flying,
}
