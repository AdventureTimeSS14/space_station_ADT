using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ThermalVision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedThermalVisionSystem))]
public sealed partial class ThermalVisionComponent : Component
{

    [DataField, AutoNetworkedField]
    public ThermalVisionState State = ThermalVisionState.Full;

    [DataField, AutoNetworkedField]
    public bool Innate;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#FF4500");
}

[Serializable, NetSerializable]
public enum ThermalVisionState
{
    Off,
    Full
}

public sealed partial class ToggleThermalVision : BaseAlertEvent;
