using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CookbookChopComboPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CookbookSpinComboPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CookbookRollComboPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CookbookSqueezeComboPerformedEvent : EntityEventArgs;
