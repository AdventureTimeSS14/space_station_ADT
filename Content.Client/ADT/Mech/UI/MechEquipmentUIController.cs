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
using Content.Shared.Mech.Components;
using Content.Shared.Mech;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Mech.UI;

[UsedImplicitly]
public sealed class MechEquipmentUIController : UIController//, IOnStateChanged<GameplayState>
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private MechEquipmentMenu? _menu;

    public override void Initialize()
    {

        EntityManager.EventBus.SubscribeLocalEvent<MechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        EntityManager.EventBus.SubscribeLocalEvent<MechComponent, CloseMechMenuEvent>(OnMechExit);

    }

    private void OnToggleEquipmentAction(EntityUid uid, MechComponent component, MechToggleEquipmentEvent args)
    {
        if (!_entityManager.TryGetComponent<MechPilotComponent>(_playerManager.LocalEntity, out var pilot) || pilot.Mech != uid)
            return;
        if (args.Handled)
            return;
        args.Handled = true;
        if (!_timing.IsFirstTimePredicted)
            return;

        List<NetEntity> list = new();
        foreach (var item in component.EquipmentContainer.ContainedEntities)
        {
            list.Add(_entityManager.GetNetEntity(item));
        }
        var ev = new PopulateMechEquipmentMenuEvent(list);
        ToggleMenu(ev);
    }

    private void OnMechExit(EntityUid uid, MechComponent component, CloseMechMenuEvent args)
    {
        if (_menu != null)
            ToggleMenu(new(new())); // Для закрытия всё равно не сильно нужна эта информация
    }

    private void OnPopulateRequest(PopulateMechEquipmentMenuEvent ev)
    {
        if (_menu != null)
            _menu.Populate(new PopulateMechEquipmentMenuEvent(ev.Equipment));
    }
    private void ToggleMenu(PopulateMechEquipmentMenuEvent ev)
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<MechEquipmentMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectEquip += OnSelectEquip;
            _menu.Populate(ev);

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectEquip -= OnSelectEquip;
            _menu.Equipment.Clear();

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

    private void OnSelectEquip(NetEntity? ent)
    {
        var player = _playerManager.LocalSession?.AttachedEntity ?? EntityUid.Invalid;

        var ev = new SelectMechEquipmentEvent(_entityManager.GetNetEntity(player), ent);
        _entityManager.RaisePredictiveEvent(ev);

        CloseMenu();
    }
}
