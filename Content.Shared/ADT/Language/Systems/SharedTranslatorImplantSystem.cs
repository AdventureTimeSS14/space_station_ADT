using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Content.Shared.ADT.Language;

namespace Content.Shared.Implants;

public abstract class SharedTranslatorImplantSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TranslatorImplantComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<TranslatorImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<TranslatorImplantComponent, EntGotRemovedFromContainerMessage>(OnRemove);
    }

    private void OnInsert(EntityUid uid, TranslatorImplantComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<LanguageSpeakerComponent>(args.Container.Owner, out var languageSpeaker))
            return;

        if (component.ToUnderstand.Count > 0)
        {
            foreach (var item in component.ToUnderstand)
            {
                if (_language.CanUnderstand(args.Container.Owner, _language.GetLanguage(item)))
                    continue;
                languageSpeaker.UnderstoodLanguages.Add(item);
                component.ImplantedToUnderstand.Add(item);
            }
        }

        if (component.ToSpeak.Count > 0)
        {
            foreach (var item in component.ToSpeak)
            {
                if (_language.CanSpeak(args.Container.Owner, _language.GetLanguage(item)))
                    continue;
                languageSpeaker.SpokenLanguages.Add(item);
                component.ImplantedToSpeak.Add(item);
            }
        }

        component.ImplantedEntity = args.Container.Owner;

        Dirty(component.ImplantedEntity.Value, languageSpeaker);

        if (!_language.GetLanguages(component.ImplantedEntity.Value, out var understood, out _, out var translatorUnderstood, out _, out var current))
            return;

        var menuEv = new LanguageMenuStateMessage(GetNetEntity(component.ImplantedEntity.Value), current, understood.ToList(), translatorUnderstood.ToList());
        RaiseNetworkEvent(menuEv);

        var ev = new ImplantImplantedEvent(uid, component.ImplantedEntity.Value);
        RaiseLocalEvent(uid, ref ev);
    }
    private void OnRemoveAttempt(EntityUid uid, TranslatorImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }

    private void OnRemove(EntityUid uid, TranslatorImplantComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (component.ImplantedEntity == null || Terminating(component.ImplantedEntity.Value))
            return;

        if (!TryComp<LanguageSpeakerComponent>(component.ImplantedEntity.Value, out var languageSpeaker))
            return;

        if (component.ImplantedToUnderstand.Count > 0)
        {
            foreach (var item in component.ImplantedToUnderstand)
            {
                languageSpeaker.UnderstoodLanguages.Remove(item);
            }
        }

        if (component.ImplantedToSpeak.Count > 0)
        {
            foreach (var item in component.ImplantedToSpeak)
            {
                languageSpeaker.SpokenLanguages.Remove(item);
                if (languageSpeaker.CurrentLanguage == item)
                    languageSpeaker.CurrentLanguage = languageSpeaker.SpokenLanguages.FirstOrDefault("Universal");
            }
        }


        component.ImplantedToUnderstand.Clear();
        component.ImplantedToSpeak.Clear();

        Dirty(component.ImplantedEntity.Value, languageSpeaker);

        if (!_language.GetLanguages(component.ImplantedEntity.Value, out var understood, out _, out var translatorUnderstood, out _, out var current))
            return;

        var menuEv = new LanguageMenuStateMessage(GetNetEntity(component.ImplantedEntity.Value), current, understood.ToList(), translatorUnderstood.ToList());
        RaiseNetworkEvent(menuEv);

        component.ImplantedEntity = null;
    }

}
