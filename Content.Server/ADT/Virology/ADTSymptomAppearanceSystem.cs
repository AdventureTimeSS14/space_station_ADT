using System.Linq;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.ADT.TTS;
using Content.Shared.Speech.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTSymptomAppearanceSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private HumanoidProfileSystem _profile = default!;
    [Dependency] private NamingSystem _naming = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomSetMarking>>(OnSetMarking);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomClearMarkings>>(OnClearMarkings);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomSkinColor>>(OnSkinColor);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomYouth>>(OnYouth);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomVoiceChange>>(OnVoiceChange);
    }

    private void OnSetMarking(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomSetMarking> args)
    {
        if (!HasComp<VisualBodyComponent>(ent))
            return;

        if (!_proto.HasIndex(args.Effect.Marking))
            return;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner, null, out _, out _, out var applied))
            return;

        var layer = args.Effect.Layer;
        var marking = args.Effect.Marking;
        var keep = args.Effect.Keep;
        var changed = false;

        foreach (var (category, layers) in applied)
        {
            if (!layers.TryGetValue(layer, out var markings))
                continue;

            if (markings.Any(m => keep.Contains(m.MarkingId)) || markings.Any(m => m.MarkingId == marking))
                return;

            layers[layer] = new List<Marking> { new(marking, new List<Color> { Color.Black }) };
            changed = true;
        }

        if (changed)
            _visualBody.ApplyMarkings(ent, applied);
    }

    private void OnClearMarkings(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomClearMarkings> args)
    {
        if (!HasComp<VisualBodyComponent>(ent))
            return;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner, null, out _, out _, out var applied))
            return;

        var changed = false;
        foreach (var (category, layers) in applied)
        {
            foreach (var layer in args.Effect.Layers)
            {
                if (!layers.TryGetValue(layer, out var markings) || markings.Count == 0)
                    continue;

                layers[layer] = new List<Marking>();
                changed = true;
            }
        }

        if (changed)
            _visualBody.ApplyMarkings(ent, applied);
    }

    private void OnSkinColor(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomSkinColor> args)
    {
        if (!HasComp<VisualBodyComponent>(ent))
            return;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner, null, out var profiles, out _, out _))
            return;

        var changed = false;
        foreach (var category in profiles.Keys.ToList())
        {
            var profile = profiles[category];
            if (profile.SkinColor == args.Effect.Color)
                continue;

            profile.SkinColor = args.Effect.Color;
            profiles[category] = profile;
            changed = true;
        }

        if (changed)
            _visualBody.ApplyProfiles(ent, profiles);
    }

    private void OnYouth(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomYouth> args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var profile))
            return;

        var target = Math.Max(args.Effect.TargetAge, args.Effect.MinAge);
        if (profile.Age <= target)
            return;

        _profile.SetAge((ent.Owner, profile), target);
    }

    private void OnVoiceChange(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomVoiceChange> args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var profile))
            return;

        if (args.Phase == ADTSymptomPhase.End)
        {
            if (!TryComp<VoiceOverrideComponent>(ent, out var current))
                return;

            if (args.State.Text != null && current.NameOverride == args.State.Text)
                RemComp<VoiceOverrideComponent>(ent);

            args.State.Text = null;
            return;
        }

        var voice = EnsureComp<VoiceOverrideComponent>(ent);
        var name = _naming.GetName(profile.Species, profile.Gender);

        voice.NameOverride = name;
        voice.Enabled = true;

        var voices = _proto.EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.RoundStart && v.Sex == profile.Sex)
            .ToList();

        if (voices.Count > 0)
            voice.TTS = _random.Pick(voices).ID;

        Dirty(ent.Owner, voice);

        args.State.Text = name;
    }
}
