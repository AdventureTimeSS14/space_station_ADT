using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Silicon.BatterySlot;
using Content.Server.ADT.Silicon.Charge;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Silicon;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Power;

public sealed class BatteryDrinkerSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
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

    private bool TryGetIPCBattery(EntityUid uid,
        [NotNullWhen(true)] out BatteryComponent? battery,
        [NotNullWhen(true)] out EntityUid batteryUid)
    {
        battery = null;
        batteryUid = default;

        if (!TryComp<BatterySlotRequiresLockComponent>(uid, out var slotComp))
            return false;

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return false;

        if (!containerManager.Containers.TryGetValue(slotComp.ItemSlot, out var container))
            return false;

        if (container.ContainedEntities.Count == 0)
            return false;

        var cellUid = container.ContainedEntities[0];

        if (!TryComp(cellUid, out battery))
            return false;

        batteryUid = cellUid;
        return true;
    }

    private bool TryGetFillableBattery(EntityUid uid,
        [NotNullWhen(true)] out BatteryComponent? battery,
        [NotNullWhen(true)] out EntityUid batteryUid)
    {
        if (TryGetIPCBattery(uid, out battery, out batteryUid))
            return true;

        if (_silicon.TryGetSiliconBattery(uid, out battery, out batteryUid))
            return true;

        if (TryComp(uid, out battery))
        {
            batteryUid = uid;
            return true;
        }

        batteryUid = default;
        return false;
    }

    private void DrinkBattery(EntityUid target, EntityUid user, BatteryDrinkerComponent drinkerComp)
    {
        var doAfterTime = drinkerComp.DrinkSpeed;

        if (TryComp<BatteryDrinkerSourceComponent>(target, out var sourceComp))
            doAfterTime *= sourceComp.DrinkSpeedMulti;
        else
            doAfterTime *= drinkerComp.DrinkAllMultiplier;

        var args = new DoAfterArgs(_entityManager, user, doAfterTime, new BatteryDrinkerDoAfterEvent(), user, target)
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

        if (!TryComp<BatteryComponent>(source, out var sourceBattery))
            return;

        if (!TryGetFillableBattery(drinker, out var drinkerBattery, out var drinkerBatteryUid))
            return;
        if (!TryComp<BatteryDrinkerSourceComponent>(source, out var sourceComp))
            return;

        var amountToDrink = drinkerBattery.MaxCharge * 0.10f;
        amountToDrink = MathF.Min(amountToDrink, sourceBattery.CurrentCharge);
        amountToDrink = MathF.Min(amountToDrink, drinkerBattery.MaxCharge - drinkerBattery.CurrentCharge);

        if (sourceComp.MaxAmount > 0)
            amountToDrink = MathF.Min(amountToDrink, (float)sourceComp.MaxAmount);

        if (amountToDrink <= 0)
        {
            _popup.PopupEntity(Loc.GetString("battery-drinker-empty", ("target", source)), drinker, drinker);
            return;
        }

        var tryUse = _battery.TryUseCharge(source, amountToDrink);
        if (tryUse)
        {
            _battery.SetCharge(drinkerBatteryUid, drinkerBattery.CurrentCharge + amountToDrink);
            if (drinkerBattery.CurrentCharge < drinkerBattery.MaxCharge * 0.95f)
            {
                args.Repeat = true;
            }
            else
            {
                args.Repeat = false;
            }
        }
        else
        {
            _battery.SetCharge(drinker, sourceBattery.CurrentCharge + drinkerBattery.CurrentCharge);
            _battery.SetCharge(source, 0);
            args.Repeat = false;
        }
    }
}
