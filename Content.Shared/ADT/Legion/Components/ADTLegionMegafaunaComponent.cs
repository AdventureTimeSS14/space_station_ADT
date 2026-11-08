using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTLegionMegafaunaComponent : Component
{
    [DataField]
    public float ChargeChance = 0.3f;

    [DataField]
    public float LaserChance = 0.6f;

    [DataField]
    public float BigLegionChance = 0.4f;

    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan SpinDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan BigLegionCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan TendrilLegionCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LaserBaseTelegraph = TimeSpan.FromSeconds(0.25);

    [DataField]
    public TimeSpan LaserHealthTelegraph = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan LaserRecovery = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LaserBeamDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan LaserWallMeltDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public int LaserRange = 50;

    [DataField]
    public float LaserDamageMax = 70f;

    [DataField]
    public float LaserDamageHealthScale = 20f;

    [DataField]
    public string LaserDamageType = "Heat";

    [DataField]
    public TimeSpan LaserBeamTick = TimeSpan.FromSeconds(0.1);

    [DataField]
    public float SkullChance = 0.33f;

    [DataField]
    public EntProtoId SkullProto = "ADTMobLegionSkull";

    [DataField]
    public EntProtoId BigLegionProto = "ADTMobLegionBig";

    [DataField]
    public EntProtoId TendrilLegionProto = "ADTMobLegion";

    [DataField]
    public EntProtoId BeamTelegraphProto = "ADTLegionBeamTelegraph";

    [DataField]
    public EntProtoId BeamProto = "ADTLegionBeam";

    [DataField]
    public EntProtoId BeamLitProto = "ADTLegionBeamLit";

    [DataField]
    public int BeamLightSpacing = 4;

    [DataField]
    public EntProtoId InfestProto = "ADTMobLegion";

    [DataField]
    public LocId InfestMessage = "adt-legion-infest";

    [DataField]
    public EntProtoId RemainsProto = "Ash";

    [DataField]
    public SoundSpecifier LaserTelegraphSound = new SoundPathSpecifier("/Audio/ADT/Legion/basscannon.ogg");

    [DataField]
    public SoundSpecifier LaserFireSound = new SoundPathSpecifier("/Audio/ADT/Legion/marauder.ogg");

    [DataField]
    public SoundSpecifier LaserHitSound = new SoundPathSpecifier("/Audio/ADT/Legion/sear.ogg");

    [DataField]
    public float ScaleBase = 0.5f;

    [DataField]
    public float? InitialScale;

    [DataField]
    public string HitboxFixture = "hitbox";

    [DataField]
    public float HitboxRadius = 1.25f;

    [DataField]
    public Vector2 HitboxOffset = new(0f, 0.5f);

    [DataField]
    public string ChargingKey = "LegionCharging";

    [DataField]
    public string FiringLaserKey = "LegionFiringLaser";

    [ViewVariables]
    public bool Charging;

    [ViewVariables]
    public bool FiringLaser;

    [ViewVariables]
    public float CurrentScale = -1f;

    [ViewVariables]
    public bool Shrunk;

    [ViewVariables]
    public TimeSpan ClientSpinUntil;
}

[Serializable, NetSerializable]
public enum ADTLegionMegafaunaVisuals : byte
{
    SpinUntil,
}
