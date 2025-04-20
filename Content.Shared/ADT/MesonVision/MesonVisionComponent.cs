using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MesonVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMesonVisionSystem))]
public sealed partial class MesonVisionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    [DataField, AutoNetworkedField]
    public MesonVisionState State = MesonVisionState.Full;

    [DataField, AutoNetworkedField]
    public bool Overlay;

    [DataField, AutoNetworkedField]
    public bool Innate;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#D3D3D3");
}

[Serializable, NetSerializable]
public enum MesonVisionState
{
    Off,
    Full
}

public sealed partial class ToggleMesonVision : BaseAlertEvent;
