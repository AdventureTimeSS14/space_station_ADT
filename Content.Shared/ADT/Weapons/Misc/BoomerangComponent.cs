using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Weapons.Boomerang;

/// <summary>
/// Component for entities that should boomerang back to their thrower once thrown
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class BoomerangComponent : Component
{
    /// <summary>
    /// Entity we should return to after landing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Thrower;

    /// <summary>
    /// Distance to thrower we should try get picked up at or fail
    /// </summary>
    [DataField]
    public float PickupDistance = 1.5f;

    /// <summary>
    /// Speed we should return at
    /// </summary>
    [DataField]
    public float ReturnSpeed = 10f;

    /// <summary>
    /// Maximum return hops we can make
    /// </summary>
    [DataField]
    public int MaxHops = 6;

    /// <summary>
    /// Return hops we've made so far
    /// </summary>
    [DataField, AutoNetworkedField]
    public int CurrentHops = 0;
}