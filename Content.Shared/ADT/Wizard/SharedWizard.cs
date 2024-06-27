using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Wizard;

public sealed partial class WizardTeleportActionEvent : WorldTargetActionEvent
{
    [DataField("blinkSound")]
    public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("blinkVolume")]
    public float BlinkVolume = 5f;
}

public sealed partial class WizardProjectileActionEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = "BulletKinetic";

    /// <summary>
    /// Gets the targeted spawn positions; may lead to multiple entities being spawned.
    /// </summary>
    [DataField("posData")]
    public WizardSpawnData Pos = new TargetCasterPos();

    [DataField]
    public SoundSpecifier ShootSound = new SoundPathSpecifier("/Audio/Weapons/Xeno/alien_spitacid.ogg");

    [DataField("shootVolume")]
    public float ShootVolume = 5f;
}

public sealed partial class WizardHealActionEvent : InstantActionEvent
{
    [DataField("healSound")]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg");

    /// <summary>
    /// Volume control for the spell.
    /// </summary>
    [DataField("healVolume")]
    public float HealVolume = 1f;

    [DataField]
    public float RegenerateBurnHealAmount = -50f;

    [DataField]
    public float RegenerateBruteHealAmount = -75f;

    [DataField]
    public float RegenerateBloodVolumeHealAmount = 100f;

    [DataField]
    public float RegenerateBleedReduceAmount = -100f;
}
