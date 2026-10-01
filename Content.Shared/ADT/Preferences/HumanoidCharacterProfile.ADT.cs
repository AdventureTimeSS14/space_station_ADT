using System.Linq;
using Content.Shared.ADT.CharecterFlavor;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Sponsors;
using Content.Shared.ADT.SpeechBarks;
using Content.Shared.ADT.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Traits;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    public const string DefaultTTSVoice = "VoiceHuman";

    public static readonly Dictionary<Sex, string> DefaultSexTTSVoice = new()
    {
        { Sex.Male, "VoiceHumanMale" },
        { Sex.Female, "VoiceHumanFemale" },
        { Sex.Unsexed, "VoiceHuman" }
    };

    public const int MaxNameLength = 96;
    public const int MaxLoadoutNameLength = 32;
    public const int MaxDescLength = 512;

    private static readonly ProtoId<TraitPrototype> PolyglotTraitId = "Polyglot";

    [DataField]
    public string OOCNotes { get; set; } = string.Empty;

    [DataField]
    public string HeadshotUrl { get; private set; } = string.Empty;

    [DataField]
    public string ExploitableInfo { get; set; } = string.Empty;

    [DataField]
    public string TTSVoice { get; set; } = DefaultTTSVoice;

    [DataField]
    public BarkData Bark = new();

    [DataField]
    private HashSet<ProtoId<LanguagePrototype>> _languages = new();

    public IReadOnlySet<ProtoId<LanguagePrototype>> Languages => _languages;

    public int LanguageSlotsBonus => TraitPreferences.Contains(PolyglotTraitId) ? 1 : 0;

    public void SetHeadshotUrl(string url, string allowedDomain)
    {
        HeadshotUrl = HeadshotHashHelper.IsValidHeadshotUrl(url, allowedDomain)
            ? url
            : string.Empty;
    }

    public HumanoidCharacterProfile SetADTData(
        string ttsVoice,
        BarkData bark,
        HashSet<ProtoId<LanguagePrototype>> languages,
        string oocNotes,
        string headshotUrl,
        string exploitableInfo)
    {
        TTSVoice = ttsVoice;
        Bark = bark;
        _languages = languages;
        OOCNotes = oocNotes;
        HeadshotUrl = headshotUrl;
        ExploitableInfo = exploitableInfo;
        return this;
    }

    private void CopyADTFrom(HumanoidCharacterProfile other)
    {
        TTSVoice = other.TTSVoice;
        Bark = other.Bark;
        _languages = new HashSet<ProtoId<LanguagePrototype>>(other._languages);
        OOCNotes = other.OOCNotes;
        HeadshotUrl = other.HeadshotUrl;
        ExploitableInfo = other.ExploitableInfo;
    }

    private static HashSet<ProtoId<LanguagePrototype>> DefaultLanguagesFor(ProtoId<SpeciesPrototype> species)
    {
        return IoCManager.Resolve<IPrototypeManager>().Index(species).DefaultLanguages.ToHashSet();
    }

    private void RandomizeADT(SpeciesPrototype species)
    {
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        _languages = species.DefaultLanguages.ToHashSet();

        var voices = prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(o => CanHaveVoice(o, Sex, Species))
            .ToArray();

        if (voices.Length > 0)
            TTSVoice = random.Pick(voices).ID;
    }

    private bool MemberwiseEqualsADT(HumanoidCharacterProfile other)
    {
        if (TTSVoice != other.TTSVoice)
            return false;

        if (!_languages.SequenceEqual(other._languages))
            return false;

        if (OOCNotes != other.OOCNotes)
            return false;

        if (HeadshotUrl != other.HeadshotUrl)
            return false;

        if (ExploitableInfo != other.ExploitableInfo)
            return false;

        return Bark.MemberwiseEquals(other.Bark);
    }

    private void AddHashCodeADT(ref HashCode hashCode)
    {
        hashCode.Add(OOCNotes);
        hashCode.Add(HeadshotUrl);
        hashCode.Add(ExploitableInfo);
        hashCode.Add(TTSVoice);
    }

    private void EnsureValidADT(
        ICommonSession session,
        IDependencyCollection collection,
        SpeciesPrototype speciesPrototype,
        int maxFlavorTextLength)
    {
        var prototypeManager = collection.Resolve<IPrototypeManager>();

        OOCNotes = FormattedMessage.RemoveMarkupOrThrow(OOCNotes);
        if (OOCNotes.Length > maxFlavorTextLength)
            OOCNotes = OOCNotes[..maxFlavorTextLength];

        ExploitableInfo = FormattedMessage.RemoveMarkupOrThrow(ExploitableInfo);
        if (ExploitableInfo.Length > maxFlavorTextLength)
            ExploitableInfo = ExploitableInfo[..maxFlavorTextLength];

        prototypeManager.TryIndex<TTSVoicePrototype>(TTSVoice, out var voice);
        if (voice is null
            || !CanHaveVoice(voice, Sex, Species)
            || !SponsorProfileValidation.IsTtsVoiceAllowed(session, collection, voice))
        {
            TTSVoice = DefaultSexTTSVoice[Sex];
        }

        if (_languages.Count <= 0)
            _languages = new(speciesPrototype.DefaultLanguages);

        List<ProtoId<LanguagePrototype>> langsInvalid = new();
        foreach (var language in _languages)
        {
            if (!prototypeManager.Index(language).Roundstart && !speciesPrototype.UniqueLanguages.Contains(language))
                langsInvalid.Add(language);
        }

        foreach (var lang in langsInvalid)
        {
            _languages.Remove(lang);
        }

        var maxLanguages = speciesPrototype.MaxLanguages + LanguageSlotsBonus;
        if (_languages.Count > maxLanguages)
        {
            var required = new HashSet<ProtoId<LanguagePrototype>>(speciesPrototype.DefaultLanguages);
            required.UnionWith(speciesPrototype.UniqueLanguages);
            foreach (var lang in _languages.ToList())
            {
                if (_languages.Count <= maxLanguages)
                    break;

                if (required.Contains(lang))
                    continue;

                _languages.Remove(lang);
            }
        }

        GetQuirkPoints();
    }

    public static bool CanHaveVoice(TTSVoicePrototype voice, Sex sex, ProtoId<SpeciesPrototype> species)
    {
        if (voice.SpeciesBlacklist.Contains(species))
            return false;

        if (voice.SpeciesWhitelist.Count > 0 && !voice.SpeciesWhitelist.Contains(species))
            return false;

        return voice.RoundStart && sex == Sex.Unsexed || (voice.Sex == sex || voice.Sex == Sex.Unsexed);
    }

    public HumanoidCharacterProfile WithOOCNotes(string oocNotes)
    {
        return new(this) { OOCNotes = oocNotes };
    }

    public HumanoidCharacterProfile WithHeadshotUrl(string headshotUrl)
    {
        return new(this) { HeadshotUrl = headshotUrl };
    }

    public HumanoidCharacterProfile WithExploitableInfo(string exploitableInfo)
    {
        return new(this) { ExploitableInfo = exploitableInfo };
    }

    public HumanoidCharacterProfile WithTTSVoice(string voice)
    {
        return new(this) { TTSVoice = voice };
    }

    public HumanoidCharacterProfile WithBarkProto(string bark)
    {
        return new(this) { Bark = Bark.WithProto(bark) };
    }

    public HumanoidCharacterProfile WithBarkPitch(float pitch)
    {
        return new(this) { Bark = Bark.WithPitch(pitch) };
    }

    public HumanoidCharacterProfile WithBarkMinVariation(float variation)
    {
        return new(this) { Bark = Bark.WithMinVar(variation) };
    }

    public HumanoidCharacterProfile WithBarkMaxVariation(float variation)
    {
        return new(this) { Bark = Bark.WithMaxVar(variation) };
    }

    public HumanoidCharacterProfile WithLanguage(ProtoId<LanguagePrototype> language)
    {
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var species = proto.Index(Species);
        if (!proto.Index(language).Roundstart && !species.UniqueLanguages.Contains(language))
            return new(this);

        if (_languages.Contains(language))
            return new(this);

        if (_languages.Count >= species.MaxLanguages + LanguageSlotsBonus)
            return new(this);

        HashSet<ProtoId<LanguagePrototype>> list = new(_languages);
        list.Add(language);

        return new(this)
        {
            _languages = list,
        };
    }

    public HumanoidCharacterProfile WithoutLanguage(ProtoId<LanguagePrototype> language)
    {
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var species = proto.Index(Species);
        if (!proto.Index(language).Roundstart && !species.UniqueLanguages.Contains(language))
            return new(this);

        if (!_languages.Contains(language))
            return new(this);

        if (_languages.Count <= 1)
            return new(this);

        HashSet<ProtoId<LanguagePrototype>> list = new(_languages);
        list.Remove(language);

        return new(this)
        {
            _languages = list,
        };
    }

    public bool CanToggleQuirk(TraitPrototype proto)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var list = TraitPreferences.Where(x => protoMan.Index(x).Quirk);

        var points = 0;
        foreach (var item in list)
        {
            points -= protoMan.Index(item).Cost;
        }

        if (list.Contains(proto.ID) && points + proto.Cost < 0)
            return false;

        if (!list.Contains(proto.ID) && points < proto.Cost)
            return false;

        return true;
    }

    public int GetQuirkPoints()
    {
        var count = 0;
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var quirks = TraitPreferences.Where(x => proto.Index(x).Quirk);
        foreach (var item in quirks)
        {
            count += proto.Index(item).Cost;
        }

        if (count > 0)
        {
            _traitPreferences.Clear();
            count = 0;
        }

        return -count;
    }
}
