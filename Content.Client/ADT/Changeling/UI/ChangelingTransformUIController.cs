using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Content.Shared.Changeling;

namespace Content.Client.ADT.Changeling.UI;

[UsedImplicitly]
public sealed class ChangelingTransformUIController : UIController//, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private ChangelingTransformMenu? _menu;

    public override void Initialize()
    {
        EntityManager.EventBus.SubscribeEvent<RequestChangelingFormsMenuEvent>(EventSource.Network, this, OnRequestMenu);
    }

    private void OnRequestMenu(RequestChangelingFormsMenuEvent ev)
    {
        ToggleMenu(ev);
    }

    private void ToggleMenu(RequestChangelingFormsMenuEvent ev)
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<ChangelingTransformMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectForm += OnSelectForm;

            _menu.Type = ev.Type;
            _menu.Target = ev.Target;

            _menu.Populate(ev);

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectForm -= OnSelectForm;
            _menu.Forms.Clear();

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

    private void OnSelectForm(NetEntity ent)
    {
        if (_menu == null)
            return;

        var player = _entityManager.GetNetEntity(_playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid);
        if (_menu.Type == ChangelingMenuType.Sting)
            player = _menu.Target;

        var ev = new SelectChangelingFormEvent(player, ent, _menu.Type);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
