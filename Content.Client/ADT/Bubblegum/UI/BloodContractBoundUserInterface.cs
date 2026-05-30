using Content.Shared.ADT.Bubblegum.Loot;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Bubblegum.UI;

public sealed class BloodContractBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BloodContractWindow? _window;

    public BloodContractBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BloodContractWindow>();
        _window.OnTargetSelected += target => SendMessage(new BloodContractSelectMessage(target));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BloodContractBuiState contractState)
            _window?.SetTargets(contractState.Targets);
    }
}
