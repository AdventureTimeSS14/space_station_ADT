using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.BloodMiner;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodMinerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool SawOpen;

    [DataField]
    public DamageSpecifier ClosedDamage = new();

    [DataField]
    public DamageSpecifier OpenDamage = new();

    [DataField]
    public float ClosedAttackRate = 2.5f;

    [DataField]
    public float OpenAttackRate = 1.5f;

    [DataField]
    public float CleaveArc = 67.5f;

    [DataField]
    public float TransformAfterAttackChance = 0.5f;

    [DataField]
    public TimeSpan TransformCooldownMin = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan TransformCooldownMax = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan NextTransformAt;

    [DataField]
    public SoundSpecifier TransformSound =
        new SoundPathSpecifier("/Audio/ADT/Effects/Magic/Clockwork/fellowship_armory.ogg");

    [DataField]
    public float DashRange = 4f;

    [DataField]
    public TimeSpan DashCooldown = TimeSpan.FromSeconds(1.5);

    [DataField]
    public TimeSpan NextDashAt;

    [DataField]
    public float DashSpeed = 12f;

    [DataField]
    public EntProtoId? DashSmokeProto = "ADTEffectBloodMinerDashSmoke";

    [DataField]
    public SoundSpecifier DashSound = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

    [DataField]
    public EntProtoId GunProto = "ADTWeaponBloodMinerKineticAccelerator";

    [DataField]
    public TimeSpan RangedCooldown = TimeSpan.FromSeconds(1.6);

    [DataField]
    public TimeSpan NextShotAt;

    [DataField]
    public float ButcherHealFraction = 0.5f;

    [DataField]
    public float DamageInterruptCoefficient = 0.01f;

    [DataField]
    public TimeSpan DecisionInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan NextDecisionAt;
}

[Serializable, NetSerializable]
public enum BloodMinerVisuals : byte
{
    SawOpen,
}
