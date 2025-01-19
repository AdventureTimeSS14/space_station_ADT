using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Toggleable;

namespace Content.Shared.ADT.Language;

public abstract class SharedTranslatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldTranslatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<HandheldTranslatorComponent, GetLanguagesEvent>(OnTranslatorGetLanguages);
        SubscribeLocalEvent<HandsComponent, GetLanguagesEvent>(OnGetLanguages);
    }

    private void OnGetLanguages(EntityUid uid, HandsComponent comp, ref GetLanguagesEvent args)
    {
        foreach (var (_, hand) in comp.Hands)
        {
            if (hand.HeldEntity.HasValue)
                RaiseLocalEvent(hand.HeldEntity.Value, ref args);
        }
    }

    private void OnTranslatorGetLanguages(EntityUid uid, HandheldTranslatorComponent comp, ref GetLanguagesEvent args)
    {
        if (!comp.Enabled)
            return;
        if (!TryComp<LanguageSpeakerComponent>(comp.User, out var speaker))
            return;
        if (speaker.Languages.Keys.Where(x => comp.Languages.ContainsKey(x)).Count() <= 0)
            return;

        foreach (var (key, value) in comp.Languages)
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

    private void OnExamined(EntityUid uid, HandheldTranslatorComponent component, ExaminedEvent args)
    {
        var state = Loc.GetString(component.Enabled
            ? "translator-enabled"
            : "translator-disabled");

        args.PushMarkup(state);
    }

    protected void OnAppearanceChange(EntityUid translator, HandheldTranslatorComponent? comp = null)
    {
        if (comp == null && !TryComp(translator, out comp))
            return;

        _appearance.SetData(translator, ToggleVisuals.Toggled, comp.Enabled);
    }
}
