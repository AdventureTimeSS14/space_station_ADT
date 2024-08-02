using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Content.Shared.Ghost;
using Robust.Shared.Network;

namespace Content.Shared.ADT.Language;

public abstract class SharedLanguageSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        
    }

    public bool CanSpeak(EntityUid uid, LanguagePrototype proto, LanguageSpeakerComponent? component = null)
    {
        if (HasComp<GhostComponent>(uid))
            return false;

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
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

        if (!Resolve(uid, ref component))
            return false;

        if (proto.ID == "Universal")
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

        foreach (var lang in component.UnderstoodLanguages)
            if (lang == proto.ID)
                return true;

        return false;
    }

    // Unholy shit
    public bool CheckTranslators(EntityUid uid, EntityUid source, LanguagePrototype proto)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var uidManager))
            return false;
        if (!TryComp<LanguageSpeakerComponent>(uid, out var comp))
            return false;
        bool canTranslate = false;
        bool canUnderstandTranslator = false;

        foreach (var container in uidManager.Containers.Values)
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp<HandheldTranslatorComponent>(entity, out var translator) && translator.Enabled)
                {
                    foreach (var item in translator.ToUnderstand)
                    {
                        if (item == proto.ID)
                            canTranslate = true;
                    }
                    if (!TryComp<LanguageSpeakerComponent>(uid, out var sourceLang))
                    {
                        canUnderstandTranslator = false;
                        continue;
                    }

                    foreach (var lang in translator.ToSpeak)
                    {
                        foreach (var understoodLangs in sourceLang.UnderstoodLanguages)
                        {
                            if (lang == understoodLangs)
                                canUnderstandTranslator = true;
                        }
                    }

                }
            }
        }

        if (canTranslate && canUnderstandTranslator)
            return true;

        canTranslate = false;
        canUnderstandTranslator = false;

        if (!TryComp<ContainerManagerComponent>(source, out var sourceManager))
            return false;

        foreach (var container in sourceManager.Containers.Values)
        {
            foreach (var entity in container.ContainedEntities)
            {
                if (TryComp<HandheldTranslatorComponent>(entity, out var translator) && translator.Enabled)
                {
                    foreach (var item in translator.ToSpeak)
                    {
                        if (item == proto.ID)
                            canTranslate = true;
                    }
                    if (!TryComp<LanguageSpeakerComponent>(source, out var sourceLang))
                    {
                        canUnderstandTranslator = false;
                        continue;
                    }

                    foreach (var lang in translator.ToUnderstand)
                    {
                        foreach (var understoodLangs in sourceLang.SpokenLanguages)
                        {
                            if (lang == understoodLangs)
                                canUnderstandTranslator = true;
                        }
                    }

                }
            }
        }

        if (canTranslate && canUnderstandTranslator)
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
            component.CurrentLanguage = language;
            RaiseLocalEvent(new LanguageMenuStateMessage(ent, language, component.UnderstoodLanguages));
        }

        RaiseNetworkEvent(new LanguageChosenMessage(ent, language));
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

    public LanguageMenuStateMessage(NetEntity componentOwner, string currentLanguage, List<string> options)
    {
        ComponentOwner = componentOwner;
        CurrentLanguage = currentLanguage;
        Options = options;
    }
}
