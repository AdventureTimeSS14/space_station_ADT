using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.CartridgeLoader.Cartridges;

public sealed partial class NanoChatUi : UIFragment
{
    private NanoChatUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NanoChatUiFragment();

        _fragment.OnMessageSent += nanoChatMessage =>
        {
            SendNanoChatUiMessage(nanoChatMessage, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NanoChatUiState cast)
            _fragment?.UpdateState(cast);
    }

    private static void SendNanoChatUiMessage(NanoChatUiMessageEvent nanoChatMessage,
        BoundUserInterface userInterface)
    {
        var message = new CartridgeUiMessage(nanoChatMessage);
        userInterface.SendMessage(message);
    }
}
