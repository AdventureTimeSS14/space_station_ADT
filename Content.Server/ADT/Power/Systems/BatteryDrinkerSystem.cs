// Simple Station

using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.PowerCell.Components;
using Content.Shared.ADT.Silicon;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Server.ADT.Silicon.Charge;
using Content.Server.Power.EntitySystems;
using Content.Server.Popups;

namespace Content.Server.ADT.Power;

public sealed class BatteryDrinkerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SiliconChargeSystem _silicon = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);

        SubscribeLocalEvent<BatteryDrinkerComponent, BatteryDrinkerDoAfterEvent>(OnDoAfter);
    }

    private void AddAltVerb(EntityUid uid, BatteryComponent batteryComponent, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<BatteryDrinkerComponent>(args.User, out var drinkerComp) ||
            !TestDrinkableBattery(uid, drinkerComp) ||
            !TryGetFillableBattery(args.User, out var drinkerBattery, out _))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => DrinkBattery(uid, args.User, drinkerComp),
            Text = Loc.GetString("battery-drinker-verb-drink"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }

    private bool TestDrinkableBattery(EntityUid target, BatteryDrinkerComponent drinkerComp)
    {
        if (!drinkerComp.DrinkAll && !HasComp<BatteryDrinkerSourceComponent>(target))
            return false;

        return true;
    }

    private bool TryGetFillableBattery(EntityUid uid, [NotNullWhen(true)] out BatteryComponent? battery, [NotNullWhen(true)] out EntityUid batteryUid)
    {
        if (_silicon.TryGetSiliconBattery(uid, out battery, out batteryUid))
            return true;

        if (TryComp(uid, out battery))
            return true;

        if (TryComp<PowerCellSlotComponent>(uid, out var powerCellSlot) &&
            _slots.TryGetSlot(uid, powerCellSlot.CellSlotId, out var slot) &&
            slot.Item != null &&
            TryComp(slot.Item.Value, out battery))
        {
            batteryUid = slot.Item.Value;
            return true;
        }

        return false;
    }

    private void DrinkBattery(EntityUid target, EntityUid user, BatteryDrinkerComponent drinkerComp)
    {
        var doAfterTime = drinkerComp.DrinkSpeed;

        if (TryComp<BatteryDrinkerSourceComponent>(target, out var sourceComp))
            doAfterTime *= sourceComp.DrinkSpeedMulti;
        else
            doAfterTime *= drinkerComp.DrinkAllMultiplier;
        var args = new DoAfterArgs(_entityManager, user, doAfterTime, new BatteryDrinkerDoAfterEvent(), user, target) //modern.df
        //var args = new DoAfterArgs(user, doAfterTime, new BatteryDrinkerDoAfterEvent(), user, target) // TODO: Make this doafter loop, once we merge Upstream.
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            Broadcast = false,
            DistanceThreshold = 1.35f,
            RequireCanInteract = true,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnDoAfter(EntityUid uid, BatteryDrinkerComponent drinkerComp, DoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        var source = args.Target.Value;
        var drinker = uid;
        var sourceBattery = Comp<BatteryComponent>(source);

        TryGetFillableBattery(drinker, out var drinkerBattery, out var drinkerBatteryUid);

        TryComp<BatteryDrinkerSourceComponent>(source, out var sourceComp);

        DebugTools.AssertNotNull(drinkerBattery);

        if (drinkerBattery == null)
            return;

        var amountToDrink = drinkerComp.DrinkMultiplier * 1000;

        amountToDrink = MathF.Min(amountToDrink, sourceBattery.CurrentCharge);
        amountToDrink = MathF.Min(amountToDrink, drinkerBattery.MaxCharge - drinkerBattery.CurrentCharge);

        if (sourceComp != null && sourceComp.MaxAmount > 0)
            amountToDrink = MathF.Min(amountToDrink, (float) sourceComp.MaxAmount);

        if (amountToDrink <= 0)
        {
            _popup.PopupEntity(Loc.GetString("battery-drinker-empty", ("target", source)), drinker, drinker);
            return;
        }

        if (_battery.TryUseCharge(source, amountToDrink, sourceBattery))
            _battery.SetCharge(drinkerBatteryUid, drinkerBattery.CurrentCharge + amountToDrink, drinkerBattery);
        else
        {
            _battery.SetCharge(drinker, sourceBattery.CurrentCharge + drinkerBattery.CurrentCharge, drinkerBattery);
            _battery.SetCharge(source, 0, sourceBattery);
        }

        //if (sourceComp != null && sourceComp.DrinkSound != null)
        //    _audio.PlayPvs(sourceComp.DrinkSound, source);
    }
}
