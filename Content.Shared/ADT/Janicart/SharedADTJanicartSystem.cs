using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Janicart;

public abstract partial class SharedADTJanicartSystem : EntitySystem
{
}

[Serializable, NetSerializable]
public sealed partial class ADTJanicartUpgradeRemoveDoAfterEvent : SimpleDoAfterEvent;