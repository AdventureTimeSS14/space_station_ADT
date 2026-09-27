using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class XenobiologyConsoleViewComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public int StoredSlimes;

    [ViewVariables, AutoNetworkedField]
    public int MaxStoredSlimes = 5;

    [ViewVariables, AutoNetworkedField]
    public int MonkeyCubes;

    [ViewVariables, AutoNetworkedField]
    public int MutationPotions;

    [ViewVariables, AutoNetworkedField]
    public int StabilizerPotions;
}
