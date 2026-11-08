using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Construction.Events;

[Serializable, NetSerializable]
public sealed partial class ExchangerDoAfterEvent : SimpleDoAfterEvent;
