using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Ghost;
using Robust.Shared.Network;
using Content.Shared.Hands.EntitySystems;
using System.Linq;
using Content.Shared.Implants.Components;

namespace Content.Shared.ADT.Language;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LanguageSpeakerComponent, GetLanguagesEvent>(OnGetLanguages);
    }

    public ProtoId<LanguagePrototype> Universal = "Universal";

    private void OnGetLanguages(EntityUid uid, LanguageSpeakerComponent comp, ref GetLanguagesEvent args)
    {
        args.Current = comp.CurrentLanguage ?? Universal;
        args.Languages = comp.Languages;

        if (_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
        {
            foreach (var item in implantContainer.ContainedEntities)
            {
                RaiseLocalEvent(item, ref args);
            }
        }
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

        if (!GetLanguagesKnowledged(uid, LanguageKnowledge.BadSpeak, out var langs, out _))
            return false;

        if (langs.ContainsKey(protoId))
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

        if (!GetLanguagesKnowledged(uid, LanguageKnowledge.Understand, out var langs, out _))
            return false;

        if (langs.ContainsKey(protoId))
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

        component.CurrentLanguage = component.Languages.Where(x => (int)x.Value >= 1).ToDictionary().Keys.FirstOrDefault("Universal");

        GetLanguages(uid, out var langs, out var translator, out var current);

        Dirty(uid, component);

        RaiseNetworkEvent(new LanguageMenuStateMessage(GetNetEntity(uid), current, langs, translator));
        RaiseNetworkEvent(new LanguageChosenMessage(GetNetEntity(uid), current));
    }

    public bool GetLanguages(
        EntityUid? player,
        out Dictionary<string, LanguageKnowledge> langs,
        out Dictionary<string, LanguageKnowledge> translator,
        out string current)
    {

        langs = new();
        translator = new();
        current = String.Empty;

        if (player == null)
            return false;
        var uid = player.Value;

        var ev = new GetLanguagesEvent(uid);
        RaiseLocalEvent(uid, ref ev);

        langs = ev.Languages;
        translator = ev.Translator;
        current = ev.Current;

        if (translator.Count() <= 0 || langs.Count() <= 0 || current == String.Empty)
            return false;

        return true;
    }

    public bool GetLanguagesKnowledged(
        EntityUid? player,
        LanguageKnowledge required,
        out Dictionary<string, LanguageKnowledge> langs,
        out string current)
    {

        langs = new();
        current = String.Empty;

        if (player == null)
            return false;
        var uid = player.Value;

        var ev = new GetLanguagesEvent(uid);
        RaiseLocalEvent(uid, ref ev);

        langs = ev.Languages.Where(x => x.Value >= required).ToDictionary();
        foreach (var item in ev.Languages)
        {
            if (ev.Translator.ContainsKey(item.Key) && ev.Translator[item.Key] > item.Value)
                langs[item.Key] = ev.Translator[item.Key];
        }

        current = ev.Current;

        if (langs.Count() <= 0 || current == String.Empty)
            return false;

        return true;
    }

    public LanguagePrototype GetLanguage(string id)
    {
        if (!_proto.TryIndex<LanguagePrototype>(id, out var result))
            return _proto.Index<LanguagePrototype>(Universal);

        return result;
    }

    public void AddSpokenLanguage(EntityUid uid, string lang, LanguageKnowledge knowledge = LanguageKnowledge.Speak, LanguageSpeakerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        if (!_proto.TryIndex<LanguagePrototype>(lang, out var proto))
            return;
        if (comp.Languages.ContainsKey(lang))
            comp.Languages[lang] = knowledge;
        else
            comp.Languages.Add(lang, knowledge);

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
    public Dictionary<string, LanguageKnowledge> Options;
    public Dictionary<string, LanguageKnowledge> TranslatorOptions;

    public LanguageMenuStateMessage(NetEntity componentOwner, string currentLanguage, Dictionary<string, LanguageKnowledge> options, Dictionary<string, LanguageKnowledge> translatorOptions)
    {
        ComponentOwner = componentOwner;
        CurrentLanguage = currentLanguage;
        Options = options;
        TranslatorOptions = translatorOptions;
    }
}
