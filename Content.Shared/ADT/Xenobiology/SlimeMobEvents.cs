
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology;

public sealed partial class SlimeLatchEvent : EntityTargetActionEvent
{
    [DataField]
    public float Damage = 5;
}

public sealed partial class XenoVacEvent : EntityTargetActionEvent;

public sealed partial class XenoVacClearEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class SlimeLatchDoAfterEvent : SimpleDoAfterEvent;
