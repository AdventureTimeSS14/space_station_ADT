using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rounding;
using Content.Shared.SMES;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Power.SMES;

[UsedImplicitly]
public sealed class SmesSystem : EntitySystem //ADT-tweak: made public
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<SmesComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SmesComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<SmesComponent, RefreshPartsEvent>(OnPartsRefresh); // ADT-Tweak
        SubscribeLocalEvent<SmesComponent, UpgradeExamineEvent>(OnUpgradeExamine); // ADT-Tweak
    }

    private void OnMapInit(EntityUid uid, SmesComponent component, MapInitEvent args)
    {
        // ADT-Tweak-Start: machine parts with tiers
        if (TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
        {
            component.BaseMaxSupply = netBattery.MaxSupply;
            component.BaseMaxChargeRate = netBattery.MaxChargeRate;
        }

        if (TryComp<BatteryComponent>(uid, out var battery))
            component.BaseMaxCharge = battery.MaxCharge;
        // ADT-Tweak-End

        UpdateSmesState(uid, component);
    }

    private void OnBatteryChargeChanged(EntityUid uid, SmesComponent component, ref ChargeChangedEvent args)
    {
        UpdateSmesState(uid, component);
    }

    // ADT-Tweak-Start: machine parts with tiers
    private void OnPartsRefresh(EntityUid uid, SmesComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
            return;

        var batteryTier = args.GetPartRating(MachinePartIds.PowerCell, 1f);
        var capacityMultiplier = batteryTier switch
        {
            >= 4f => 3f,
            >= 3f => 2f,
            >= 2f => 1.5f,
            _ => 1f,
        };

        if (TryComp<BatteryComponent>(uid, out var battery))
            _battery.SetMaxCharge((uid, battery), component.BaseMaxCharge * capacityMultiplier);

        netBattery.MaxSupply = component.BaseMaxSupply * args.GetStatMultiplier(MachineStat.ChargeRate);
        netBattery.MaxChargeRate = component.BaseMaxChargeRate * args.GetStatMultiplier(MachineStat.ChargeRate);

        UpdateSmesState(uid, component);
    }

    private void OnUpgradeExamine(EntityUid uid, SmesComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
            return;

        var inputMultiplier = component.BaseMaxChargeRate <= 0f
            ? 1f
            : netBattery.MaxChargeRate / component.BaseMaxChargeRate;
        var outputMultiplier = component.BaseMaxSupply <= 0f
            ? 1f
            : netBattery.MaxSupply / component.BaseMaxSupply;

        args.AddPercentageUpgrade("machine-upgrade-power-input", inputMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-power-output", outputMultiplier, benefit: true);

        if (TryComp<BatteryComponent>(uid, out var battery) && component.BaseMaxCharge > 0f)
            args.AddPercentageUpgrade("machine-upgrade-smes-capacity", battery.MaxCharge / component.BaseMaxCharge, benefit: true);
    }
    // ADT-Tweak-End

    private void UpdateSmesState(EntityUid uid, SmesComponent smes)
    {
        var newLevel = CalcChargeLevel(uid);
        if (newLevel != smes.LastChargeLevel && smes.LastChargeLevelTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeLevel = newLevel;
            smes.LastChargeLevelTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeLevel, newLevel);
        }

        var newChargeState = CalcChargeState(uid);
        if (newChargeState != smes.LastChargeState && smes.LastChargeStateTime + smes.VisualsChangeDelay < _gameTiming.CurTime)
        {
            smes.LastChargeState = newChargeState;
            smes.LastChargeStateTime = _gameTiming.CurTime;

            _appearance.SetData(uid, SmesVisuals.LastChargeState, newChargeState);
        }
    }

    private int CalcChargeLevel(EntityUid uid, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref battery, false))
            return 0;

        var currentCharge = _battery.GetCharge((uid, battery));
        return ContentHelpers.RoundToLevels(currentCharge, battery.MaxCharge, 6);
    }

    private ChargeState CalcChargeState(EntityUid uid, PowerNetworkBatteryComponent? netBattery = null)
    {
        if (!Resolve(uid, ref netBattery, false))
            return ChargeState.Still;

        return (netBattery.CurrentSupply - netBattery.CurrentReceiving) switch
        {
            > 0 => ChargeState.Discharging,
            < 0 => ChargeState.Charging,
            _ => ChargeState.Still
        };
    }
}
