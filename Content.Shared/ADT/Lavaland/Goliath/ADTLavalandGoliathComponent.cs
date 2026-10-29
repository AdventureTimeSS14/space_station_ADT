using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Lavaland.Goliath;

[RegisterComponent]
public sealed partial class ADTLavalandGoliathComponent : Component
{
    [DataField]
    public TimeSpan ThinkInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan RangedCooldown = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan DamageCooldownReduction = TimeSpan.FromSeconds(1);

    [DataField]
    public float PreAttackFraction = 0.25f;

    [DataField]
    public float MeleeRange = 1.5f;

    [DataField]
    public float RangedRange = 7.5f;

    [DataField]
    public float ChargeChance = 0.5f;

    [DataField]
    public TimeSpan ChargeDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float ChargePast = 2f;

    [DataField]
    public float ChargeSpeed = 14.3f;

    [DataField]
    public TimeSpan ChargeTimeout = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan PostChargeDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public int MaxMineralAmount = 7;

    [DataField]
    public EntProtoId TentaclePrototype = "ADTEffectLavalandGoliathTentacle";

    [DataField]
    public EntProtoId ChargeDecoyPrototype = "ADTEffectLavalandGoliathChargeDecoy";

    [DataField]
    public float ReflectChance = 0.3f;

    [DataField]
    public Angle ReflectSpread = Angle.FromDegrees(45);

    [DataField]
    public float ReflectedLifetime = 0.14f;

    [DataField]
    public float LaserResistThreshold = 30f;

    [DataField]
    public float LaserResistMultiplier = 1f / 3f;

    [DataField]
    public float ThrowImmunityThreshold = 20f;

    [DataField]
    public bool AncientTentacles;

    [DataField]
    public int AncientTentacleRadius = 4;

    [DataField]
    public float AncientTentacleChance = 0.1f;

    [DataField]
    public string IdleState = "goliath";

    [DataField]
    public string AggroState = "goliath";

    [DataField]
    public string PreAttackState = "goliath2";

    [DataField]
    public string DeadState = "goliath_dead";

    [DataField]
    public Color WindUpColor = Color.FromHex("#FF5050");

    [DataField]
    public float RetaliatePadding = 2f;

    [DataField]
    public float MaxRetaliateRadius = 20f;

    [ViewVariables]
    public float? BaseAggroRadius;

    [ViewVariables]
    public TimeSpan NextThink;

    [ViewVariables]
    public TimeSpan NextRanged;

    [ViewVariables]
    public EntityUid? PendingHitSource;

    [ViewVariables]
    public GameTick PendingHitTick;

    [ViewVariables]
    public bool PendingHitIsThrow;

    [ViewVariables]
    public float PendingHitTotal;

    [ViewVariables]
    public bool PendingHitHasBrute;
}

[RegisterComponent]
public sealed partial class ADTLavalandGoliathChargeComponent : Component
{
    [ViewVariables]
    public MapCoordinates Destination;

    [ViewVariables]
    public ADTGoliathChargePhase Phase = ADTGoliathChargePhase.WindUp;

    [ViewVariables]
    public TimeSpan PhaseEndsAt;

    [ViewVariables]
    public bool WasAwake;
}

[Serializable, NetSerializable]
public enum ADTGoliathChargePhase : byte
{
    WindUp,
    Running,
    Recover,
}

[RegisterComponent]
public sealed partial class ADTLavalandGoliathTentacleComponent : Component
{
    [DataField]
    public TimeSpan SpawnTime = TimeSpan.FromSeconds(0.7);

    [DataField]
    public TimeSpan WiggleTime = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan LatchTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan RetractTime = TimeSpan.FromSeconds(0.7);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);

    [DataField]
    public int MinDamage = 10;

    [DataField]
    public int MaxDamage = 15;

    [DataField]
    public int MinStunnedDamage = 20;

    [DataField]
    public int MaxStunnedDamage = 30;

    [ViewVariables]
    public EntityUid? Spawner;

    [ViewVariables]
    public ADTGoliathTentacleStage Stage = ADTGoliathTentacleStage.Spawning;

    [ViewVariables]
    public TimeSpan NextStage;
}

[Serializable, NetSerializable]
public enum ADTGoliathTentacleStage : byte
{
    Spawning,
    Wiggling,
    Latched,
    Retracting,
}

[Serializable, NetSerializable]
public enum ADTGoliathTentacleVisuals : byte
{
    Stage,
}

[Serializable, NetSerializable]
public enum ADTLavalandGoliathVisualLayers : byte
{
    Glow,
}

[Serializable, NetSerializable]
public enum ADTLavalandGoliathVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum ADTLavalandGoliathState : byte
{
    Idle,
    Aggro,
    PreAttack,
    WindUp,
}
