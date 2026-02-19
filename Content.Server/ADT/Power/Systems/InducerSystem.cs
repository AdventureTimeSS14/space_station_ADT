using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Server.Power.EntitySystems;

public sealed class InducerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InducerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<InducerComponent, InducerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InducerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnAfterInteract(EntityUid uid, InducerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        var target = args.Target.Value;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-battery"), uid, args.User);
            return;
        }

        if (!_itemSlots.TryGetSlot(uid, component.PowerCellSlotId, out var slot) || slot.Item == null ||
            !TryComp<BatteryComponent>(slot.Item.Value, out var sourceBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-power-cell"), uid, args.User);
            return;
        }

        if (sourceBattery.CurrentCharge <= 0)
        {
            _popup.PopupEntity(Loc.GetString("inducer-empty"), uid, args.User);
            return;
        }

        if (_battery.IsFull((target, targetBattery)))
        {
            _popup.PopupEntity(Loc.GetString("inducer-target-full"), uid, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.TransferDelay, new InducerDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            DistanceThreshold = component.MaxDistance,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, InducerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
            return;

        if (!_itemSlots.TryGetSlot(uid, component.PowerCellSlotId, out var slot) || slot.Item == null)
            return;

        if (!TryComp<BatteryComponent>(slot.Item.Value, out var sourceBattery))
            return;

        var energyToTransfer = component.TransferRate * component.TransferDelay;
        energyToTransfer = Math.Min(energyToTransfer, sourceBattery.CurrentCharge);

        var freeSpace = targetBattery.MaxCharge - targetBattery.CurrentCharge;
        energyToTransfer = Math.Min(energyToTransfer, freeSpace);

        if (energyToTransfer <= 0)
            return;

        if (_battery.TryUseCharge((slot.Item.Value, sourceBattery), energyToTransfer))
        {
            _battery.ChangeCharge((target, targetBattery), energyToTransfer);
            var percent = (int)(targetBattery.CurrentCharge / targetBattery.MaxCharge * 100);
            _popup.PopupEntity(Loc.GetString("inducer-success", ("percent", percent)), uid, args.User);

            if (targetBattery.CurrentCharge < targetBattery.MaxCharge * 0.95f)
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
            _battery.SetCharge((target, targetBattery), targetBattery.CurrentCharge + energyToTransfer);
            _battery.SetCharge((slot.Item.Value, sourceBattery), sourceBattery.CurrentCharge - energyToTransfer);
            args.Repeat = false;
        }
    }

    private void OnGetVerbs(EntityUid uid, InducerComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var priority = 0;
        foreach (var rate in component.AvailableTransferRates)
        {
            AlternativeVerb verb = new()
            {
                Text = Loc.GetString("inducer-set-transfer-rate", ("rate", rate)),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Category = VerbCategory.SelectType,
                Act = () =>
                {
                    component.TransferRate = rate;
                    Dirty(uid, component);
                    _popup.PopupEntity(Loc.GetString("inducer-transfer-rate-set", ("rate", rate)), uid, args.User);
                },
                Priority = priority
            };

            priority--;

            args.Verbs.Add(verb);
        }
    }
}
