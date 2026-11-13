using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Components;

[RegisterComponent]
public sealed partial class TendrilComponent : Component
{
    [DataField]
    public int MaxSpawns = 3;

    [DataField]
    public TimeSpan SpawnDelay = TimeSpan.FromSeconds(30);

    [DataField(required: true)]
    public Dictionary<EntProtoId, float> Spawns = new();

    [DataField]
    public LocId SpawnPopup = "adt-tendril-spawn-popup";

    [DataField]
    public int ClearRadius = 1;

    [DataField]
    public EntProtoId Collapse = "ADTTendrilCollapse";

    [DataField]
    public EntProtoId Chest = "ADTCrateNecropolisRandom";

    [ViewVariables]
    public HashSet<EntityUid> Mobs = new();

    [ViewVariables]
    public TimeSpan NextSpawn;

    [DataField]
    public TimeSpan AggroMemory = TimeSpan.FromSeconds(45);

    [DataField]
    public float AggroRadiusPadding = 5f;

    [DataField]
    public float MaxAggroRadius = 100f;

    [ViewVariables]
    public EntityUid? Aggressor;

    [ViewVariables]
    public TimeSpan AggroEndTime;
}

[RegisterComponent]
public sealed partial class TendrilCollapseComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public int Radius = 2;

    [DataField]
    public EntProtoId Chasm = "FloorChasmEntity";

    [DataField]
    public float ShakeRange = 7f;

    [DataField]
    public float ShakeDuration = 15f;

    [DataField]
    public float ShakeStrength = 1f;

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/ADT/Lavaland/Tendril/tendril_destroyed.ogg", AudioParams.Default.WithVolume(10f).WithMaxDistance(50f));

    [DataField]
    public SoundSpecifier CollapseSound = new SoundPathSpecifier("/Audio/Effects/explosionfar.ogg", AudioParams.Default.WithVolume(10f).WithMaxDistance(50f));

    [DataField]
    public LocId StartPopup = "adt-tendril-collapse-start";

    [DataField]
    public LocId TearPopup = "adt-tendril-collapse-tear";

    [DataField]
    public LocId CollapsePopup = "adt-tendril-collapse-end";

    [ViewVariables]
    public TimeSpan CollapseAt;
}
