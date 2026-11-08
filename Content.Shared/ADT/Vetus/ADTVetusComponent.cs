using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Vetus;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTVetusComponent : Component
{
    [DataField]
    public List<ADTVetusMode> Modes = new()
    {
        ADTVetusMode.Bluespace,
        ADTVetusMode.Grav,
        ADTVetusMode.Pyro,
        ADTVetusMode.Flux,
        ADTVetusMode.Vortex,
    };

    [ViewVariables]
    public ADTVetusMode? Mode;

    [DataField]
    public float FluxMeleeDamage = 25f;

    [DataField]
    public TimeSpan RangedCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public int AngerRange = 10;

    [DataField]
    public int AngerCap = 6;

    [DataField]
    public int StationAngerCap = 3;

    [ViewVariables]
    public int ExtraPlayerAnger;

    [DataField]
    public float DevourHealFraction = 0.1f;

    [DataField]
    public EntProtoId DevourRemains = "Ash";

    [ViewVariables]
    public bool ShieldEnabled;

    [DataField]
    public TimeSpan ShieldCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId ShieldSparks = "EffectSparks";

    [DataField]
    public SoundSpecifier ShieldBlockSound = new SoundCollectionSpecifier("sparks");

    [DataField]
    public List<TimeSpan> ChargeDelays = new()
    {
        TimeSpan.FromSeconds(0.9),
        TimeSpan.FromSeconds(0.6),
        TimeSpan.FromSeconds(0.3),
    };

    [DataField]
    public List<TimeSpan> BluespaceChargeDelays = new()
    {
        TimeSpan.FromSeconds(2.4),
        TimeSpan.FromSeconds(1.8),
        TimeSpan.FromSeconds(1.2),
        TimeSpan.FromSeconds(0.6),
    };

    [DataField]
    public int ChargePast = 2;

    [DataField]
    public float ChargeSpeed = 12.5f;

    [DataField]
    public TimeSpan ChargeRecovery = TimeSpan.FromSeconds(1.5);

    [DataField]
    public float ChargeDamage = 25f;

    [DataField]
    public float ChargeArmorPenetration = 0.4f;

    [DataField]
    public float ChargeTrampleRadius = 0.8f;

    [DataField]
    public EntProtoId ChargeTelegraph = "ADTVetusChargeTarget";

    [DataField]
    public float ChargeTelegraphLifetime = 1.5f;

    [DataField]
    public SoundSpecifier ChargeImpactSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/meteorimpact.ogg");

    [DataField]
    public float GravThrowDistance = 3f;

    [DataField]
    public float GravThrowSpeed = 10f;

    [DataField]
    public int TeleportRange = 7;

    [DataField]
    public EntProtoId TeleportEffect = "ADTVetusBluespaceKaboom";

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");

    [ViewVariables]
    public Vector2i? LastTrailTile;

    [ViewVariables]
    public Vector2i? LastSmashTile;

    [DataField]
    public EntProtoId PyroTrailFire = "ADTDrakeFire";

    [DataField]
    public float PyroTrailExposeTemperature = 700f;

    [DataField]
    public float PyroTrailExposeVolume = 50f;

    [DataField]
    public EntProtoId LavaWarning = "ADTVetusLavaWarning";

    [DataField]
    public TimeSpan LavaResetTime = TimeSpan.FromSeconds(6);

    [DataField]
    public float VortexTrailIntensity = 2f;

    [DataField]
    public float SmashObjectDamage = 400f;

    [DataField]
    public float HeavyIntensity = 4f;

    [DataField]
    public float DevastationIntensity = 8f;

    [DataField]
    public float HeavyExplosionDamage = 10f;

    [DataField]
    public float DevastationExplosionDamage = 25f;

    [DataField]
    public int AnomalyCount = 3;

    [DataField]
    public int AnomalyRange = 5;

    [DataField]
    public TimeSpan AnomalyLifetime = TimeSpan.FromSeconds(15);

    [DataField]
    public Dictionary<ADTVetusMode, EntProtoId> Anomalies = new()
    {
        { ADTVetusMode.Bluespace, "ADTVetusAnomalyBluespace" },
        { ADTVetusMode.Grav, "ADTVetusAnomalyGrav" },
        { ADTVetusMode.Pyro, "ADTVetusAnomalyPyro" },
        { ADTVetusMode.Flux, "ADTVetusAnomalyFlux" },
        { ADTVetusMode.Vortex, "ADTVetusAnomalyVortex" },
    };

    [DataField]
    public EntProtoId BluespaceSlowdown = "ADTStatusEffectVetusBluespaceSlowdown";

    [DataField]
    public TimeSpan BluespaceSlowdownDuration = TimeSpan.FromSeconds(15);

    [DataField]
    public int SpecialCount = 3;

    [DataField]
    public int SpecialRange = 4;

    [DataField]
    public int RockMinRange = 2;

    [DataField]
    public EntProtoId FloatingRock = "ADTVetusFloatingRock";

    [DataField]
    public EntProtoId RockProjectile = "ADTVetusRock";

    [DataField]
    public TimeSpan RockDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public float ProjectileSpeed = 20f;

    [DataField]
    public int VolcanoMinRange = 1;

    [DataField]
    public EntProtoId FluxProjectile = "ADTVetusTeslaBolt";

    [DataField]
    public int FluxRange = 7;

    [DataField]
    public EntProtoId VortexTarget = "ADTVetusRockfallTarget";

    [DataField]
    public int VortexRange = 9;

    [DataField]
    public float VortexChance = 0.15f;

    [ViewVariables]
    public bool Exploding;

    [DataField]
    public TimeSpan SelfDestructDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan SelfDestructBeepInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public int SelfDestructDevastation;

    [DataField]
    public int SelfDestructHeavy = 7;

    [DataField]
    public int SelfDestructLight = 15;

    [DataField]
    public float SelfDestructFlashRange = 20f;

    [DataField]
    public TimeSpan SelfDestructFlashDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier SelfDestructAlarm = new SoundPathSpecifier("/Audio/Machines/alarm.ogg", AudioParams.Default.WithMaxDistance(20f));

    [DataField]
    public SoundSpecifier SelfDestructBeep = new SoundPathSpecifier("/Audio/Machines/timer.ogg");

    [DataField]
    public Dictionary<ADTVetusMode, EntProtoId> Cores = new()
    {
        { ADTVetusMode.Bluespace, "AnomalyCoreBluespace" },
        { ADTVetusMode.Grav, "AnomalyCoreGravity" },
        { ADTVetusMode.Pyro, "AnomalyCorePyroclastic" },
        { ADTVetusMode.Flux, "AnomalyCoreElectricity" },
        { ADTVetusMode.Vortex, "AnomalyCoreGravity" },
    };

    [DataField]
    public float ChargeChance = 0.3f;

    [DataField]
    public float AnomalyChance = 0.15f;

    [DataField]
    public float SpecialChance = 0.6f;
}

[Serializable, NetSerializable]
public enum ADTVetusMode : byte
{
    Bluespace,
    Grav,
    Pyro,
    Flux,
    Vortex,
}

[Serializable, NetSerializable]
public enum ADTVetusVisuals : byte
{
    Mode,
    Shield,
}
