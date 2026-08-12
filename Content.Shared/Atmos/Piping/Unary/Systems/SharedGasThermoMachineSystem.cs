using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Atmos.Piping.Unary.Systems;

public abstract class SharedGasThermoMachineSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GasThermoMachineComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineToggleMessage>(OnToggleMessage);

        // ADT-Tweak-Start: machine parts with tiers
        SubscribeLocalEvent<GasThermoMachineComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<GasThermoMachineComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        // ADT-Tweak-End
        SubscribeLocalEvent<GasThermoMachineComponent, GasThermomachineChangeTemperatureMessage>(OnChangeTemperature);
    }

    private void OnExamined(EntityUid uid, GasThermoMachineComponent thermoMachine, ExaminedEvent args)
    {
        if (Loc.TryGetString("gas-thermomachine-system-examined",
                out var str,
                ("machineName", !IsHeater(thermoMachine) ? "freezer" : "heater"),
                ("tempColor", !IsHeater(thermoMachine) ? "deepskyblue" : "red"),
                ("temp", Math.Round(thermoMachine.TargetTemperature, 2))
            ))
        {
            args.PushMarkup(str);
        }
    }

    public bool IsHeater(GasThermoMachineComponent comp)
    {
        return comp.Cp >= 0;
    }

    private void OnToggleMessage(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineToggleMessage args)
    {
        var powerState = _receiver.TogglePower(uid, user: args.Actor);
        _adminLogger.Add(LogType.AtmosPowerChanged, $"{ToPrettyString(args.Actor)} turned {(powerState ? "On" : "Off")} {ToPrettyString(uid)}");
        DirtyUI(uid, thermoMachine);
    }

    private void OnChangeTemperature(EntityUid uid, GasThermoMachineComponent thermoMachine, GasThermomachineChangeTemperatureMessage args)
    {
        if (IsHeater(thermoMachine))
            thermoMachine.TargetTemperature = MathF.Min(args.Temperature, GetMaxTemperature(thermoMachine)); // ADT-Tweak machine parts
        else
            thermoMachine.TargetTemperature = MathF.Max(args.Temperature, GetMinTemperature(thermoMachine)); // ADT-Tweak machine parts
        thermoMachine.TargetTemperature = MathF.Max(thermoMachine.TargetTemperature, Atmospherics.TCMB);
        _adminLogger.Add(LogType.AtmosTemperatureChanged, $"{ToPrettyString(args.Actor)} set temperature on {ToPrettyString(uid)} to {thermoMachine.TargetTemperature}");
        Dirty(uid, thermoMachine);
        DirtyUI(uid, thermoMachine);
    }

    // ADT-Tweak-Start: machine parts with tiers
    private void OnRefreshParts(EntityUid uid, GasThermoMachineComponent component, RefreshPartsEvent args)
    {
        component.HeatCapacityMultiplier = args.GetStatMultiplier(MachineStat.Capacity);
        component.TemperatureRangeBonus = (args.GetPartRating(MachinePartIds.MicroLaser) - 1f) * 30f;

        component.TargetTemperature = Math.Clamp(component.TargetTemperature, GetMinTemperature(component), GetMaxTemperature(component));
        Dirty(uid, component);
        DirtyUI(uid, component);
    }

    private static void OnUpgradeExamine(EntityUid uid, GasThermoMachineComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-thermomachine-heat-capacity", component.HeatCapacityMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-thermomachine-temp-range", (GetMaxTemperature(component) - GetMinTemperature(component)) / (component.MaxTemperature - component.MinTemperature), benefit: true);
    }

    public static float GetMinTemperature(GasThermoMachineComponent component) => MathF.Max(Atmospherics.TCMB, component.MinTemperature - component.TemperatureRangeBonus);

    public static float GetMaxTemperature(GasThermoMachineComponent component) => component.MaxTemperature + component.TemperatureRangeBonus;
    // ADT-Tweak-End

    protected virtual void DirtyUI(EntityUid uid, GasThermoMachineComponent? thermoMachine, UserInterfaceComponent? ui=null) {}
}
