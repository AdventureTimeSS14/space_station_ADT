using Content.Shared.Dataset;
using Content.Shared.Polymorph;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTShadowlingComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<FactionIconPrototype> StatusIcon = "ADTShadowlingFaction";

    [DataField, AutoNetworkedField]
    public bool Hatched;

    [DataField, AutoNetworkedField]
    public bool AscensionUnlocked;

    [DataField, AutoNetworkedField]
    public int KnownThralls;

    [DataField]
    public Color EyeColor = Color.Red;

    [DataField]
    public HashSet<EntProtoId> UnlockedAbilities = new();

    [DataField]
    public List<EntProtoId> DisguisedActions = new()
    {
        "ADTActionShadowlingHatch",
    };

    [DataField]
    public List<EntProtoId> HatchedActions = new()
    {
        "ADTActionShadowlingEnthrall",
        "ADTActionShadowlingGlare",
        "ADTActionShadowlingVeil",
        "ADTActionShadowlingShadowWalk",
        "ADTActionShadowlingIcyVeins",
        "ADTActionShadowlingCollectiveMind",
        "ADTActionShadowlingNullCharge",
        "ADTActionShadowlingDestroyEngines",
    };

    [DataField]
    public Dictionary<EntProtoId, int> ProgressionAbilities = new()
    {
        ["ADTActionShadowlingBlindnessSmoke"] = 1,
        ["ADTActionShadowlingSonicScreech"] = 3,
        ["ADTActionShadowlingBlackRecuperation"] = 5,
    };

    [DataField]
    public EntProtoId AscendAction = "ADTActionShadowlingAscend";

    [DataField, AutoNetworkedField]
    public int RequiredThralls = 8;

    [DataField]
    public List<EntityUid> GrantedActions = new();

    [DataField]
    public ProtoId<PolymorphPrototype> HatchPolymorph = "ADTShadowlingHatched";

    [DataField]
    public EntProtoId ChrysalisProto = "ADTShadowlingChrysalis";

    [DataField]
    public ProtoId<DatasetPrototype> NameDataset = "ADTShadowlingNames";

    [DataField]
    public List<TimeSpan> HatchStages = new()
    {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(9),
        TimeSpan.FromSeconds(8),
        TimeSpan.FromSeconds(3),
    };

    [DataField]
    public SoundSpecifier? HatchSound = new SoundPathSpecifier("/Audio/ADT/Shadowling/ghost.ogg");

    [DataField]
    public SoundSpecifier? ChrysalisBreakSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    [DataField]
    public TimeSpan EnthrallStageTime = TimeSpan.FromSeconds(3);

    [DataField]
    public int EnthrallStages = 3;

    [DataField]
    public TimeSpan EnthrallKnockdown = TimeSpan.FromSeconds(24);

    [DataField]
    public SoundSpecifier? EnthrallSound = new SoundPathSpecifier("/Audio/ADT/Shadowling/verb-shadowling.ogg");

    [DataField]
    public TimeSpan RoundDurationCooldown = TimeSpan.FromMinutes(25);
}
