using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mobs;

public sealed partial class TryCatchBreathAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed partial class TryCatchBreathDoAfterEvent : SimpleDoAfterEvent;
