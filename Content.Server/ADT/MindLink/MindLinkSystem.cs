using Content.Server.ADT.Language;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.MindLink;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.MindLink;

/// <summary>
/// Управляет ментальными связями между парами сущностей: выдаёт язык связи
/// и снимает его при смерти или удалении одного из участников.
/// </summary>
public sealed class MindLinkSystem : EntitySystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindLinkComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<MindLinkComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    /// <summary>
    /// Связывает пару сущностей ментальной связью на языке <paramref name="language"/>.
    /// Язык выдаётся обеим сторонам, но только если хотя бы одна из них уже владеет им.
    /// </summary>
    public bool TryAddMindLink(EntityUid a, EntityUid b, ProtoId<LanguagePrototype> language)
    {
        if (HasComp<MindLinkComponent>(a) || HasComp<MindLinkComponent>(b))
        {
            Log.Debug($"Can't add mind link to {ToPrettyString(a)} and {ToPrettyString(b)}: link already exists");
            return false;
        }

        if (!KnowsLanguage(a, language) && !KnowsLanguage(b, language))
        {
            Log.Debug($"Can't add mind link to {ToPrettyString(a)} and {ToPrettyString(b)}: neither side knows language {language}");
            return false;
        }

        var aLink = EnsureComp<MindLinkComponent>(a);
        aLink.Partner = b;
        aLink.Language = language;

        var bLink = EnsureComp<MindLinkComponent>(b);
        bLink.Partner = a;
        bLink.Language = language;

        _language.AddSpokenLanguage(a, language);
        _language.AddSpokenLanguage(b, language);
        return true;
    }

    /// <summary>
    /// Разрывает связь сущности <paramref name="uid"/> с её партнёром.
    /// </summary>
    public void RemoveMindLink(EntityUid uid)
    {
        if (!TryComp<MindLinkComponent>(uid, out var link))
            return;

        var partner = link.Partner;
        var language = link.Language;
        RemComp<MindLinkComponent>(uid);
        if (!HasLanguageFromPrototype(uid, language))
            _language.RemoveLanguage(uid, language);

        if (partner == null || !TryComp<MindLinkComponent>(partner.Value, out var partnerLink) || partnerLink.Partner != uid)
            return;

        RemComp<MindLinkComponent>(partner.Value);
        if (!HasLanguageFromPrototype(partner.Value, language))
            _language.RemoveLanguage(partner.Value, language);
    }

    private void OnTerminating(Entity<MindLinkComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveMindLink(ent.Owner);
    }

    private void OnMobStateChanged(Entity<MindLinkComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemoveMindLink(ent.Owner);
    }

    private bool KnowsLanguage(EntityUid uid, ProtoId<LanguagePrototype> language)
    {
        return TryComp<LanguageSpeakerComponent>(uid, out var comp) && comp.Languages.ContainsKey(language);
    }

    // Не снимать язык, которым сторона владеет из прототипа, иначе связь будет невозможна повторно.
    private bool HasLanguageFromPrototype(EntityUid uid, ProtoId<LanguagePrototype> language)
    {
        if (!TryComp<MetaDataComponent>(uid, out var meta) || meta.EntityPrototype == null)
            return false;

        if (!meta.EntityPrototype.TryGetComponent<LanguageSpeakerComponent>(out var comp, _componentFactory))
            return false;

        return comp.Languages.ContainsKey(language);
    }
}