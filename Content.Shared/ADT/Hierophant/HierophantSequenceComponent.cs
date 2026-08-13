using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Hierophant;

[RegisterComponent]
public sealed partial class HierophantSequenceComponent : Component
{
    [DataField]
    public List<HierophantStep> Queue = new();
}

[DataDefinition]
public sealed partial class HierophantStep
{
    [DataField]
    public TimeSpan ExecuteAt;

    [DataField]
    public HierophantStepType Type;

    [DataField]
    public EntityCoordinates Coords;

    [DataField]
    public Direction Direction;

    [DataField]
    public List<Direction>? Directions;

    [DataField]
    public float Damage;

    [DataField]
    public float Speed;

    [DataField]
    public int Amount;

    [DataField]
    public bool FriendlyFireCheck;

    [DataField]
    public bool MonsterDamageBoost = true;

    [DataField]
    public string? Message;

    [DataField]
    public EntityUid? Target;
}

[Serializable, NetSerializable]
public enum HierophantStepType : byte
{
    /// <summary>Spawn a single blast</summary>
    Blast,

    /// <summary>Spawn a plain telegraph decal</summary>
    Telegraph,

    /// <summary>Spawn the cardinal-flavoured telegraph</summary>
    TelegraphCardinal,

    /// <summary>Spawn the diagonal-flavoured telegraph</summary>
    TelegraphDiagonal,

    /// <summary>Spawn the teleport telegraph</summary>
    TelegraphTeleport,

    /// <summary>Spawn a centre blast plus a wall of blasts down each listed direction</summary>
    CrossBlast,

    /// <summary>Spawn the square trail effect</summary>
    Squares,

    /// <summary>Spawn a chaser aimed at Target</summary>
    Chaser,

    /// <summary>Spawn a temporary arena wall</summary>
    Wall,

    /// <summary>Begin fading out for a blink</summary>
    BlinkFadeOut,

    /// <summary>Actually move to the blink destination</summary>
    BlinkMove,

    /// <summary>Begin fading back in</summary>
    BlinkFadeIn,

    /// <summary>Finish the blink, restoring density and clearing the blinking flag</summary>
    BlinkFinish,

    /// <summary>Set the blinking flag, which stops movement and further attacks</summary>
    SetBlinking,

    /// <summary>Clear the blinking flag</summary>
    ClearBlinking,

    /// <summary>Shift to the attack colour</summary>
    SetAttackColor,

    /// <summary>Shift back to the normal colour</summary>
    ClearAttackColor,

    /// <summary>Speak a line</summary>
    Say,
}
