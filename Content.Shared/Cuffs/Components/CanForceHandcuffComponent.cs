using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class CanForceHandcuffComponent : Component
{
    [DataField]
    public EntProtoId HandcuffsId = "Handcuffs";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Handcuffs;

    [ViewVariables]
    public BaseContainer? Container;

    [DataField]
    public bool RequireHands = true;

    [DataField]
    public bool Complex = false;
}
