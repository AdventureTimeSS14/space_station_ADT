using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Vetus;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTVetusLegComponent : Component
{
    [DataField]
    public float MaxHealth = 300f;

    [DataField]
    public float RegenAmount = 2f;

    [DataField]
    public TimeSpan RegenInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public float BaseTransferRate = 0.75f;

    [DataField]
    public float LowTransferWarning = 0.25f;

    [DataField]
    public float TurretBreakHealth = 200f;

    [DataField]
    public float TurretRepairHealth = 250f;

    [ViewVariables]
    public bool TurretEnabled = true;

    [DataField]
    public int VisionRange = 9;

    [DataField]
    public TimeSpan ThinkInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ThinkJitterMin = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ThinkJitterMax = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public EntityUid? Target;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextThink;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRegen;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextShot;

    [DataField]
    public EntProtoId TurretProjectile = "ADTVetusLegBullet";

    [DataField]
    public float TurretProjectileSpeed = 20f;

    [DataField]
    public TimeSpan TurretMinCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan TurretMaxCooldown = TimeSpan.FromSeconds(6);

    [DataField]
    public float TurretSkipChance = 0.33f;

    [DataField]
    public int TurretCoreSafeRange = 3;

    [DataField]
    public SoundSpecifier TurretSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/rifle.ogg");

    [DataField]
    public float MeleeDamage = 15f;

    [DataField]
    public float ArmorPenetration = 0.2f;

    [DataField]
    public float ChargeBumpDamage = 12.5f;

    [DataField]
    public float ChargeBumpRadius = 0.6f;

    [DataField]
    public float ChargeBumpCameraKick = 0.6f;

    [DataField]
    public SoundSpecifier ChargeBumpSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/meteorimpact.ogg");

    [DataField]
    public SoundSpecifier StepSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/meteorimpact.ogg", AudioParams.Default.WithVolume(-6f).WithVariation(0.1f));

    [DataField]
    public int MeleePullInward = 1;

    [DataField]
    public float HeavyIntensity = 4f;

    [DataField]
    public float DevastationIntensity = 8f;

    [DataField]
    public float HeavyExplosionDamage = 10f;

    [DataField]
    public float DevastationExplosionDamage = 25f;
}
