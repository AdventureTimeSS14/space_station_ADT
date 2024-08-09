using System.Linq;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;
using Content.Shared.ADT.GhostInteractions;
using Content.Shared.Interaction.Events;

namespace Content.Server.ADT.GhostInteractions;

// this does not support holding multiple translators at once yet.
// that should not be an issue for now, but it better get fixed later.
public sealed class GhostRadioSystem : SharedGhostRadioSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostRadioComponent, ActivateInWorldEvent>(OnRadioToggle);
        SubscribeLocalEvent<GhostRadioComponent, UseInHandEvent>(OnRadioUseInHand);

        SubscribeLocalEvent<GhostRadioComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    private void OnRadioToggle(EntityUid translator, GhostRadioComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        var hasPower = _powerCell.HasDrawCharge(translator);

        var isEnabled = !component.Enabled;

        isEnabled &= hasPower;
        component.Enabled = isEnabled;

        OnAppearanceChange(translator, component);

        // HasPower shows a popup when there's no power, so we do not proceed in that case
        if (hasPower)
        {
            var message =
                Loc.GetString(component.Enabled ? "ghost-radio-component-turnon" : "ghost-radio-component-shutoff");
            _popup.PopupEntity(message, component.Owner, args.User);
        }
    }

    private void OnRadioUseInHand(EntityUid translator, GhostRadioComponent component, UseInHandEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        var hasPower = _powerCell.HasDrawCharge(translator);

        var isEnabled = !component.Enabled;

        isEnabled &= hasPower;
        component.Enabled = isEnabled;

        OnAppearanceChange(translator, component);

        // HasPower shows a popup when there's no power, so we do not proceed in that case
        if (hasPower)
        {
            var message =
                Loc.GetString(component.Enabled ? "ghost-radio-component-turnon" : "ghost-radio-component-shutoff");
            _popup.PopupEntity(message, component.Owner, args.User);
        }
    }

    private void OnPowerCellSlotEmpty(EntityUid translator, GhostRadioComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;
        OnAppearanceChange(translator, component);
    }
}
