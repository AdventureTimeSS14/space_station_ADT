using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;
using Content.Client.ADT.Phantom.UI;
using Content.Shared.ADT.Phantom;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Phantom;

[UsedImplicitly]
public sealed class PhantomStylesUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private PhantomStyleMenu? _menu;

    public void ToggleStylesMenu()
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

        _menu.Close();
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
