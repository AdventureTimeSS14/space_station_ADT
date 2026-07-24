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

    /// <summary>
    /// PointLight effect entity spawned for the local player while NV is full (Starlight port).
    /// Device NV goggles override this while worn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId EffectPrototype = "ADTEffectNightVision";

    /// <summary>
    /// Screen shader prototype id (client). Stronger variants can be used per-species.
    /// Device NV goggles override this while worn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Shader = "ADTModernNightVisionShader";
}

[Serializable, NetSerializable]
public enum NightVisionState
{
    Off,
    Full
}

public sealed partial class ToggleNightVision : BaseAlertEvent;
