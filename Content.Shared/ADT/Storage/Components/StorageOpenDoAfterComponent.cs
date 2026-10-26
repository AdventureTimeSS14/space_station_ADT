using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Storage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageOpenDoAfterComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);
}

[Serializable, NetSerializable]
public sealed partial class StorageOpenDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public bool Open;

    public StorageOpenDoAfterEvent()
    {
    }

    public StorageOpenDoAfterEvent(bool open)
    {
        Open = open;
    }
}
