using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Hierophant.Effects;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class HierophantChaserComponent : Component
{
    [DataField]
    public EntityUid? Caster;

    [DataField]
    public EntityUid? Target;

    [DataField]
    public Direction MovingDir = Direction.South;

    [DataField]
    public Direction? PreviousMovingDir;

    [DataField]
    public Direction? MorePreviouserMovingDir;

    [DataField]
    public int Moving;

    [DataField]
    public int StandardMovingBeforeRecalc = 4;

    [DataField]
    public int TilesPerStep = 1;

    [DataField]
    public float Speed = 0.3f;

    [DataField, AutoPausedField]
    public TimeSpan NextStepAt;

    [DataField]
    public float Damage = 10f;

    [DataField]
    public bool MonsterDamageBoost = true;

    [DataField]
    public bool FriendlyFireCheck;
}
