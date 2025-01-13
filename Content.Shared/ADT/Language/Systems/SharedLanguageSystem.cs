using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Content.Shared.Hands.EntitySystems;
using System.Linq;

namespace Content.Shared.ADT.Language;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageSpeakerComponent, GetLanguagesEvent>(OnGetLanguages);
    }

    public ProtoId<LanguagePrototype> Universal = "Universal";

    private void OnGetLanguages(EntityUid uid, LanguageSpeakerComponent comp, ref GetLanguagesEvent args)
    {
        args.Current = comp.CurrentLanguage ?? Universal;
        args.Spoken.AddRange(comp.SpokenLanguages);
        args.Understood.AddRange(comp.UnderstoodLanguages);
    }

    public bool CanSpeak(EntityUid uid, LanguagePrototype proto)
    {
        return CanSpeak(uid, proto.ID);
    }

    public bool CanUnderstand(EntityUid uid, LanguagePrototype proto)
    {
        return CanUnderstand(uid, proto.ID);
    }

    public bool CanSpeak(EntityUid uid, string protoId)
    {
        if (!_proto.TryIndex<LanguagePrototype>(protoId, out var proto))
            return false;

        if (HasComp<GhostComponent>(uid))
            return false;

        if (proto.ID == Universal)
            return true;

        if (!GetLanguages(uid, out _, out var spoken, out _, out var translator, out _))
            return false;

        if (spoken.Contains(protoId) || translator.Contains(protoId))
            return true;

        return false;
    }

    public bool CanUnderstand(EntityUid uid, string protoId)
    {
        if (!_proto.TryIndex<LanguagePrototype>(protoId, out var proto))
            return false;

        if (HasComp<GhostComponent>(uid))
            return true;

        if (proto.ID == Universal)
            return true;

        if (!GetLanguages(uid, out var understood, out _, out var translator, out _, out _))
            return false;

        if (understood.Contains(protoId) || translator.Contains(protoId))
            return true;

        return false;
    }

    public LanguagePrototype GetCurrentLanguage(EntityUid uid)
    {
        var universalProto = _proto.Index<LanguagePrototype>(Universal);

        if (!TryComp<LanguageSpeakerComponent>(uid, out var comp) || comp.CurrentLanguage == null)
            return universalProto;

        if (_proto.TryIndex<LanguagePrototype>(comp.CurrentLanguage, out var proto))
            return proto;

        return universalProto;
    }

    public void SelectDefaultLanguage(EntityUid uid, LanguageSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!_netMan.IsServer)
            return;

        var language = component.SpokenLanguages.FirstOrDefault("Universal");

        GetLanguages(uid, out _, out _, out var translator, out _, out _);
        component.CurrentLanguage = language;

        Dirty(uid, component);

        RaiseNetworkEvent(new LanguageMenuStateMessage(GetNetEntity(uid), language, component.UnderstoodLanguages, translator.ToList()));
        RaiseNetworkEvent(new LanguageChosenMessage(GetNetEntity(uid), language));
    }

    public bool GetLanguages(
        EntityUid? player,
        out IEnumerable<string> understood,
        out IEnumerable<string> spoken,
        out IEnumerable<string> translatorUnderstood,
        out IEnumerable<string> translatorSpoken,
        out string current,
        LanguageSpeakerComponent? comp = null)
    {

        understood = new List<string>();
        spoken = new List<string>();
        translatorUnderstood = new List<string>();
        translatorSpoken = new List<string>();
        current = String.Empty;

        if (player == null)
            return false;
        var uid = player.Value;

        var ev = new GetLanguagesEvent(uid);
        RaiseLocalEvent(uid, ref ev);

        understood = ev.Understood.Distinct();
        spoken = ev.Spoken.Distinct();
        translatorUnderstood = ev.TranslatorUnderstood.Distinct();
        translatorSpoken = ev.TranslatorSpoken.Distinct();
        current = ev.Current;

        if (understood.Count() <= 0 || spoken.Count() <= 0 || current == String.Empty)
            return false;

        return true;
    }
    public LanguagePrototype GetLanguage(string id)
    {
        if (!_proto.TryIndex<LanguagePrototype>(id, out var result))
            return _proto.Index<LanguagePrototype>(Universal);

        return result;
    }

    public void AddSpokenLanguage(EntityUid uid, string lang, LanguageSpeakerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!_proto.TryIndex<LanguagePrototype>(lang, out var proto))
            return;
        if (!CanSpeak(uid, lang))
            comp.SpokenLanguages.Add(lang);
        if (!CanUnderstand(uid, lang))
            comp.UnderstoodLanguages.Add(lang);

        UpdateUi(uid, comp);
    }

    public virtual void UpdateUi(EntityUid uid, LanguageSpeakerComponent? comp = null)
    {

    }
}


/// <summary>
///   Sent when a client wants to change its selected language.
/// </summary>
[Serializable, NetSerializable]
public sealed class LanguageChosenMessage : EntityEventArgs
{
    public NetEntity Uid;
    public string SelectedLanguage;

    public LanguageChosenMessage(NetEntity uid, string selectedLanguage)
    {
        Uid = uid;
        SelectedLanguage = selectedLanguage;
    }
}


/// <summary>
///   Sent by the server when the client needs to update its language menu,
///   or directly after [RequestLanguageMenuStateMessage].
/// </summary>
[Serializable, NetSerializable]
public sealed class LanguageMenuStateMessage : EntityEventArgs
{
    public NetEntity ComponentOwner;
    public string CurrentLanguage;
    public List<string> Options;
    public List<string> TranslatorOptions;

    public LanguageMenuStateMessage(NetEntity componentOwner, string currentLanguage, List<string> options, List<string> translatorOptions)
    {
        ComponentOwner = componentOwner;
        CurrentLanguage = currentLanguage;
        Options = options;
        TranslatorOptions = translatorOptions;
    }
}
