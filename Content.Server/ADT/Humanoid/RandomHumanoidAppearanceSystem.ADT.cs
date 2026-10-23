using System.Linq;
using Content.Server.Humanoid.Components;
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

    private void ApplyADTRestrictions(RandomHumanoidAppearanceComponent component, HumanoidCharacterProfile profile)
    {
        if (component.Voices.Count > 0)
            profile.TTSVoice = _random.Pick(component.Voices);

        Color? hairColor = component.HairColors.Count > 0 ? _random.Pick(component.HairColors) : null;
        var hairStyle = component.HairStyles.Count > 0 ? _random.Pick(component.HairStyles) : (ProtoId<MarkingPrototype>?) null;

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
    }
}
