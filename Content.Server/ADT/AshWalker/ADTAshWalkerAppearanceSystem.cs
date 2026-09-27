using System.Linq;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Language;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.AshWalker;

public sealed class ADTAshWalkerAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    private const float MarkingChance = 0.75f;
    private const string TribeLanguage = "ADTAshWalkerCollectiveMind";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshWalkerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTAshWalkerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        var appearance = profile.Appearance;
        appearance.Markings = RandomMarkings(humanoid.Species, profile.Sex, appearance.SkinColor);
        profile = profile.WithCharacterAppearance(appearance);

        _visualBody.ApplyProfileTo(ent.Owner, profile);
        _humanoidProfile.ApplyProfileTo(ent.Owner, profile);
        _metaData.SetEntityName(ent, profile.Name);

        var speaker = EnsureComp<LanguageSpeakerComponent>(ent);
        speaker.Languages[TribeLanguage] = ent.Comp.Shaman ? LanguageKnowledge.Speak : LanguageKnowledge.Understand;
        _language.UpdateUi(ent);
    }

    private Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> RandomMarkings(
        string species,
        Sex sex,
        Color skinColor)
    {
        var result = new List<Marking>();

        foreach (var (_, data) in _markings.GetMarkingData(species))
        {
            foreach (var layer in data.Layers)
            {
                if (!_random.Prob(MarkingChance))
                    continue;

                var options = _markings.MarkingsByLayerAndGroupAndSex(layer, data.Group, sex).Values.ToList();

                if (options.Count == 0)
                    continue;

                var marking = _random.Pick(options);
                result.Add(new Marking(marking.ID, Enumerable.Repeat(skinColor, marking.Sprites.Count)));
            }
        }

        return _markings.ConvertMarkings(result, species);
    }
}
