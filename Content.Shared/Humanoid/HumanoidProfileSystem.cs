using Content.Shared.ADT.TTS;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.ADT.Language;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Enums;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private SharedLanguageSystem _language = default!; // ADT-Tweak

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    // ADT-Tweak start
    public void SetSex(Entity<HumanoidProfileComponent?> ent, Sex newSex)
    {
        var comp = ent.Comp;
        if (comp == null)
            return;

        var oldSex = comp.Sex;
        if (oldSex == newSex)
            return;

        comp.Sex = newSex;
        Dirty(ent);

        var sexChanged = new SexChangedEvent(oldSex, newSex);
        RaiseLocalEvent(ent, ref sexChanged);
    }
    // ADT-Tweak end

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Voice = profile.Voice;
        SetSex(ent, profile.Sex); // ADT-Tweak
        Dirty(ent);

        var voiceChanged = new VoiceChangedEvent(ent.Comp.Voice, profile.Voice);
        RaiseLocalEvent(ent, ref voiceChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }

        // ADT-Tweak-Start
        if (TryComp<TTSComponent>(ent, out var tts))
            tts.VoicePrototypeId = profile.TTSVoice;

        if (TryComp<SpeechBarksComponent>(ent, out var barks))
        {
            barks.Data = profile.Bark;
            if (ProtoMan.TryIndex(barks.Data.Proto, out BarkPrototype? barkProto))
                barks.Data.Sound = barkProto.Sound;
        }

        var languageSpeaker = EnsureComp<LanguageSpeakerComponent>(ent);
        languageSpeaker.Languages.Clear();
        foreach (var lang in profile.Languages)
            languageSpeaker.Languages[lang.ToString()] = LanguageKnowledge.Speak;

        if (ProtoMan.TryIndex(ent.Comp!.Species, out var speciesProto))
        {
            foreach (var forced in speciesProto.ForceLanguages)
                languageSpeaker.Languages.TryAdd(forced.ToString(), LanguageKnowledge.Speak);
        }

        _language.SelectDefaultLanguage(ent);
        _language.UpdateUi(ent);
        // ADT-Tweak-End
    }

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (ProtoMan.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!ProtoMan.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
    // ADT-Tweak start
    public void SetGender(Entity<HumanoidProfileComponent?> ent, Gender newGender)
    {
        var comp = ent.Comp;
        if (comp == null)
            return;

        if (comp.Gender == newGender)
            return;

        comp.Gender = newGender;
        Dirty(ent);
    }

    public void SetAge(Entity<HumanoidProfileComponent?> ent, int newAge)
    {
        var comp = ent.Comp;
        if (comp == null)
            return;

        if (comp.Age == newAge)
            return;

        comp.Age = newAge;
        Dirty(ent);
    }
    // ADT-Tweak end
}
