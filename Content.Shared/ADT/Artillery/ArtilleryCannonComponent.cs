using Content.Shared.Explosion;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Artillery;

[NetSerializable, Serializable]
public enum ArtilleryVisuals : byte
{
    Charging,
    Reloading,
}

[RegisterComponent]
public sealed partial class ArtilleryCannonComponent : Component
{
    [DataField("cooldownMinutes")]
    public float CooldownMinutes = 5f;

    [DataField("explosionType")]
    public ProtoId<ExplosionPrototype> ExplosionType = "BSA";

    [DataField("totalIntensity")]
    public float TotalIntensity = 20000f;

    [DataField("intensitySlope")]
    public float IntensitySlope = 5f;

    [DataField("maxIntensity")]
    public float MaxIntensity = 50f;

    [DataField("effectRange")]
    public float EffectRange = 30f;

    [DataField("fireSound")]
    public string FireSound = "/Audio/ADT/Weapon/BSA/bsa_fire.ogg";

    [DataField("fireDirection")]
    public Direction FireDirection = Direction.East;

    [DataField("laserProto")]
    public EntProtoId LaserProto = "ArtilleryBluespaceHitscan";

    [DataField("bluespaceFlashProto")]
    public EntProtoId BluespaceFlashProto = "EffectFlashBluespace";

    [DataField("muzzleForwardOffset")]
    public float MuzzleForwardOffset = 4.95f;

    [DataField("muzzleVerticalOffset")]
    public float MuzzleVerticalOffset = 0.7f;

    [DataField("powerUseActive")]
    public int PowerUseActive = 600;

    [DataField("multiMap")]
    public bool MultiMap = false;

    [ViewVariables]
    public TimeSpan NextFireTime;

    [ViewVariables]
    public bool IsEnabled = false;

    [ViewVariables]
    public bool IsCharging = false;
}
