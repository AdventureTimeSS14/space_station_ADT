using Content.Client.Chat.UI;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Content.Client.ADT.Phantom.UI;
using Content.Shared.ADT.Phantom;
using Robust.Shared.Player;
using Content.Shared.ADT.Language;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class PhantomStylesUIController : UIController//, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private PhantomStyleMenu? _menu;
    private NetEntity _uid = NetEntity.Invalid;

    public override void Initialize()
    {

        EntityManager.EventBus.SubscribeEvent<RequestPhantomStyleMenuEvent>(EventSource.Network, this, OnRequestMenu);
    }

    private void OnRequestMenu(RequestPhantomStyleMenuEvent ev)
    {
        _uid = ev.Target;
        ToggleStylesMenu();
    }

    private void ToggleStylesMenu()
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<PhantomStyleMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectStyle += OnSelectStyle;

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectStyle -= OnSelectStyle;

            CloseMenu();
        }
    }

    private void OnWindowClosed()
    {
        CloseMenu();
    }

    private void OnWindowOpen()
    {
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Dispose();
        _menu = null;
    }

    private void OnSelectStyle(ProtoId<PhantomStylePrototype> protoId)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomStyleEvent(_entityManager.GetNetEntity(player), protoId);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
