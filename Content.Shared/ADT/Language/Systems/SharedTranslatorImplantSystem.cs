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
        SubscribeLocalEvent<TranslatorImplantComponent, GetLanguagesEvent>(OnGetLanguages);
        SubscribeLocalEvent<TranslatorImplantComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnGetLanguages(EntityUid uid, TranslatorImplantComponent component, ref GetLanguagesEvent args)
    {
        foreach (var (key, value) in component.Languages)
        {
            if (args.Translator.ContainsKey(key))
            {
                if (args.Translator[key] >= value)
                    continue;
                args.Translator[key] = value;
            }
            else
                args.Translator.Add(key, value);
        }
    }

    private void OnRemoveAttempt(EntityUid uid, TranslatorImplantComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (component.Permanent && component.ImplantedEntity != null)
            args.Cancel();
    }

}
