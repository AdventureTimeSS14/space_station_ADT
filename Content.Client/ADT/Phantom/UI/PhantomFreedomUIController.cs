using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Content.Client.ADT.Phantom.UI;
using Content.Shared.ADT.Phantom;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class PhantomFreedomUIController : UIController//, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private PhantomFreedomMenu? _menu;

    public void ToggleMenu(List<EntProtoId> prototypes)
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<PhantomFreedomMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectFreedom += OnSelectFreedom;
            _menu.Populate(prototypes);

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectFreedom -= OnSelectFreedom;

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

        _menu.Close();
        _menu = null;
    }

    private void OnSelectFreedom(string id)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectPhantomFreedomEvent(_entityManager.GetNetEntity(player), id);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
