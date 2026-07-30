using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
using Content.Shared.Buckle.Components;

namespace Content.Shared.ADT.Spike;

public abstract partial class SharedSpikeSystem : EntitySystem
{
    public static bool IsEntityImpaled(EntityUid entity, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent(entity, out BuckleComponent? buckle))
            return false;

        if (!buckle.Buckled || buckle.BuckledTo == null)
            return false;

        return entMan.HasComponent<SpikeComponent>(buckle.BuckledTo.Value);
    }
}

[Serializable, NetSerializable]
public sealed partial class ImpaleDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RemoveDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class EscapeDoAfterEvent : SimpleDoAfterEvent
{
}
