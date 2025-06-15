using Content.Client.UserInterface.Systems.Chat;
using Robust.Client.UserInterface;

namespace Content.Client._Ganimed.IconsJob;

public sealed class ChatIconsSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        OnRadioIconsChanged(true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
    }

    private void OnRadioIconsChanged(bool enable)
    {
        _uiMan.GetUIController<ChatUIController>().Repopulate();
    }
}
