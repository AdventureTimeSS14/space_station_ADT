using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseCapoeiraEvent : EntityEventArgs
{
    [DataField]
    public virtual float VelocityPowerMultiplier { get; set; } = 0.6f;

    [DataField]
    public virtual float MinPower { get; set; } = 1f;

    [DataField]
    public virtual float MaxPower { get; set; } = 4f;

    [DataField]
    public virtual float MinVelocity { get; set; }

    [DataField]
    public virtual float AttackSpeedMultiplier { get; set; } = 1f;

    [DataField]
    public virtual TimeSpan AttackSpeedMultiplierTime { get; set; } = TimeSpan.Zero;

    [DataField]
    public virtual SoundSpecifier? Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg");

    [DataField]
    public float StaminaToHeal = -20f;
}

public sealed partial class PushKickPerformedEvent : BaseCapoeiraEvent
{
    [DataField]
    public float ThrowRange = 1f;
}

public sealed partial class SweepKickPerformedEvent : BaseCapoeiraEvent;

public sealed partial class CircleKickPerformedEvent : BaseCapoeiraEvent
{
    [DataField]
    public TimeSpan SlowDownTime = TimeSpan.FromSeconds(2);
}

public sealed partial class SpinKickPerformedEvent : BaseCapoeiraEvent;

public sealed partial class KickUpPerformedEvent : BaseCapoeiraEvent;
