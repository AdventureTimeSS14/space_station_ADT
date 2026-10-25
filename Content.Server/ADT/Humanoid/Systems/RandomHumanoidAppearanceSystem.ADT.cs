using System.Linq;
using Content.Server.ADT.NPC;
using Content.Server.Humanoid.Components;
using Content.Server.RandomMetadata;
using Content.Shared.ADT.Language;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Humanoid.Systems;

public sealed partial class RandomHumanoidAppearanceSystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RandomMetadataSystem _randomMetadata = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private ADTNpcRosterSystem _roster = default!;

    private static bool IsFemale(HumanoidCharacterProfile profile)
    {
        return profile.Sex == Sex.Female;
    }

    private static List<T> ForSex<T>(HumanoidCharacterProfile profile, List<T> male, List<T> female)
    {
        return IsFemale(profile) && female.Count > 0 ? female : male;
    }

    private HumanoidCharacterProfile ApplyADTRestrictions(EntityUid uid, RandomHumanoidAppearanceComponent component, HumanoidCharacterProfile profile)
    {
        component.RosterName = null;

        if (component.Roster is { } roster && _roster.Take(roster, MetaData(uid).EntityPrototype?.ID) is { } entry)
        {
            component.RosterName = Loc.GetString(entry.Name);

            if (profile.Sex != entry.Sex)
            {
                profile = HumanoidCharacterProfile.Random(
                    HumanoidCharacterProfile.RandomizeConfigAll &
                    ~(HumanoidCharacterProfile.RandomizeCfg.Species | HumanoidCharacterProfile.RandomizeCfg.Sex),
                    new HumanoidCharacterProfile().WithSpecies(profile.Species).WithSex(entry.Sex));
            }
        }

        var voices = ForSex(profile, component.Voices, component.FemaleVoices);

        if (voices.Count > 0)
            profile.TTSVoice = _random.Pick(voices);

        var styles = ForSex(profile, component.HairStyles, component.FemaleHairStyles);
        Color? hairColor = component.HairColors.Count > 0 ? _random.Pick(component.HairColors) : null;
        var hairStyle = styles.Count > 0 ? _random.Pick(styles) : (ProtoId<MarkingPrototype>?) null;

        foreach (var layers in profile.Appearance.Markings.Values)
        {
            if (component.ClearMarkings)
            {
                foreach (var layer in layers.Keys.ToList())
                {
                    if (layer is HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair)
                        continue;

                    layers[layer] = new List<Marking>();
                }
            }

            if (component.ClearFemaleFacialHair && IsFemale(profile) && layers.ContainsKey(HumanoidVisualLayers.FacialHair))
                layers[HumanoidVisualLayers.FacialHair] = new List<Marking>();

            if (hairStyle != null && layers.ContainsKey(HumanoidVisualLayers.Hair))
            {
                var color = hairColor ?? profile.Appearance.HairColor.FirstOrDefault(Color.Black);
                layers[HumanoidVisualLayers.Hair] = new List<Marking> { new(hairStyle.Value, new[] { color }) };
            }

            if (hairColor != null)
            {
                foreach (var layer in new[] { HumanoidVisualLayers.Hair, HumanoidVisualLayers.FacialHair })
                {
                    if (!layers.TryGetValue(layer, out var markings))
                        continue;

                    layers[layer] = markings
                        .Select(marking => new Marking(marking.MarkingId, Enumerable.Repeat(hairColor.Value, Math.Max(1, marking.MarkingColors.Count))))
                        .ToList();
                }
            }
        }

        if (hairColor != null)
            profile.Appearance.HairColor = new List<Color> { hairColor.Value };

        if (component.SkinTone is { } tone &&
            _proto.TryIndex<SpeciesPrototype>(profile.Species, out var species) &&
            _proto.TryIndex(species.SkinColoration, out var coloration) &&
            coloration.Strategy.InputType == SkinColorationStrategyInput.Unary)
        {
            profile.Appearance.SkinColor = coloration.Strategy.FromUnary(_random.NextFloat(tone.X, tone.Y));
        }

        return profile;
    }

    private void ApplyADTProfile(EntityUid uid, RandomHumanoidAppearanceComponent component, HumanoidCharacterProfile profile)
    {
        var segments = ForSex(profile, component.NameSegments, component.FemaleNameSegments);

        if (component.RosterName is { } rosterName)
            _metaData.SetEntityName(uid, rosterName);
        else if (segments.Count > 0 && component.NameFormat is { } format)
            _metaData.SetEntityName(uid, _randomMetadata.GetRandomFromSegments(segments, format));

        if (component.Languages.Count == 0 && component.Language == null)
            return;

        if (!TryComp<LanguageSpeakerComponent>(uid, out var speaker))
            return;

        foreach (var language in component.Languages)
            speaker.Languages[language] = LanguageKnowledge.Speak;

        if (component.Language is { } current)
        {
            speaker.Languages[current] = LanguageKnowledge.Speak;
            speaker.CurrentLanguage = current;
        }

        Dirty(uid, speaker);
        _language.UpdateUi(uid, speaker);
    }
}
