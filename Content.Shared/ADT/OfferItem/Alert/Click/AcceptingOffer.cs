using Content.Shared.Alert;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Alert.Click;

/// <summary>
/// Accepting the offer and receive item
/// </summary>
public sealed partial class AcceptOfferAlertEvent : BaseAlertEvent;

/// <summary>
/// Receiver refuses the offered item.
/// </summary>
[Serializable, NetSerializable]
public sealed class OfferItemDeclineEvent : EntityEventArgs;
