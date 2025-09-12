using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Passivly regerenate holy type of damage
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PassiveHolyHealingComponent : Component
{
    /// <summary>
    /// Quantity of healed holy damage per tic
    /// </summary>
    [DataField("healAmount"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HealAmount = -1.0;

    /// <summary>
    /// Interval of healing per sec
    /// </summary>
    [DataField("interval"), ViewVariables(VVAccess.ReadWrite)]
    public float Interval = 1f;

    [DataField("nextHealTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextHealTime;
}