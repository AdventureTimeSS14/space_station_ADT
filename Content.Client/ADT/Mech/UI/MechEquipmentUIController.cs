using JetBrains.Annotations;
using Content.Shared.Mech;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.ADT.Mech.UI;

[UsedImplicitly]
public sealed class MechEquipmentUIController : UIController
{
    [Dependency] private IEntityManager _entityManager = default!;

    private MechEquipmentMenu? _menu;
    private NetEntity? _mech;

    public void ToggleMenu(EntityUid mech, List<EntityUid> equipment)
    {
        if (_menu != null)
        {
            CloseMenu();
            return;
        }

        _mech = _entityManager.GetNetEntity(mech);
        _menu = UIManager.CreateWindow<MechEquipmentMenu>();
        _menu.OnClose += CloseMenu;
        _menu.OnSelectEquip += OnSelectEquip;
        _menu.Populate(equipment);
        _menu.OpenCentered();
    }

    public void CloseMenu()
    {
        if (_menu == null)
            return;

        var menu = _menu;
        _menu = null;
        _mech = null;

        menu.OnClose -= CloseMenu;
        menu.OnSelectEquip -= OnSelectEquip;
        menu.Close();
    }

    private void OnSelectEquip(EntityUid? equipment)
    {
        if (_mech is { } mech)
        {
            var ev = new SelectMechEquipmentEvent(mech, _entityManager.GetNetEntity(equipment));
            _entityManager.RaisePredictiveEvent(ev);
        }

        CloseMenu();
    }
}
