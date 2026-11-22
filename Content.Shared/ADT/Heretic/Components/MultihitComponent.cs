using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

/// <summary>
///     ADT: from Goob Multihit. Attacks again with another held weapon.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MultihitComponent : Component
{
    [DataField]
    public float DamageMultiplier = 0.67f;

    [DataField]
    public TimeSpan MultihitDelay = TimeSpan.FromSeconds(0.25);

    /// <summary>
    ///     Which held items count as a second weapon.
    /// </summary>
    [DataField]
    public EntityWhitelist? MultihitWhitelist;

    /// <summary>
    ///     Conditions on the wielder. Empty = always allowed.
    /// </summary>
    [DataField]
    public List<BaseMultihitUserConditionEvent> Conditions = new();

    [DataField]
    public bool RequireAllConditions;
}

/// <summary>
///     ADT: marks a weapon currently doing a multihit follow-up.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveMultihitComponent : Component
{
    [ViewVariables]
    public float DamageMultiplier = 1f;
}
