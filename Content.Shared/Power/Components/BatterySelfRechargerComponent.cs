<<<<<<< HEAD
=======
using Robust.Shared.GameStates;
>>>>>>> upstreamwiz/master
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Power.Components;

/// <summary>
/// Self-recharging battery.
/// To be used in combination with <see cref="BatteryComponent"/>.
/// </summary>
<<<<<<< HEAD
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class BatterySelfRechargerComponent : Component
{
    /// <summary>
    /// Is the component currently enabled?
    /// </summary>
    [DataField]
    public bool AutoRecharge = true;

    /// <summary>
    /// At what rate does the entity automatically recharge?
    /// </summary>
    [DataField]
    public float AutoRechargeRate;

    /// <summary>
    /// How long should the entity stop automatically recharging if charge is used?
    /// </summary>
    [DataField]
    public TimeSpan AutoRechargePauseTime = TimeSpan.FromSeconds(0);
=======
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class BatterySelfRechargerComponent : Component
{
    /// <summary>
    /// At what rate does the entity automatically recharge? In watts.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables]
    public float AutoRechargeRate;

    /// <summary>
    /// How long should the entity stop automatically recharging if a charge is used?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AutoRechargePauseTime = TimeSpan.Zero;
>>>>>>> upstreamwiz/master

    /// <summary>
    /// Do not auto recharge if this timestamp has yet to happen, set for the auto recharge pause system.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
<<<<<<< HEAD
    [AutoPausedField]
    public TimeSpan NextAutoRecharge = TimeSpan.FromSeconds(0);
=======
    [AutoNetworkedField, AutoPausedField, ViewVariables]
    public TimeSpan? NextAutoRecharge = TimeSpan.FromSeconds(0);
>>>>>>> upstreamwiz/master
}
