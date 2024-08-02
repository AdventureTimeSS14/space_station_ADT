using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Language;
using Content.Shared.PowerCell;

namespace Content.Server.Language;

public sealed class TranslatorSystem : SharedTranslatorSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorToggle);
        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    private void OnTranslatorToggle(EntityUid translator, HandheldTranslatorComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;

        var hasPower = _powerCell.HasDrawCharge(translator);

        if (hasPower)
        {
            component.Enabled = !component.Enabled;
            //_powerCell.SetPowerCellDrawEnabled(translator, component.Enabled);
            var popupMessage = Loc.GetString(component.Enabled ? "translator-component-turnon" : "translator-component-shutoff", ("translator", component.Owner));
            _popup.PopupEntity(popupMessage, component.Owner, args.User);
        }

        OnAppearanceChange(translator, component);
    }

    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;
        //_powerCell.SetPowerCellDrawEnabled(translator, false);
        OnAppearanceChange(translator, component);
    }
}
