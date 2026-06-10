using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoDiscombobulatePerformedEvent : EntityEventArgs
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    [DataField]
    public float SpeedMultiplier = 0.7f;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoEyePokePerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoThrowPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoArmbarPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoWheelThrowPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class JudoGoldenBlastPerformedEvent : EntityEventArgs;
