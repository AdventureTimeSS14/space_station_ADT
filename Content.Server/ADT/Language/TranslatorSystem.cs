using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.ADT.Language;
using Content.Shared.PowerCell;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Hands;

namespace Content.Server.ADT.Language;

public sealed class TranslatorSystem : SharedTranslatorSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ActivateInWorldEvent>(OnTranslatorActivateInWorld);
        SubscribeLocalEvent<HandheldTranslatorComponent, UseInHandEvent>(OnTranslatorUseInHand);

        SubscribeLocalEvent<HandheldTranslatorComponent, GotEquippedHandEvent>(OnPickUp);
        SubscribeLocalEvent<HandheldTranslatorComponent, GotUnequippedHandEvent>(OnDrop);

        SubscribeLocalEvent<HandheldTranslatorComponent, PowerCellSlotEmptyEvent>(OnPowerCellSlotEmpty);
    }

    private void OnTranslatorActivateInWorld(EntityUid translator, HandheldTranslatorComponent component, ActivateInWorldEvent args)
    {
        if (!component.ToggleOnInteract)
            return;
        Dirty(translator, component);

        ToggleTranslator(translator);

        _language.UpdateUi(args.User);
    }

    private void OnTranslatorUseInHand(EntityUid translator, HandheldTranslatorComponent component, UseInHandEvent args)
    {
        if (!component.ToggleOnInteract)
            return;
        Dirty(translator, component);

        ToggleTranslator(translator);
        component.User = component.Enabled ? args.User : null;

        _language.UpdateUi(args.User);
    }

    private void OnPickUp(EntityUid translator, HandheldTranslatorComponent component, GotEquippedHandEvent args)
    {
        Dirty(translator, component);

        component.User = args.User;

        _language.UpdateUi(args.User);
    }

    private void OnDrop(EntityUid translator, HandheldTranslatorComponent component, GotUnequippedHandEvent args)
    {
        Dirty(translator, component);

        if (component.User.HasValue)
            _language.SelectDefaultLanguage(component.User.Value);

        component.User = null;

        _language.UpdateUi(args.User);
    }

    private void ToggleTranslator(EntityUid uid, HandheldTranslatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var hasPower = _powerCell.HasDrawCharge(uid);

        if (hasPower)
        {
            component.Enabled = !component.Enabled;
            var popupMessage = Loc.GetString(component.Enabled ? "translator-component-turnon" : "translator-component-shutoff", ("translator", component.Owner));
            _popup.PopupEntity(popupMessage, component.Owner);
            if (!component.Enabled && component.User.HasValue)
                _language.SelectDefaultLanguage(component.User.Value);
        }

        Dirty(uid, component);
        OnAppearanceChange(uid, component);
    }
    private void OnPowerCellSlotEmpty(EntityUid translator, HandheldTranslatorComponent component, PowerCellSlotEmptyEvent args)
    {
        component.Enabled = false;

        component.User = null;

        Dirty(translator, component);
        OnAppearanceChange(translator, component);
    }
}
