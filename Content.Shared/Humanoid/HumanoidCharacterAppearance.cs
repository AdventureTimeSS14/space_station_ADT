using System.Numerics;
using Content.Shared.ADT.BodyTypes; // ADT-Tweak
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using static Content.Shared.Preferences.HumanoidCharacterProfile;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public List<Color> HairColor { get; set; } = new() { Color.Black }; // ADT-tweak
    
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; set; } = new();

    // ADT-Tweak-Start
    /// <summary>
    /// Alternative torso sprite, null for the default one.
    /// </summary>
    [DataField]
    public ProtoId<BodyTypePrototype>? BodyType { get; set; }
    // ADT-Tweak-End

    public HumanoidCharacterAppearance(
        Color eyeColor,
        List<Color> hairColor, // ADT-tweak
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings,
        ProtoId<BodyTypePrototype>? bodyType = null) // ADT-Tweak
    {
        EyeColor = ClampColor(eyeColor);
        HairColor = hairColor.Select(ClampColor).ToList(); // ADT-tweak
        SkinColor = ClampColor(skinColor);
        Markings = markings;
        BodyType = bodyType; // ADT-Tweak
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.HairColor, other.SkinColor, new(other.Markings), other.BodyType) // ADT-Tweak
    {

    }

    public HumanoidCharacterAppearance WithHairColor(List<Color> newColor) // ADT-tweak
    {
        return new(EyeColor, newColor, SkinColor, Markings, BodyType); // ADT-Tweak
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, HairColor, SkinColor, Markings, BodyType); // ADT-Tweak
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, HairColor, newColor, Markings, BodyType); // ADT-Tweak
    }

    public HumanoidCharacterAppearance WithMarkings(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> newMarkings)
    {
        return new(EyeColor, HairColor, SkinColor, newMarkings, BodyType); // ADT-Tweak
    }

    // ADT-Tweak-Start
    public HumanoidCharacterAppearance WithBodyType(ProtoId<BodyTypePrototype>? bodyType)
    {
        return new(EyeColor, HairColor, SkinColor, Markings, bodyType);
    }
    // ADT-Tweak-End

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            Color.Black,
            new List<Color> { Color.Black }, // ADT-tweak
            skinColor,
            new()
        );
        return EnsureValid(appearance, species, sex);
    }

    private static readonly IReadOnlyList<Color> RealisticEyeColors =
    [
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    ];

    /// <summary>
    /// Picks a random eye color.
    /// </summary>
    public static Color RandomEyes()
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var eyes = random.Pick(RealisticEyeColors);
        return eyes;
    }

    /// <summary>
    /// Picks a random skin color using species.
    /// </summary>
    public static Color RandomSkin(ProtoId<SpeciesPrototype> species)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        var speciesProto = protoMan.Index(species);
        var skinType = speciesProto.SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;

        var skinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };

<<<<<<< ours
        var newHairColor = random.Pick(HairStyles.RealisticHairColors);
        newHairColor = newHairColor
            .WithRed(RandomizeColor(newHairColor.R))
            .WithGreen(RandomizeColor(newHairColor.G))
            .WithBlue(RandomizeColor(newHairColor.B));

        return new HumanoidCharacterAppearance(newEyeColor, new List<Color> { newHairColor }, newSkinColor, new()); // ADT-tweak

        // ADT-Tweak start
        float RandomizeColor(float channel)
        {
            return MathHelper.Clamp01(channel + random.Next(-25, 25) / 100f);
        }
        // ADT-Tweak end
||||||| base
        return new HumanoidCharacterAppearance(newEyeColor, newSkinColor, new());
=======
        return skinColor;
    }

    /// <summary>
    ///     Generates a randomized character appearance.
    /// </summary>
    /// <remarks>
    ///     When <see cref="RandomizeCfg"/> and an existing <see cref="HumanoidCharacterAppearance"> are passed in,
    ///     values will be selectively randomized with the option to maintain existing values.
    /// </remarks>
    /// <param name="charEditorRandomizeConfig">Which values to randomize.</param>
    /// <param name="baseAppearance">Appearance to base the new appearance on. Values that are not randomized will be taken from this appearance.</param>
    /// <param name="species">Species prototype ID.</param>
    /// <param name="sex">Sex.</param>
    public static HumanoidCharacterAppearance Random(SpeciesPrototype species, Sex sex, RandomizeCfg? charEditorRandomizeConfig = null, HumanoidCharacterAppearance? baseAppearance = null)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        var skinType = protoMan.Index(species.SkinColoration);
        var palette = GetRandomClampedPalette(skinType, random);

        // squash Cfg as necessary
        palette = palette with
        {
            SkinColor = (charEditorRandomizeConfig & RandomizeCfg.Skin) != 0 || baseAppearance is null
                ? palette.SkinColor : baseAppearance.SkinColor,
            EyeColor = (charEditorRandomizeConfig & RandomizeCfg.Eyes) != 0 || baseAppearance is null
                ? palette.EyeColor : baseAppearance.EyeColor
        };

        var markings = ((charEditorRandomizeConfig & RandomizeCfg.Markings) != 0 || baseAppearance is null)
            ? RandomizeMarkings(species, sex, palette, protoMan, random)
            : baseAppearance.Markings;
        // TODO if someone really cares they can probably regenerate the old markings with new colors but im too tired to figure that out

        HumanoidCharacterAppearance appearance = new(
            palette.EyeColor,
            palette.SkinColor,
            markings);

        // Safety step. Most systems which called Random() also called this, and not doing so caused issues with markings.
        // In the future it could *maybe* be removed, but it's probably worth the extra CPU cycles to validate this info.
        return EnsureValid(appearance, species, sex);
>>>>>>> theirs
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance, ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var hairColor = appearance.HairColor.Select(ClampColor).ToList(); // ADT-tweak

        var skinColor = appearance.SkinColor;
        var eyeColor = ClampColor(appearance.EyeColor); // not using ClampEyeColorToStrategy so characters can have fun eye colours
        var validatedMarkings = appearance.Markings.ShallowClone();
        // ADT-Tweak-Start
        var bodyType = appearance.BodyType is { } bodyTypeId &&
                       proto.TryIndex(bodyTypeId, out var bodyTypeProto) &&
                       bodyTypeProto.Species.Contains(species)
            ? appearance.BodyType
            : null;
        // ADT-Tweak-End

        if (proto.TryIndex(species, out var speciesProto))
        {
            var coloration = proto.Index(speciesProto.SkinColoration);
            var organs = markingManager.GetOrgans(species);
            skinColor = coloration.Strategy.EnsureVerified(skinColor);

            foreach (var (organ, _) in appearance.Markings)
            {
                if (!organs.ContainsKey(organ))
                    validatedMarkings.Remove(organ);
            }

            foreach (var (organ, organProtoID) in organs)
            {
                if (!markingManager.TryGetMarkingData(organProtoID, out var organData))
                {
                    validatedMarkings.Remove(organ);
                    continue;
                }

                var actualMarkings = appearance.Markings.GetValueOrDefault(organ)?.ShallowClone() ?? new();

                markingManager.EnsureValidColors(actualMarkings);
                markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
                markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
                markingManager.EnsureValidLimits(actualMarkings, organData.Value.Group, organData.Value.Layers, skinColor, eyeColor);

                validatedMarkings[organ] = actualMarkings;
            }
        }

        return new HumanoidCharacterAppearance(
            eyeColor,
            hairColor, // ADT-tweak
            skinColor,
            validatedMarkings,
            bodyType); // ADT-Tweak
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EyeColor.Equals(other.EyeColor) && HairColor.SequenceEqual(other.HairColor) && // ADT-tweak
               SkinColor.Equals(other.SkinColor) &&
               BodyType == other.BodyType && // ADT-Tweak
               MarkingManager.MarkingsAreEqual(Markings, other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, HairColor, SkinColor, Markings, BodyType); // ADT-Tweak
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}