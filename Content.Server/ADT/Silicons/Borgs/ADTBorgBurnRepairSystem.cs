using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.ADT.Silicons.Borgs.Components;
using Content.Shared.ADT.Damage;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stacks;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTBorgBurnRepairSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBorgBurnRepairableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ADTBorgBurnRepairableComponent, ADTBorgBurnRepairDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractUsing(Entity<ADTBorgBurnRepairableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<CablePlacerComponent>(args.Used))
            return;

        args.Handled = true;

        if (!CanRepair(ent, args.Target, out var failKey))
        {
            _popup.PopupEntity(Loc.GetString(failKey), args.Target, args.User);
            return;
        }

        if (_stack.GetCount(args.Used) < ent.Comp.CablePerUse)
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-repair-no-cable"), args.Target, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ADTBorgBurnRepairDoAfterEvent(), ent, target: args.Target, used: args.Used);
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<ADTBorgBurnRepairableComponent> ent, ref ADTBorgBurnRepairDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target || args.Used is not { } used)
            return;

        if (!CanRepair(ent, target, out _))
            return;

        var stackCount = _stack.GetCount(used);
        if (stackCount < ent.Comp.CablePerUse)
            return;

        _stack.SetCount(used, stackCount - ent.Comp.CablePerUse);

        _damageable.ChangeDamage(target, ent.Comp.Damage, true, false, origin: args.User);
        _audio.PlayPvs(ent.Comp.RepairSound, target);

        _popup.PopupEntity(Loc.GetString("adt-borg-repair-burn"), target, args.User);

        if (CanRepair(ent, target, out _) && _stack.GetCount(used) >= ent.Comp.CablePerUse)
            args.Repeat = true;

        args.Handled = true;
    }

    private bool CanRepair(
        Entity<ADTBorgBurnRepairableComponent> ent,
        EntityUid target,
        out string failKey)
    {
        if (!TryComp<WiresPanelComponent>(target, out var panel) || !panel.Open)
        {
            failKey = "adt-borg-repair-panel-closed";
            return false;
        }

        if (_powerCell.TryGetBatteryFromSlot(target, out _))
        {
            failKey = "adt-borg-repair-battery";
            return false;
        }

        if (!TryComp<DamageableComponent>(target, out var damageable))
        {
            failKey = "adt-borg-repair-no-damage";
            return false;
        }

        if (ent.Comp.Damage.HasHealableDamage(_damageable.GetAllDamage((target, damageable))))
        {
            failKey = string.Empty;
            return true;
        }

        failKey = "adt-borg-repair-no-damage";
        return false;
    }
}
