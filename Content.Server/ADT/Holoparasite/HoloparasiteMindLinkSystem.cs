using Content.Server.ADT.Language;
using Content.Server.Guardian;
using Content.Shared.ADT.Holoparasite;
using Content.Shared.ADT.Language;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Holoparasite;

/// <summary>
/// Выдаёт ментальную связь паре носитель-голопаразит и снимает её при разрыве.
/// </summary>
public sealed class HoloparasiteMindLinkSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;

    [ValidatePrototypeId<LanguagePrototype>]
    public const string MindLinkLanguage = "ADTHoloparasiteMindLink";

    public override void Initialize()
    {
        base.Initialize();

        // ComponentShutdown занят ванилью: снимаем связь по терминации и смерти голопаразита.
        SubscribeLocalEvent<HoloparasiteMindLinkComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<GuardianComponent, MobStateChangedEvent>(OnGuardianStateChanged);
    }

    public void TryAddMindLink(EntityUid host, EntityUid guardian)
    {
        if (!TryComp<LanguageSpeakerComponent>(guardian, out var guardianLang) ||
            !guardianLang.Languages.ContainsKey(MindLinkLanguage))
        {
            return;
        }

        EnsureComp<LanguageSpeakerComponent>(host);
        _language.AddSpokenLanguage(host, MindLinkLanguage);
        _language.AddSpokenLanguage(guardian, MindLinkLanguage);

        var hostLink = EnsureComp<HoloparasiteMindLinkComponent>(host);
        hostLink.Partner = guardian;

        var guardianLink = EnsureComp<HoloparasiteMindLinkComponent>(guardian);
        guardianLink.Partner = host;
    }

    private void OnTerminating(Entity<HoloparasiteMindLinkComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveMindLink(ent.Owner);
    }

    private void OnGuardianStateChanged(EntityUid uid, GuardianComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemoveMindLink(uid);
    }

    private void RemoveMindLink(EntityUid uid)
    {
        if (!TryComp<HoloparasiteMindLinkComponent>(uid, out var link))
            return;

        var partner = link.Partner;
        RemComp<HoloparasiteMindLinkComponent>(uid);
        _language.RemoveLanguage(uid, MindLinkLanguage);

        if (partner == null || !TryComp<HoloparasiteMindLinkComponent>(partner.Value, out var partnerLink))
            return;

        RemComp<HoloparasiteMindLinkComponent>(partner.Value);
        _language.RemoveLanguage(partner.Value, MindLinkLanguage);
    }
}
