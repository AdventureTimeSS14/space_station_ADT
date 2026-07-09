using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class PtsdLegSweepComboPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class PtsdArmLockBehindBackComboPerformedEvent : EntityEventArgs;
[Serializable, NetSerializable, DataDefinition]
public sealed partial class PtsdBootKickComboPerformedEvent : EntityEventArgs;