using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Drake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ADTDrakeSwoopComponent : Component
{
    [DataField, AutoNetworkedField]
    public ADTDrakeSwoopPhase Phase = ADTDrakeSwoopPhase.Rising;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan PhaseEndsAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextStepAt;

    [DataField]
    public EntityUid? Target;

    [DataField]
    public bool LavaArena;

    [DataField]
    public bool LavaSuccess = true;

    [DataField]
    public TimeSpan Cooldown;

    [DataField]
    public ADTDrakeSwoopFollowUp FollowUp;

    [DataField]
    public int InitialX;

    [DataField]
    public bool Negative;

    public bool Invulnerable => Phase is ADTDrakeSwoopPhase.Ascending
        or ADTDrakeSwoopPhase.Chasing
        or ADTDrakeSwoopPhase.Arena
        or ADTDrakeSwoopPhase.Descending;
}

[Serializable, NetSerializable]
public enum ADTDrakeSwoopPhase : byte
{
    Rising,
    Ascending,
    Chasing,
    Arena,
    Descending,
    Landed,
}

public enum ADTDrakeSwoopFollowUp : byte
{
    None,
    LavaSwoopCones,
}
