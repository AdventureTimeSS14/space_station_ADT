using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shadowling;

public sealed partial class ADTShadowlingHatchEvent : InstantActionEvent;

public sealed partial class ADTShadowlingEnthrallEvent : EntityTargetActionEvent;

public sealed partial class ADTShadowlingGlareEvent : EntityTargetActionEvent;

public sealed partial class ADTShadowlingVeilEvent : InstantActionEvent;

public sealed partial class ADTShadowlingShadowWalkEvent : InstantActionEvent;

public sealed partial class ADTShadowlingIcyVeinsEvent : InstantActionEvent;

public sealed partial class ADTShadowlingCollectiveMindEvent : InstantActionEvent;

public sealed partial class ADTShadowlingBlindnessSmokeEvent : InstantActionEvent;

public sealed partial class ADTShadowlingScreechEvent : InstantActionEvent;

public sealed partial class ADTShadowlingNullChargeEvent : EntityTargetActionEvent;

public sealed partial class ADTShadowlingBlackRecuperationEvent : EntityTargetActionEvent;

public sealed partial class ADTShadowlingDestroyEnginesEvent : EntityTargetActionEvent;

public sealed partial class ADTShadowlingAscendEvent : InstantActionEvent;

public sealed partial class ADTAscendantAnnihilateEvent : EntityTargetActionEvent;

public sealed partial class ADTAscendantHypnosisEvent : EntityTargetActionEvent;

public sealed partial class ADTAscendantPhaseShiftEvent : InstantActionEvent;

public sealed partial class ADTAscendantUnphaseEvent : InstantActionEvent;

public sealed partial class ADTAscendantLightningStormEvent : InstantActionEvent;

public sealed partial class ADTAscendantBroadcastEvent : InstantActionEvent;

public sealed partial class ADTShadowlingGuiseEvent : InstantActionEvent;

public sealed partial class ADTShadowlingDarksightEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingCollectiveMindDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingEmpowerDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingReviveDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingDestroyEnginesDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingAscendDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int Stage;

    public ADTShadowlingAscendDoAfterEvent()
    {
    }

    public ADTShadowlingAscendDoAfterEvent(int stage)
    {
        Stage = stage;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingDethrallDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingHatchDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int Stage;

    public ADTShadowlingHatchDoAfterEvent()
    {
    }

    public ADTShadowlingHatchDoAfterEvent(int stage)
    {
        Stage = stage;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTShadowlingEnthrallDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int Stage;

    public ADTShadowlingEnthrallDoAfterEvent()
    {
    }

    public ADTShadowlingEnthrallDoAfterEvent(int stage)
    {
        Stage = stage;
    }
}

