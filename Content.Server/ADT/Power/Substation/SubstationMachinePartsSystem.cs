using Content.Server.Power.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;

namespace Content.Server.ADT.Power.Substation;

public sealed class SubstationMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubstationMachinePartsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SubstationMachinePartsComponent, RefreshPartsEvent>(OnPartsRefresh);
        SubscribeLocalEvent<SubstationMachinePartsComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnMapInit(EntityUid uid, SubstationMachinePartsComponent component, MapInitEvent args)
    {
        if (TryComp<PowerNetworkBatteryComponent>(uid, out var netBattery))
        {
            component.BaseMaxSupply = netBattery.MaxSupply;
            component.BaseMaxChargeRate = netBattery.MaxChargeRate;
        }

        if (TryComp<BatteryComponent>(uid, out var battery))
            component.BaseMaxCharge = battery.MaxCharge;
    }

    private void OnPartsRefresh(EntityUid uid, SubstationMachinePartsComponent component, RefreshPartsEvent args)
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
    }

    private void OnUpgradeExamine(EntityUid uid, SubstationMachinePartsComponent component, UpgradeExamineEvent args)
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
            args.AddPercentageUpgrade("machine-upgrade-capacity", battery.MaxCharge / component.BaseMaxCharge, benefit: true);
    }
}