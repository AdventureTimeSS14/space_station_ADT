using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause] // ADT-Tweak
public sealed partial class DefibrillatorComponent : Component
{
    // ADT-Tweak

    /// <summary>
    /// Whether or not it's turned on and able to be used.
    /// </summary>
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    /// <summary>
    /// The time at which the zap cooldown will be completed
    /// </summary>
    [DataField("nextZapTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan? NextZapTime;

    /// <summary>
    /// The minimum time between zaps
    /// </summary>
    [DataField("zapDelay"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(3);

    //End ADT-Tweak

    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField("zapHeal", required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier ZapHeal = default!;

    // ADT-Tweak
    [DataField]
    public bool AllowDoAfterMovement = true;

    [DataField]
    public bool LowChargeState = false;
    // End ADT-Tweak

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField("zapDamage"), ViewVariables(VVAccess.ReadWrite)]
    public int ZapDamage = 5;

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField("writheDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(5);   // ADT-Tweak

    /// <summary>
    /// How long the doafter for zapping someone takes
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField("doAfterDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(5);  // ADT-Tweak

    /// <summary>
    /// The sound when someone is zapped.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("zapSound")]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    // ADT-Tweak

    /// <summary>
    /// The sound when the defib is powered on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("powerOnSound")]
    public SoundSpecifier? PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_safety_on.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("powerOffSound")]
    public SoundSpecifier? PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_safety_off.ogg");

    // End ADT-Tweak

    [ViewVariables(VVAccess.ReadWrite), DataField("chargeSound")]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("failureSound")]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("successSound")]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("readySound")]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");

    // ADT-Tweak
    [ViewVariables(VVAccess.ReadWrite), DataField("lowChargeSound")]
    public SoundSpecifier? LowChargeSound = new SoundPathSpecifier("/Audio/ADT/Items/Defib/defib_low_charge.ogg");
    // End ADT-Tweak
}

// ADT-Tweak
[Serializable, NetSerializable]
public enum DefibrillatorVisuals : byte
{
    Ready
}
// End ADT-Tweak

[Serializable, NetSerializable]
public sealed partial class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent
{

}
