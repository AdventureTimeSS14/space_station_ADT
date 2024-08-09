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
using Content.Shared.Preferences;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class PhantomVesselsUIController : UIController//, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private PhantomVesselsMenu? _menu;

    public override void Initialize()
    {

        EntityManager.EventBus.SubscribeEvent<RequestPhantomVesselMenuEvent>(EventSource.Network, this, OnRequestMenu);
        EntityManager.EventBus.SubscribeEvent<PopulatePhantomVesselMenuEvent>(EventSource.Network, this, OnPopulateRequest);

    }

    private void OnRequestMenu(RequestPhantomVesselMenuEvent ev)
    {
        ToggleStylesMenu(ev);
    }

    private void OnPopulateRequest(PopulatePhantomVesselMenuEvent ev)
    {
        if (_menu != null)
            _menu.Populate(new RequestPhantomVesselMenuEvent(ev.Uid, ev.Vessels));
    }
    private void ToggleStylesMenu(RequestPhantomVesselMenuEvent ev)
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<PhantomVesselsMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectVessel += OnSelectVessel;
            _menu.Populate(ev);

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectVessel -= OnSelectVessel;
            _menu.Vessels.Clear();

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

    private void OnSelectVessel(NetEntity ent)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomVesselEvent(_entityManager.GetNetEntity(player), ent);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
