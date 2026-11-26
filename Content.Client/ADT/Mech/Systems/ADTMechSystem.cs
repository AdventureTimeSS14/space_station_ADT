using Content.Client.ADT.Mech.UI;
using Content.Client.Mech;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Mech.EntitySystems;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Containers;

namespace Content.Client.ADT.Mech.Systems;

public sealed class ADTMechSystem : SharedADTMechSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMechComponent, EntInsertedIntoContainerMessage>(OnEquipmentInserted);
        SubscribeLocalEvent<ADTMechComponent, EntRemovedFromContainerMessage>(OnEquipmentRemoved, after: new[] { typeof(StatusEffectsSystem) });
        SubscribeLocalEvent<MechThrusterComponent, AppearanceChangeEvent>(OnThrusterAppearance, after: new[] { typeof(MechSystem) });
    }

    private void OnEquipmentInserted(Entity<ADTMechComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateEquipmentUi(ent, args.Container);
    }

    private void OnEquipmentRemoved(Entity<ADTMechComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateEquipmentUi(ent, args.Container);
    }

    private void UpdateEquipmentUi(EntityUid mech, BaseContainer container)
    {
        if (!TryComp<MechComponent>(mech, out var mechComp) || container.ID != mechComp.EquipmentContainerId)
            return;

        if (_userInterface.TryGetOpenUi(mech, MechUiKey.Key, out var ui))
            ui.Update<MechBoundUiState>();
    }

    protected override void OpenRadial(EntityUid mech, EntityUid pilot)
    {
        if (_player.LocalEntity != pilot || !TryComp<MechComponent>(mech, out var mechComp))
            return;

        _ui.GetUIController<MechEquipmentUIController>().ToggleMenu(mech, GetSelectableEquipment(mechComp));
    }

    protected override void CloseRadial(EntityUid mech, EntityUid pilot)
    {
        if (_player.LocalEntity != pilot)
            return;

        _ui.GetUIController<MechEquipmentUIController>().CloseMenu();
    }

    private void OnThrusterAppearance(Entity<MechThrusterComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || ent.Comp.FlightState == null)
            return;

        if (_appearance.TryGetData<bool>(ent, MechVisuals.Broken, out var broken, args.Component) && broken)
            return;

        if (_appearance.TryGetData<bool>(ent, MechVisuals.Open, out var open, args.Component) && open)
            return;

        if (!_appearance.TryGetData<bool>(ent, MechThrusterVisuals.Flying, out var flying, args.Component) || !flying)
            return;

        _sprite.LayerSetRsiState((ent.Owner, args.Sprite), MechVisualLayers.Base, ent.Comp.FlightState);
    }
}
