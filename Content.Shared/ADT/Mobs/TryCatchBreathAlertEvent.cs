using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mobs;

public sealed partial class TryCatchBreathAlertEvent : BaseAlertEvent
{
    public TryCatchBreathAlertEvent(EntityUid user, ProtoId<AlertPrototype> alertId)
        : base(user, alertId)
    {
    }
}

[Serializable, NetSerializable]
public sealed partial class TryCatchBreathDoAfterEvent : SimpleDoAfterEvent;
