using System.Numerics;
using Content.Shared.ADT.Vehicle.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Vehicle.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedADTVehicleSystem))]
public sealed partial class ADTVehicleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LastRider;

    [ViewVariables]
    public Vector2 BaseBuckleOffset = Vector2.Zero;

    [DataField]
    public SoundSpecifier? HornSound = new SoundPathSpecifier("/Audio/Effects/Vehicle/carhorn.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f)
    };

    [DataField]
    public EntProtoId? HornAction = "ADTActionVehicleHorn";

    [DataField, AutoNetworkedField]
    public EntityUid? HornActionEntity;

    [DataField]
    public bool SouthOver;

    [DataField]
    public bool NorthOver;

    [DataField]
    public bool WestOver;

    [DataField]
    public bool EastOver;

    [DataField]
    public float NorthOverride;

    [DataField]
    public float SouthOverride;

    [DataField]
    public bool AutoAnimate = true;

    [DataField]
    public bool HideRider;
}
