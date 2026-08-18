using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Attached to APC powered entities that possess a rechargeable internal battery.
/// If external power is interrupted, the entity will draw power from this battery instead.
/// Requires <see cref="Content.Server.Power.Components.ApcPowerReceiverComponent"/> and <see cref="BatteryComponent"/> to function.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPowerNetSystem), typeof(SharedPowerReceiverSystem))]
public sealed partial class ApcPowerReceiverBatteryComponent : Component
{
    /// <summary>
    /// Indicates whether power is currently being drawn from the battery.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    // ADT Tweak start
    /// <summary>
    /// Whether the battery acts as an emergency backup: when external power is
    /// missing it keeps the device "Powered" (and drains the battery to do so).
    /// Set to false to make the device power off entirely and preserve the charge.
    /// </summary>
    [DataField("batteryBackup")]
    public bool BatteryBackup = true;
    // ADT Tweak end

    /// <summary>
    /// The passive load the entity places on the APC power network.
    /// If not connected to an active APC power network, this amount
    /// of power is drained from the battery every second.
    /// </summary>
    [DataField]
    public float IdleLoad = 5f;

    /// <summary>
    /// Determines how much battery charge the entity's battery gains
    /// per second when connected to an active APC power network.
    /// </summary>
    [DataField]
    public float BatteryRechargeRate = 50f;

    /// <summary>
    /// While the battery is being recharged, the load this entity places on the APC
    /// power network is increased by the <see cref="BatteryRechargeRate"/> multiplied
    /// by this factor.
    /// </summary>
    [DataField]
    public float BatteryRechargeEfficiency = 1f;
}

/// <summary>
/// Raised whenever an ApcPowerReceiverBattery starts / stops discharging
/// </summary>
[ByRefEvent]
public readonly record struct ApcPowerReceiverBatteryChangedEvent(bool Enabled);
