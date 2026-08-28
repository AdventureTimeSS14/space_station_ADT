using Content.Shared.ADT.Shadowling;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Flash.Components;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.ADT.Silicon.Components;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private void InitializeDethrall()
    {
        SubscribeLocalEvent<ADTShadowlingThrallComponent, InteractUsingEvent>(OnThrallInteractUsing);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, ADTShadowlingDethrallDoAfterEvent>(OnDethrallDoAfter);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, DoAfterAttemptEvent<ADTShadowlingDethrallDoAfterEvent>>(OnDethrallAttempt);
    }

    private void OnThrallInteractUsing(Entity<ADTShadowlingThrallComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsLightSource(args.Used))
            return;

        args.Handled = true;

        if (!IsLightLit(args.Used))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-needs-lit"), args.User, args.User);
            return;
        }

        if (!IsRestrained(ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-needs-restrained"), args.User, args.User);
            return;
        }

        if (IsHiveNearby(ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-hive-nearby"), args.User, args.User, PopupType.MediumCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-begin", ("target", ent.Owner)), args.User, args.User);
        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-begin-thrall"), ent, ent, PopupType.LargeCaution);

        var ev = new ADTShadowlingDethrallDoAfterEvent(GetTotalDamage(ent));

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.DethrallTime, ev, ent, ent, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDethrallAttempt(Entity<ADTShadowlingThrallComponent> ent, ref DoAfterAttemptEvent<ADTShadowlingDethrallDoAfterEvent> args)
    {
        var user = args.DoAfter.Args.User;

        if (args.DoAfter.Args.Used is not { } light || !IsLightLit(light))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-needs-lit"), user, user, PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        if (!IsRestrained(ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-broke-free", ("target", ent.Owner)), user, user, PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        if (IsHiveNearby(ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-hive-nearby"), user, user, PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        if (GetTotalDamage(ent) > args.Event.StartDamage)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-hurt", ("target", ent.Owner)), user, user, PopupType.MediumCaution);
            args.Cancel();
        }
    }

    private void OnDethrallDoAfter(Entity<ADTShadowlingThrallComponent> ent, ref ADTShadowlingDethrallDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (TryComp<ADTLesserShadowlingComponent>(ent, out var lesser))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-thrall"), ent, ent, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("shadowling-dethrall-backlash-surgeon", ("target", ent.Owner)), args.User, args.User, PopupType.LargeCaution);

            _stun.TryKnockdown(args.User, lesser.SurgeryBacklashStun, true);

            var backlash = new DamageSpecifier();
            backlash.DamageDict.Add("Blunt", lesser.SurgeryBacklashDamage);
            _damageable.TryChangeDamage(args.User, backlash, true, origin: ent);
            return;
        }

        var tumorProto = ent.Comp.TumorProto;

        if (!_shadowling.TryFreeThrall(ent))
            return;

        Spawn(tumorProto, Transform(ent).Coordinates);
        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-success", ("target", ent.Owner)), args.User, args.User, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-freed"), ent, ent, PopupType.LargeCaution);
    }

    private bool IsLightSource(EntityUid uid)
    {
        return HasComp<HandheldLightComponent>(uid) || HasComp<FlashComponent>(uid);
    }

    private bool IsLightLit(EntityUid uid)
    {
        if (TryComp<HandheldLightComponent>(uid, out var handheld))
            return handheld.Activated;

        return HasComp<FlashComponent>(uid);
    }

    private bool IsRestrained(EntityUid uid)
    {
        if (HasComp<BorgChassisComponent>(uid) || HasComp<SiliconComponent>(uid))
            return true;

        if (TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled)
            return true;

        return TryComp<CuffableComponent>(uid, out var cuffable) && !cuffable.CanStillInteract;
    }

    private bool IsHiveNearby(Entity<ADTShadowlingThrallComponent> ent)
    {
        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.DethrallHiveRange))
        {
            if (nearby == ent.Owner)
                continue;

            if (IsHiveMember(nearby))
                return true;
        }

        return false;
    }

    private FixedPoint2 GetTotalDamage(EntityUid uid)
    {
        return TryComp<Shared.Damage.Components.DamageableComponent>(uid, out var damageable) ? damageable.TotalDamage : FixedPoint2.Zero;
    }
}
