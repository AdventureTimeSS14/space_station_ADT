using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Storage.Components;

/// <summary>
/// Requires a do-after to open or close this EntityStorage (e.g. body bags), so it can't be
/// toggled instantly. The do-after breaks on movement, so a bag that is being dragged cannot be
/// opened by a bystander.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageOpenDoAfterComponent : Component
{
    /// <summary>
    /// How long the open/close do-after takes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Set right before re-toggling after the do-after completes, so the follow-up toggle is not
    /// delayed again. Transient, reset within the same tick.
    /// </summary>
    [ViewVariables]
    public bool BypassDoAfter;
}

[Serializable, NetSerializable]
public sealed partial class StorageOpenDoAfterEvent : SimpleDoAfterEvent { }
