using Content.Shared.Stacks;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public float WeightlessAcceleration = 1f;

    [DataField]
    public float WeightlessModifier = 1.2f;

    [DataField]
    public float WeightlessFriction = 0.25f;

    [DataField]
    public float WeightlessFrictionNoInput = 0.25f;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/shuttle_thruster.ogg");

    public float Accumulator = 0f;
}
