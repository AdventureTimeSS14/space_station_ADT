using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Medical.Patch;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTPatchedComponent : Component
{
    public const string ContainerId = "adt_patch";

    [ViewVariables(VVAccess.ReadOnly)]
    public Container PatchContainer = default!;
}
