using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseCursedKatanaEvent : EntityEventArgs;

[DataDefinition]
public sealed partial class KatanaTendonCutPerformedEvent : BaseCursedKatanaEvent;

[DataDefinition]
public sealed partial class KatanaHiltStrikePerformedEvent : BaseCursedKatanaEvent;

[DataDefinition]
public sealed partial class KatanaDashPerformedEvent : BaseCursedKatanaEvent;

[DataDefinition]
public sealed partial class KatanaDarkHealPerformedEvent : BaseCursedKatanaEvent;
