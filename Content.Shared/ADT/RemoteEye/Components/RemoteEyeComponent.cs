using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RemoteEye.Components;

/// <summary>
/// Component for the remote eye entity that can move through camera vision.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteEyeComponent : Component
{
    /// <summary>
    /// The console that spawned this eye.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Console;
}
