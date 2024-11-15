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

    }

    public ProtoId<LanguagePrototype> GalacticCommon = "GalacticCommon";
    public ProtoId<LanguagePrototype> Universal = "Universal";

    public bool CanSpeak(EntityUid uid, LanguagePrototype proto, LanguageSpeakerComponent? component = null)
    {
        if (HasComp<GhostComponent>(uid))
            return false;

        if (HasComp<UniversalLanguageSpeakerComponent>(uid))
            return true;

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
            return true;

        if (GetLanguages(uid, out _, out _, out _, out var translator, out _) && translator.Contains(proto.ID))
            return true;

        foreach (var lang in component.SpokenLanguages)
            if (lang == proto.ID)
                return true;

        return false;
    }

    public bool CanUnderstand(EntityUid uid, LanguagePrototype proto, LanguageSpeakerComponent? component = null)
    {
        if (HasComp<GhostComponent>(uid))
            return true;

        if (HasComp<UniversalLanguageSpeakerComponent>(uid))
            return true;

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
            return true;

        if (GetLanguages(uid, out _, out _, out var translator, out _, out _) && translator.Contains(proto.ID))
            return true;

        foreach (var lang in component.UnderstoodLanguages)
            if (lang == proto.ID)
                return true;

        return false;
    }

    public bool CanSpeak(EntityUid uid, string protoId, LanguageSpeakerComponent? component = null)
    {
        if (!_proto.TryIndex<LanguagePrototype>(protoId, out var proto))
            return false;

        if (HasComp<GhostComponent>(uid))
            return false;

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
            return true;

        if (GetLanguages(uid, out _, out _, out _, out var translator, out _) && translator.Contains(protoId))
            return true;

        foreach (var lang in component.SpokenLanguages)
            if (lang == proto.ID)
                return true;

        return false;
    }

    public bool CanUnderstand(EntityUid uid, string protoId, LanguageSpeakerComponent? component = null)
    {
        if (!_proto.TryIndex<LanguagePrototype>(protoId, out var proto))
            return false;

        if (HasComp<GhostComponent>(uid))
            return true;

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
            return true;

        if (GetLanguages(uid, out _, out _, out var translator, out _, out _) && translator.Contains(protoId))
            return true;

        foreach (var lang in component.UnderstoodLanguages)
            if (lang == proto.ID)
                return true;

        return false;
    }

    public LanguagePrototype GetCurrentLanguage(EntityUid uid)
    {
        var universalProto = _proto.Index<LanguagePrototype>("Universal");

        if (!TryComp<LanguageSpeakerComponent>(uid, out var comp) || comp.CurrentLanguage == null)
            return universalProto;

        if (_proto.TryIndex<LanguagePrototype>(comp.CurrentLanguage, out var proto))
            return proto;

        return universalProto;
    }

    public void SelectLanguage(NetEntity ent, string language, LanguageSpeakerComponent? component = null)
    {
        var speaker = GetEntity(ent);

        if (!CanSpeak(speaker, GetLanguage(language)))
            return;

        if (component == null && !TryComp(speaker, out component))
            return;

        if (component.CurrentLanguage == language)
            return;

        if (_netMan.IsClient)
        {
            GetLanguages(speaker, out _, out _, out var translator, out _, out _);
            component.CurrentLanguage = language;
            RaiseLocalEvent(new LanguageMenuStateMessage(ent, language, component.UnderstoodLanguages, translator));
        }

        RaiseNetworkEvent(new LanguageChosenMessage(ent, language));
    }

    public void SelectDefaultLanguage(EntityUid uid, LanguageSpeakerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var language = component.SpokenLanguages.FirstOrDefault("Universal");

        GetLanguages(uid, out _, out _, out var translator, out _, out _);
        component.CurrentLanguage = language;

        Dirty(uid, component);

        RaiseNetworkEvent(new LanguageMenuStateMessage(GetNetEntity(uid), language, component.UnderstoodLanguages, translator));
        RaiseNetworkEvent(new LanguageChosenMessage(GetNetEntity(uid), language));
    }

    public bool GetLanguages(
        EntityUid? player,
        out List<string> understood,
        out List<string> spoken,
        out List<string> translatorUnderstood,
        out List<string> translatorSpoken,
        out string current,
        LanguageSpeakerComponent? comp = null)
    {
        understood = new();
        spoken = new();
        translatorUnderstood = new();
        translatorSpoken = new();
        current = String.Empty;

        if (player == null)
            return false;
        var uid = player.Value;

        if (!Resolve(uid, ref comp))
            return false;

        understood.AddRange(comp.UnderstoodLanguages);
        spoken.AddRange(comp.SpokenLanguages);
        current = GetCurrentLanguage(uid).ID;

        foreach (var item in _hands.EnumerateHeld(uid))
        {
            if (TryComp<HandheldTranslatorComponent>(item, out var translator) && translator.Enabled)
            {
                foreach (var lang in translator.Required)
                {
                    if (understood.Contains(lang))
                    {
                        translatorUnderstood.AddRange(translator.ToUnderstand);
                        translatorSpoken.AddRange(translator.ToSpeak);
                        break;
                    }
                }
            }
        }

        if (understood.Count <= 0 || spoken.Count <= 0 || current == String.Empty)
            return false;

        return true;
    }
    public LanguagePrototype GetLanguage(string id)
    {
        if (!_proto.TryIndex<LanguagePrototype>(id, out var result))
            return _proto.Index<LanguagePrototype>("Universal");

        return result;
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
