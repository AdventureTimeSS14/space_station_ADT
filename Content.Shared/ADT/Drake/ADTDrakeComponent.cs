using Content.Shared.Damage.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTDrakeComponent : Component
{
    [ViewVariables]
    public float AngerModifier;

    [DataField]
    public float AngerDamageDivisor = 50f;

    [DataField]
    public float MaxAnger = 20f;

    [DataField]
    public TimeSpan RangedCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan DecisionInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRangedAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan RecoveryUntil;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDecisionAt;

    [DataField]
    public float DevourHealFraction = 0.5f;

    [DataField]
    public ProtoId<DamageGroupPrototype> HealGroup = "Brute";

    [DataField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/Effects/demon_dies.ogg");

    [DataField]
    public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/Magic/fireball.ogg");

    [DataField]
    public int FireLineRange = 15;

    [DataField]
    public List<float> FireConeAngles = new() { -40f, 0f, 40f };

    [DataField]
    public float FireConeMeteorChance = 0.5f;

    [DataField]
    public float FireLineDamage = 20f;

    [DataField]
    public float FireLineMechDamage = 45f;

    [DataField]
    public TimeSpan FireLineStepDelay = TimeSpan.FromSeconds(0.15);

    [DataField]
    public EntProtoId FireLineProto = "ADTDrakeFireLine";

    [DataField]
    public EntProtoId FireProto = "ADTDrakeFire";

    [DataField]
    public int FireRainRadius = 9;

    [DataField]
    public float FireRainChance = 0.11f;

    [DataField]
    public EntProtoId FireRainTargetProto = "ADTDrakeMeteorTarget";

    [DataField]
    public float ShootFireChanceBase = 0.1f;

    [DataField]
    public int MassFireSpiralCount = 12;

    [DataField]
    public int MassFireRange = 15;

    [DataField]
    public int MassFireTimes = 3;

    [DataField]
    public TimeSpan MassFireWaveDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan MassFireWaveRecovery = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan MassFireEndRecovery = TimeSpan.FromSeconds(3);

    [DataField]
    public float LavaSwoopChanceBase = 0.15f;

    [DataField]
    public TimeSpan LavaSwoopCooldown = TimeSpan.FromSeconds(100);

    [DataField]
    public TimeSpan LavaSwoopArenaCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan LavaSwoopConeDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan LavaSwoopEndRecovery = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan SwoopRiseTime = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan SwoopAscendTime = TimeSpan.FromSeconds(0.7);

    [DataField]
    public TimeSpan SwoopStepDelay = TimeSpan.FromSeconds(0.05);

    [DataField]
    public TimeSpan SwoopDescentTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan SwoopLandedTime = TimeSpan.FromSeconds(0.1);

    [DataField]
    public float SwoopRiseAlpha = 0.8f;

    [DataField]
    public float SwoopRiseScale = 0.9f;

    [DataField]
    public float SwoopAirAlpha = 0.39f;

    [DataField]
    public float SwoopAirScale = 0.7f;

    [DataField]
    public int SwoopDirectionChangeRange = 5;

    [DataField]
    public float SwoopDamage = 75f;

    [DataField]
    public float SwoopMechDamage = 75f;

    [DataField]
    public float SwoopThrowRange = 3f;

    [DataField]
    public float SwoopShakeRange = 7f;

    [DataField]
    public float SwoopShakeStrength = 1f;

    [DataField]
    public SoundSpecifier SwoopLandSound = new SoundPathSpecifier("/Audio/ADT/Drake/meteorimpact.ogg");

    [DataField]
    public EntProtoId SwoopRiseLeftProto = "ADTDrakeFlightRiseLeft";

    [DataField]
    public EntProtoId SwoopRiseRightProto = "ADTDrakeFlightRiseRight";

    [DataField]
    public EntProtoId SwoopLandLeftProto = "ADTDrakeFlightLandLeft";

    [DataField]
    public EntProtoId SwoopLandRightProto = "ADTDrakeFlightLandRight";

    [DataField]
    public EntProtoId SwoopLandingMarkerProto = "ADTDrakeLandingMarker";

    [DataField]
    public int LavaPoolsAmount = 30;

    [DataField]
    public TimeSpan LavaPoolDelay = TimeSpan.FromSeconds(0.8);

    [DataField]
    public int LavaPoolRadius = 1;

    [DataField]
    public TimeSpan LavaPoolResetTime = TimeSpan.FromSeconds(6);

    [DataField]
    public EntProtoId LavaWarningProto = "ADTDrakeLavaWarning";

    [DataField]
    public EntProtoId LavaSafeProto = "ADTDrakeLavaSafe";

    [DataField]
    public EntProtoId ArenaWallProto = "ADTDrakeFireWall";

    [DataField]
    public int ArenaWallRadius = 3;

    [DataField]
    public int ArenaRadius = 2;

    [DataField]
    public int ArenaSafeRing = 2;

    [DataField]
    public int ArenaMechSafeRing = 2;

    [DataField]
    public int ArenaRounds = 3;

    [DataField]
    public TimeSpan ArenaStartDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan ArenaRoundDelay = TimeSpan.FromSeconds(2.4);

    [DataField]
    public TimeSpan ArenaLavaResetTime = TimeSpan.FromSeconds(1);

    [DataField]
    public string ArenaFloorTile = "FloorBasalt";

    [DataField]
    public float EscapeHeal = 250f;

    [DataField]
    public TimeSpan EscapeRecovery = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan EscapeMassFireDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float EscapeSpeedMultiplier = 2f;

    [DataField]
    public float EscapeLightRadius = 10f;

    [ViewVariables]
    public bool EscapeEnraged;

    [ViewVariables]
    public float? BaseLightRadius;
}

[Serializable, NetSerializable]
public enum ADTDrakeVisuals : byte
{
    EscapeEnraged,
}
