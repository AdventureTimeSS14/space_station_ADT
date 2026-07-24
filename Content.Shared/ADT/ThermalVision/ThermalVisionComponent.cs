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

    /// <summary>
    /// PointLight effect entity spawned for the local player while thermal is full (Starlight port).
    /// </summary>
    [DataField]
    public EntProtoId EffectPrototype = "ADTEffectThermalVision";

    /// <summary>
    /// Use half-alpha thermal screen shader instead of the full one.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UseAlternativeShader;
}

[Serializable, NetSerializable]
public enum ThermalVisionState
{
    Off,
    Full
}

public sealed partial class ToggleThermalVision : BaseAlertEvent;
