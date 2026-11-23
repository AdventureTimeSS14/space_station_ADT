using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Storage;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTNoArmedExplosivesStorageComponent : Component
{
    [DataField]
    public string ContainerId = "storagebase";
}
