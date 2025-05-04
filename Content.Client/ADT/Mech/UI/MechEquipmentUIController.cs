using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;
using Content.Shared.Mech.Components;
using Content.Shared.Mech;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Mech.UI;

[UsedImplicitly]
public sealed class MechEquipmentUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private MechEquipmentMenu? _menu;

    public void ToggleMenu()
    {
        if (_menu == null)
        {
            // setup window
            _menu = UIManager.CreateWindow<MechEquipmentMenu>();
            _menu.OnClose += OnWindowClosed;
            _menu.OnOpen += OnWindowOpen;
            _menu.OnSelectEquip += OnSelectEquip;

            _menu.OpenCentered();
        }
        else
        {
            _menu.OnClose -= OnWindowClosed;
            _menu.OnOpen -= OnWindowOpen;
            _menu.OnSelectEquip -= OnSelectEquip;

            CloseMenu();
        }
    }

    public void PopulateMenu(List<NetEntity> equip)
    {
        _menu?.Populate(equip);
    }

    private void OnWindowClosed()
    {
        CloseMenu();
    }

    private void OnWindowOpen()
    {
    }

    public void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Close();
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
