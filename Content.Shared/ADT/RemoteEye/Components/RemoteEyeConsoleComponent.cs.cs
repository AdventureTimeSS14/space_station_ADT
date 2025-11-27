using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.RemoteEye.Components;

/// <summary>
/// Console that allows transferring consciousness to a remote eye entity.
/// Stores the original entity to allow returning.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RemoteEyeConsoleComponent : Component
{
    /// <summary>
    /// The eye entity that the user transfers to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? RemoteEye;

    /// <summary>
    /// The original entity that used the console.
    /// Stored to allow returning.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? OriginalEntity;

    public EntityUid? ReturnActionEntity;

    /// <summary>
    /// Prototype to spawn for the remote eye.
    /// </summary>
    [DataField]
    public string RemoteEyePrototype = "RemoteEye";
}
