using Content.Server.Power.Components;
<<<<<<< HEAD
using Content.Shared.Cargo;
using Content.Shared.Examine;
using Content.Shared.Power;
=======
>>>>>>> upstreamwiz/master
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Rejuvenate;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

<<<<<<< HEAD
[UsedImplicitly]
public sealed partial class BatterySystem : SharedBatterySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

=======
public sealed class BatterySystem : SharedBatterySystem
{
>>>>>>> upstreamwiz/master
    public override void Initialize()
    {
        base.Initialize();

<<<<<<< HEAD
        SubscribeLocalEvent<ExaminableBatteryComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BatteryComponent, RejuvenateEvent>(OnBatteryRejuvenate);
        SubscribeLocalEvent<PowerNetworkBatteryComponent, RejuvenateEvent>(OnNetBatteryRejuvenate);
        SubscribeLocalEvent<BatteryComponent, PriceCalculationEvent>(CalculateBatteryPrice);
        SubscribeLocalEvent<BatteryComponent, ChangeChargeEvent>(OnChangeCharge);
        SubscribeLocalEvent<BatteryComponent, GetChargeEvent>(OnGetCharge);

=======
        SubscribeLocalEvent<PowerNetworkBatteryComponent, RejuvenateEvent>(OnNetBatteryRejuvenate);
>>>>>>> upstreamwiz/master
        SubscribeLocalEvent<NetworkBatteryPreSync>(PreSync);
        SubscribeLocalEvent<NetworkBatteryPostSync>(PostSync);
    }

<<<<<<< HEAD
=======
    protected override void OnStartup(Entity<BatteryComponent> ent, ref ComponentStartup args)
    {
        // Debug assert to prevent anyone from killing their networking performance by dirtying a battery's charge every single tick.
        // This checks for components that interact with the power network, have a charge rate that ramps up over time and therefore
        // have to set the charge in an update loop instead of using a <see cref="RefreshChargeRateEvent"/> subscription.
        // This is usually the case for APCs, SMES, battery powered turrets or similar.
        // For those entities you should disable net sync for the battery in your prototype, using
        /// <code>
        /// - type: Battery
        ///   netSync: false
        /// </code>
        /// This disables networking and prediction for this battery.
        if (!ent.Comp.NetSyncEnabled)
            return;

        DebugTools.Assert(!HasComp<ApcPowerReceiverBatteryComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
        DebugTools.Assert(!HasComp<PowerNetworkBatteryComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
        DebugTools.Assert(!HasComp<PowerConsumerComponent>(ent), $"{ToPrettyString(ent.Owner)} has a predicted battery connected to the power net. Disable net sync!");
    }


>>>>>>> upstreamwiz/master
    private void OnNetBatteryRejuvenate(Entity<PowerNetworkBatteryComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.NetworkBattery.CurrentStorage = ent.Comp.NetworkBattery.Capacity;
    }

<<<<<<< HEAD
    private void OnBatteryRejuvenate(Entity<BatteryComponent> ent, ref RejuvenateEvent args)
    {
        SetCharge(ent.AsNullable(), ent.Comp.MaxCharge);
    }

    private void OnExamine(Entity<ExaminableBatteryComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp<BatteryComponent>(ent, out var battery))
            return;

        var chargePercentRounded = 0;
        if (battery.MaxCharge != 0)
            chargePercentRounded = (int)(100 * battery.CurrentCharge / battery.MaxCharge);
        args.PushMarkup(
            Loc.GetString(
                "examinable-battery-component-examine-detail",
                ("percent", chargePercentRounded),
                ("markupPercentColor", "green")
            )
        );
    }

=======
>>>>>>> upstreamwiz/master
    private void PreSync(NetworkBatteryPreSync ev)
    {
        // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
        var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
<<<<<<< HEAD
        while (enumerator.MoveNext(out var netBat, out var bat))
        {
            DebugTools.Assert(bat.CurrentCharge <= bat.MaxCharge && bat.CurrentCharge >= 0);
            netBat.NetworkBattery.Capacity = bat.MaxCharge;
            netBat.NetworkBattery.CurrentStorage = bat.CurrentCharge;
=======
        while (enumerator.MoveNext(out var uid, out var netBat, out var bat))
        {
            var currentCharge = GetCharge((uid, bat));
            DebugTools.Assert(currentCharge <= bat.MaxCharge && currentCharge >= 0);
            netBat.NetworkBattery.Capacity = bat.MaxCharge;
            netBat.NetworkBattery.CurrentStorage = currentCharge;
>>>>>>> upstreamwiz/master
        }
    }

    private void PostSync(NetworkBatteryPostSync ev)
    {
        // Ignoring entity pausing. If the entity was paused, neither component's data should have been changed.
        var enumerator = AllEntityQuery<PowerNetworkBatteryComponent, BatteryComponent>();
        while (enumerator.MoveNext(out var uid, out var netBat, out var bat))
        {
            SetCharge((uid, bat), netBat.NetworkBattery.CurrentStorage);
<<<<<<< HEAD
        }
    }

    /// <summary>
    /// Gets the price for the power contained in an entity's battery.
    /// </summary>
    private void CalculateBatteryPrice(Entity<BatteryComponent> ent, ref PriceCalculationEvent args)
    {
        args.Price += ent.Comp.CurrentCharge * ent.Comp.PricePerJoule;
    }

    private void OnChangeCharge(Entity<BatteryComponent> ent, ref ChangeChargeEvent args)
    {
        if (args.ResidualValue == 0)
            return;

        args.ResidualValue -= ChangeCharge(ent.AsNullable(), args.ResidualValue);
    }

    private void OnGetCharge(Entity<BatteryComponent> entity, ref GetChargeEvent args)
    {
        args.CurrentCharge += entity.Comp.CurrentCharge;
        args.MaxCharge += entity.Comp.MaxCharge;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BatterySelfRechargerComponent, BatteryComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var comp, out var bat))
        {
            if (!comp.AutoRecharge || IsFull((uid, bat)))
                continue;

            if (comp.NextAutoRecharge > curTime)
                continue;

            SetCharge((uid, bat), bat.CurrentCharge + comp.AutoRechargeRate * frameTime);
=======
>>>>>>> upstreamwiz/master
        }
    }
}
