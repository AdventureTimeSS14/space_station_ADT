using Content.Server.ADT.Chat;
using Content.Server.Radio;
using Content.Shared.ADT.Language;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Server.ADT.Language;

public sealed partial class LanguageSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    private void InitializeTraits()
    {
        SubscribeLocalEvent<DeafTraitComponent, MapInitEvent>(OnDeafInit);
        SubscribeLocalEvent<DeafTraitComponent, CanHearVoiceEvent>(OnCanHear);
        SubscribeLocalEvent<DeafTraitComponent, RadioReceiveAttemptEvent>(OnRadioRecieveAttempt);
    }

    private void OnDeafInit(EntityUid uid, DeafTraitComponent comp, MapInitEvent args)
    {
        if (!TryComp<LanguageSpeakerComponent>(uid, out var language))
            return;
        language.Languages.Clear();
        AddSpokenLanguage(uid, "SignLanguage");
        SelectDefaultLanguage(uid);
        UpdateUi(uid);
    }

    private void OnCanHear(EntityUid uid, DeafTraitComponent comp, ref CanHearVoiceEvent args)
    {
        args.Cancelled = true;
        if (args.Whisper)
            return;

        _popup.PopupEntity(Loc.GetString("popup-deaf-cannot-hear", ("entity", Identity.Entity(args.Source, EntityManager))),
                            args.Source,
                            uid,
                            PopupType.Small);
    }

    private void OnRadioRecieveAttempt(EntityUid uid, DeafTraitComponent comp, ref RadioReceiveAttemptEvent args)
    {
        args.Cancelled = true;
    }

}
