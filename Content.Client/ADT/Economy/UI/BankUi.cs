using Content.Client.UserInterface.Fragments;
using Content.Shared.ADT.Economy;
using Content.Shared.CartridgeLoader;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Economy.UI;

[UsedImplicitly]
public sealed partial class BankUi : UIFragment
{
    private BankUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new BankUiFragment();

        _fragment.OnLinkAttempt += message => userInterface.SendMessage(new CartridgeUiMessage(message));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BankCartridgeUiState bankState)
            return;

        _fragment?.UpdateState(bankState);
    }
}
