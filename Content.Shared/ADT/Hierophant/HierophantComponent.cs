using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Hierophant;

[Serializable, NetSerializable]
public enum HierophantVisuals : byte
{
    Attacking,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class HierophantComponent : Component
{
    [DataField]
    public int BurstRange = 3;

    [DataField]
    public int BeamRange = 5;

    [DataField]
    public float ChaserSpeed = 0.3f;

    [DataField]
    public float AngerModifier;

    [DataField]
    public TimeSpan RangedCooldownTime = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan MajorAttackCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan ChaserCooldownTime = TimeSpan.FromSeconds(10.1);

    [DataField]
    public TimeSpan ArenaCooldownTime = TimeSpan.FromSeconds(20);

    [DataField, AutoPausedField]
    public TimeSpan NextRangedAt;

    [DataField, AutoPausedField]
    public TimeSpan NextChaserAt;

    [DataField, AutoPausedField]
    public TimeSpan NextArenaAt;

    [DataField]
    public bool Blinking;

    [DataField]
    public EntityUid? SpawnedBeacon;

    [DataField]
    public TimeSpan TimeoutTime = TimeSpan.FromSeconds(30);

    [DataField, AutoPausedField]
    public TimeSpan? TimeoutAt;

    [DataField]
    public bool DidReset = true;

    [DataField]
    public bool Enraged;

    [DataField]
    public EntityUid? LastTarget;

    [DataField]
    public int ArenaRadius = 11;

    [DataField]
    public float TileMovementRadius = 26f;

    [DataField]
    public float BlinkBlastDamage = 30f;

    [DataField]
    public DamageSpecifier SelfRepair = new()
    {
        DamageDict =
        {
            { "Heat", 1 },
            { "Blunt", 1 },
        },
    };

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ActionADTHierophantBlink",
        "ActionADTHierophantChaserSwarm",
        "ActionADTHierophantCrossBlasts",
        "ActionADTHierophantBlinkSpam",
    };

    [DataField]
    public EntProtoId BlastProto = "ADTHierophantBlast";

    [DataField]
    public EntProtoId ChaserProto = "ADTHierophantChaser";

    [DataField]
    public EntProtoId SquaresProto = "ADTHierophantSquares";

    [DataField]
    public EntProtoId WallProto = "ADTHierophantTempWall";

    [DataField]
    public EntProtoId BeaconProto = "ADTHierophantBeacon";

    [DataField]
    public EntProtoId TelegraphProto = "ADTHierophantTelegraph";

    [DataField]
    public EntProtoId TelegraphCardinalProto = "ADTHierophantTelegraphCardinal";

    [DataField]
    public EntProtoId TelegraphDiagonalProto = "ADTHierophantTelegraphDiagonal";

    [DataField]
    public EntProtoId TelegraphTeleportProto = "ADTHierophantTelegraphTeleport";

    [DataField]
    public SoundSpecifier BlinkOutSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/wand_teleport.ogg");

    [DataField]
    public SoundSpecifier BlinkInSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/airlock_open.ogg");

    [DataField]
    public SoundSpecifier TelegraphSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/bin_close.ogg");

    [DataField]
    public SoundSpecifier MoveSound = new SoundPathSpecifier("/Audio/ADT/Hierophant/mechmove04.ogg");

    [DataField]
    public Color AttackColor = Color.FromHex("#660099");

    [ViewVariables]
    public TimeSpan LastMoveSound;

    [DataField]
    public TimeSpan MoveSoundCooldown = TimeSpan.FromMilliseconds(200);

    [ViewVariables]
    public MapCoordinates? LastTrailPosition;

    [DataField]
    public float TrailStepDistance = 0.5f;
}
