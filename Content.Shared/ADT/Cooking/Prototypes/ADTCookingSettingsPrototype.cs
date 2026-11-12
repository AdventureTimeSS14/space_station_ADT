using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Prototypes;

[Prototype("adtCookingSettings")]
public sealed partial class ADTCookingSettingsPrototype : IPrototype
{
    public const string DefaultId = "Default";

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public float UpdateInterval = 1f;

    [DataField]
    public Dictionary<ADTStoveHeat, float> HeatMultipliers = new();

    [DataField]
    public ADTStoveHeat OvenHeat = ADTStoveHeat.Medium;

    [DataField]
    public List<ProtoId<ADTDishQualityPrototype>> Qualities = new();

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> NutrientReagents = new();

    [DataField]
    public List<ADTFlavorGroup> FlavorGroups = new();

    [DataField]
    public List<int> QualityThresholds = new();

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> WaterReagents = new();

    [DataField]
    public float DefaultOvercookTime = 20f;

    [DataField]
    public float DefaultBurnTime = 40f;

    [DataField]
    public float NoMediumBurnMultiplier = 2f;

    [DataField]
    public int NoMediumMaxLevel = 1;

    [DataField]
    public Dictionary<ADTCookwareType, EntProtoId> FreeformResults = new();

    [DataField]
    public Dictionary<ADTCookwareType, float> FreeformTimes = new();

    [DataField]
    public float FreeformNutritionMultiplier = 1f;

    [DataField]
    public int FreeformMaxLevel = 2;

    [DataField]
    public int FreeformMaxPortions = 4;

    [DataField]
    public FixedPoint2 FreeformWaterPerPortion = 15;

    [DataField]
    public FixedPoint2 FreeformMinWater = 10;

    [DataField]
    public EntProtoId BurntResult = "FoodBadRecipe";

    [DataField]
    public HashSet<EntProtoId> MeltIntoMedium = new();

    [DataField]
    public HashSet<EntProtoId> BaseIngredients = new();

    [DataField]
    public FixedPoint2 BatterMinVolume = 2;

    [DataField]
    public HashSet<EntProtoId> Bowls = new();

    [DataField]
    public ProtoId<TagPrototype> StirTag = "ADTLadle";

    [DataField]
    public ProtoId<TagPrototype> FlipTag = "ADTSpatula";

    [DataField]
    public float FlipWindowStart = 0.4f;

    [DataField]
    public float FlipWindowEnd = 0.7f;

    [DataField]
    public float StirInterval = 15f;

    [DataField]
    public float StirWindow = 8f;

    [DataField]
    public FixedPoint2 FlareMinWater = 1;

    [DataField]
    public ADTStoveHeat FlareMinHeat = ADTStoveHeat.Medium;

    [DataField]
    public TimeSpan FireDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public float FireRange = 1f;

    [DataField]
    public float FireStacks = 1f;

    [DataField]
    public string FreeformFlavor = "terrible";

    [DataField]
    public float ReheatTime = 5f;

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> DressingReagents = new();

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> SpiceReagents = new();

    [DataField]
    public float GlazeStrength = 0.42f;

    [DataField]
    public float GlazeMaxStrength = 0.65f;

    [DataField]
    public float SpiceDensity = 0.06f;

    [DataField]
    public float SpiceMaxDensity = 0.14f;

    [DataField]
    public FixedPoint2 SaucePourAmount = 3;

    [DataField]
    public SoundSpecifier? FlareSound;

    [DataField]
    public SoundSpecifier? BurnSound;

    [DataField]
    public SoundSpecifier? StirSound;

    [DataField]
    public SoundSpecifier? FlipSound;

    [DataField]
    public SoundSpecifier? FryDropSound;

    [DataField]
    public SoundSpecifier? WaterDropSound;

    [DataField]
    public SoundSpecifier? OvenOpenSound;

    [DataField]
    public SoundSpecifier? OvenCloseSound;

    [DataField]
    public SoundSpecifier? FryLoopSound;

    [DataField]
    public SoundSpecifier? BoilLoopSound;

    [DataField]
    public SoundSpecifier? OvenLoopSound;

    [DataField]
    public SoundSpecifier? FireLoopSound;

    [DataField]
    public SoundSpecifier? OvenTimerSound;
}

[DataDefinition]
public sealed partial class ADTFlavorGroup
{
    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = new();

    [DataField]
    public FixedPoint2 Amount = 1;

    [DataField]
    public HashSet<EntProtoId> Ingredients = new();

    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();
}
