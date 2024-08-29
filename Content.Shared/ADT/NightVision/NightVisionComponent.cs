// taken and adapted from https://github.com/RMC-14/RMC-14?ysclid=lzx00zxd6e53093995

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.NightVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedNightVisionSystem))]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    [DataField, AutoNetworkedField]
    public NightVisionState State = NightVisionState.Full;

    [DataField, AutoNetworkedField]
    public bool Overlay;

    [DataField, AutoNetworkedField]
    public bool Innate;

    [DataField, AutoNetworkedField]
    public bool SeeThroughContainers;
}

[Serializable, NetSerializable]
public enum NightVisionState
{
    Off,
    Half,
    Full
}

public sealed partial class ToggleNightVision : BaseAlertEvent;
