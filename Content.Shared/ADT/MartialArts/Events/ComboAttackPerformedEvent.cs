using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

public sealed class ComboAttackPerformedEvent(
    EntityUid performer,
    EntityUid target,
    EntityUid weapon,
    ComboAttackType type)
    : CancellableEntityEventArgs
{
    public EntityUid Performer { get; } = performer;
    public EntityUid Target { get; } = target;
    public EntityUid Weapon { get; } = weapon;
    public ComboAttackType Type { get; } = type;
}

[Serializable, NetSerializable]
public enum ComboAttackType : byte
{
    Harm,
    HarmLight,
    Disarm,
    Grab,
    Hug,
}

[Serializable, NetSerializable]
public sealed class ComboBeingPerformedEvent(ProtoId<ComboPrototype> protoId) : EntityEventArgs
{
    public ProtoId<ComboPrototype> ProtoId = protoId;
}

public sealed class SaveLastAttacksEvent : EntityEventArgs;

public sealed class ResetLastAttacksEvent(bool dirty = true) : EntityEventArgs
{
    public bool Dirty = dirty;
}

public sealed class LoadLastAttacksEvent(bool dirty = true) : EntityEventArgs
{
    public bool Dirty = dirty;
}

[ByRefEvent]
public readonly record struct AfterComboCheckEvent(EntityUid Performer, EntityUid Target, EntityUid Weapon, ComboAttackType Type);

[ByRefEvent]
public record struct GetPerformedAttackTypesEvent(List<ComboAttackType>? AttackTypes = null);
