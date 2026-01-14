using Robust.Shared.GameStates;

namespace Content.Shared.ADT.RemoteEye.Components;

/// <summary>
/// Camera component that provides vision range for remote eye entities.
/// Different from StationAiVisionComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteEyeCameraComponent : Component
{
    /// <summary>
    /// Whether this camera is actively providing vision.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Whether vision is blocked by walls.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Occluded = true;

    /// <summary>
    /// Whether the camera needs power to function.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsPower = false;

    /// <summary>
    /// Whether the camera needs to be anchored.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedsAnchoring = false;

    /// <summary>
    /// Vision range in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 7.5f;
}
