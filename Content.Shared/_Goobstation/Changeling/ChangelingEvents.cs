using System;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Changeling;

#region Evolution events

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AugmentedEyesightPurchasedEvent : EntityEventArgs;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class AwakenedInstinctPurchasedEvent : EntityEventArgs;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class ChameleonSkinPurchasedEvent : EntityEventArgs;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DarknessAdaptionPurchasedEvent : EntityEventArgs;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class VoidAdaptionPurchasedEvent : EntityEventArgs;

#endregion
