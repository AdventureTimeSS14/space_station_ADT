using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Tag;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private void InitializeProgression()
    {
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingCollectiveMindEvent>(OnCollectiveMind);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingCollectiveMindDoAfterEvent>(OnCollectiveMindDoAfter);

        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingBlindnessSmokeEvent>(OnBlindnessSmoke);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingScreechEvent>(OnScreech);

        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingNullChargeEvent>(OnNullCharge);

        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingBlackRecuperationEvent>(OnBlackRecuperation);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingEmpowerDoAfterEvent>(OnEmpowerDoAfter);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingReviveDoAfterEvent>(OnReviveDoAfter);

        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingDestroyEnginesEvent>(OnDestroyEngines);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingDestroyEnginesDoAfterEvent>(OnDestroyEnginesDoAfter);
    }

    public int CountLivingThralls()
    {
        var count = 0;
        var query = EntityQueryEnumerator<ADTShadowlingThrallComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (!_mobState.IsDead(uid))
                count++;
        }

        return count;
    }

    private void OnCollectiveMind(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingCollectiveMindEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingCollectiveMindActionComponent>(args.Action, out var mind))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-hivemind-begin"), ent, ent);

        var doAfter = new DoAfterArgs(EntityManager, ent, mind.CastTime, new ADTShadowlingCollectiveMindDoAfterEvent(), ent, used: args.Action)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnCollectiveMindDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingCollectiveMindDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-hivemind-interrupted"), ent, ent, PopupType.MediumCaution);
            return;
        }

        args.Handled = true;

        if (TryComp<ADTShadowlingCollectiveMindActionComponent>(args.Args.Used ?? EntityUid.Invalid, out var mind))
            _damageable.TryChangeDamage(ent.Owner, mind.SelfHeal, true, false);

        var thralls = CountLivingThralls();
        ent.Comp.KnownThralls = thralls;
        Dirty(ent);

        foreach (var (action, needed) in ent.Comp.ProgressionAbilities)
        {
            if (thralls < needed)
                continue;

            if (_shadowling.TryUnlockAbility(ent, action))
                _popup.PopupEntity(Loc.GetString("shadowling-hivemind-unlocked"), ent, ent, PopupType.Medium);
        }

        if (thralls < ent.Comp.RequiredThralls)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-hivemind-count", ("current", thralls), ("needed", ent.Comp.RequiredThralls)), ent, ent);
            return;
        }

        var query = EntityQueryEnumerator<ADTShadowlingComponent>();
        while (query.MoveNext(out var uid, out var other))
        {
            if (other.AscensionUnlocked || !other.Hatched)
                continue;

            other.AscensionUnlocked = true;
            _shadowling.TryUnlockAbility((uid, other), other.AscendAction);
            _popup.PopupEntity(Loc.GetString("shadowling-hivemind-ascension-ready"), uid, uid, PopupType.LargeCaution);
        }
    }

    private void OnBlindnessSmoke(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingBlindnessSmokeEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingBlindnessSmokeActionComponent>(args.Action, out var smokeAction))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-smoke-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("shadowling-smoke-others", ("user", ent.Owner)), ent, Filter.PvsExcept(ent.Owner), true, PopupType.MediumCaution);
        _audio.PlayPvs(smokeAction.Sound, ent);

        var smoke = Spawn(smokeAction.SmokeProto, Transform(ent).Coordinates.SnapToGrid(EntityManager));
        if (!TryComp<SmokeComponent>(smoke, out var smokeComp))
        {
            QueueDel(smoke);
            return;
        }

        var solution = new Solution();
        solution.AddReagent(smokeAction.Reagent, smokeAction.ReagentAmount);
        _smoke.StartSmoke(smoke, solution, smokeAction.Duration, smokeAction.SpreadAmount, smokeComp);

        args.Handled = true;
    }

    private void OnScreech(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingScreechEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingScreechActionComponent>(args.Action, out var screech))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-screech-cast", ("user", ent.Owner)), ent, PopupType.LargeCaution);
        _audio.PlayPvs(screech.Sound, ent);

        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, screech.Range))
        {
            if (_tag.HasTag(nearby, WindowTag))
            {
                _damageable.TryChangeDamage(nearby, screech.WindowDamage, true, origin: ent.Owner);
                continue;
            }

            if (!HasComp<MobStateComponent>(nearby) || IsHiveMember(nearby) || _mobState.IsDead(nearby))
                continue;

            if (HasComp<BorgChassisComponent>(nearby))
            {
                _popup.PopupEntity(Loc.GetString("shadowling-screech-silicon"), nearby, nearby, PopupType.LargeCaution);
                _borgShutdown.TryShutdown(nearby, screech.SiliconShutdown);
                continue;
            }

            _popup.PopupEntity(Loc.GetString("shadowling-screech-hit"), nearby, nearby, PopupType.LargeCaution);
            _statusNew.TryAddStatusEffectDuration(nearby, screech.ConfusionEffect, screech.ConfusionTime);
            _deafness.Deafen(nearby, screech.Deafness);
            _stamina.TryTakeStamina(nearby, screech.StaminaDamage, source: ent.Owner);
        }

        args.Handled = true;
    }

    private void OnNullCharge(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingNullChargeEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingNullChargeActionComponent>(args.Action, out var nullCharge))
            return;

        var target = args.Target;

        if (!TryComp<ApcComponent>(target, out var apc))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-null-charge-invalid"), ent, ent);
            return;
        }

        if (!TryComp<BatteryComponent>(target, out var battery) || _battery.GetCharge((target, battery)) <= 0)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-null-charge-empty"), ent, ent);
            return;
        }

        _battery.SetCharge((target, battery), 0);

        if (apc.MainBreakerEnabled)
            _apc.ApcToggleBreaker(target, apc, user: ent);

        _audio.PlayPvs(nullCharge.Sound, target);
        _popup.PopupEntity(Loc.GetString("shadowling-null-charge-done"), ent, ent, PopupType.Medium);

        args.Handled = true;
    }

    private void OnBlackRecuperation(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingBlackRecuperationEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingBlackRecuperationActionComponent>(args.Action, out var power))
            return;

        var target = args.Target;
        if (!HasComp<ADTShadowlingThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-recuperation-not-thrall"), ent, ent);
            return;
        }

        if (_mobState.IsDead(target))
        {
            StartRecuperation(ent, target, power.ReviveTime, new ADTShadowlingReviveDoAfterEvent(), args.Action);
            _popup.PopupEntity(Loc.GetString("shadowling-recuperation-revive-begin"), ent, ent);
            args.Handled = true;
            return;
        }

        if (HasComp<ADTLesserShadowlingComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-recuperation-already-empowered"), ent, ent);
            return;
        }

        if (!power.IgnoreLimit && CountEmpoweredThralls() >= power.MaxEmpowered)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-recuperation-too-many"), ent, ent, PopupType.MediumCaution);
            return;
        }

        StartRecuperation(ent, target, power.EmpowerTime, new ADTShadowlingEmpowerDoAfterEvent(), args.Action);
        _popup.PopupEntity(Loc.GetString("shadowling-recuperation-empower-begin"), ent, ent);
        args.Handled = true;
    }

    private void StartRecuperation(EntityUid user, EntityUid target, TimeSpan time, DoAfterEvent ev, EntityUid action)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, time, ev, user, target, action)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnEmpowerDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingEmpowerDoAfterEvent args)
    {
        DoEmpower(ref args);
    }

    private void DoEmpower(ref ADTShadowlingEmpowerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (!TryComp<ADTShadowlingBlackRecuperationActionComponent>(args.Args.Used ?? EntityUid.Invalid, out var power))
            return;

        var empowered = _polymorph.PolymorphEntity(target, power.EmpowerPolymorph);
        if (empowered == null)
            return;

        EnsureComp<ADTShadowlingThrallComponent>(empowered.Value);
        _audio.PlayPvs(power.Sound, empowered.Value);
        _popup.PopupEntity(Loc.GetString("shadowling-recuperation-empowered"), empowered.Value, empowered.Value, PopupType.LargeCaution);
    }

    private void OnReviveDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingReviveDoAfterEvent args)
    {
        DoRevive(ref args);
    }

    private void DoRevive(ref ADTShadowlingReviveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (TryComp<ADTShadowlingBlackRecuperationActionComponent>(args.Args.Used ?? EntityUid.Invalid, out var power))
            _audio.PlayPvs(power.Sound, target);

        _rejuvenate.PerformRejuvenate(target);
        _popup.PopupEntity(Loc.GetString("shadowling-recuperation-revived"), target, target, PopupType.LargeCaution);
    }

    private int CountEmpoweredThralls()
    {
        var count = 0;
        var query = EntityQueryEnumerator<ADTLesserShadowlingComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (!_mobState.IsDead(uid))
                count++;
        }

        return count;
    }

    private void OnDestroyEngines(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingDestroyEnginesEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        if (!TryComp<ADTShadowlingDestroyEnginesActionComponent>(args.Action, out var power))
            return;

        if (IsHiveMember(args.Target) || !HasComp<MobStateComponent>(args.Target) || _mobState.IsDead(args.Target))
            return;

        if (_roundEnd.ExpectedCountdownEnd == null)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-destroy-engines-no-shuttle"), ent, ent, PopupType.MediumCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("shadowling-destroy-engines-begin"), ent, ent);
        _popup.PopupEntity(Loc.GetString("shadowling-destroy-engines-victim"), args.Target, args.Target, PopupType.LargeCaution);

        var doAfter = new DoAfterArgs(EntityManager, ent, power.CastTime, new ADTShadowlingDestroyEnginesDoAfterEvent(), ent, args.Target, args.Action)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
        args.Handled = true;
    }

    private void OnDestroyEnginesDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingDestroyEnginesDoAfterEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-destroy-engines-interrupted"), ent, ent, PopupType.MediumCaution);
            return;
        }

        args.Handled = true;

        if (!TryComp<ADTShadowlingDestroyEnginesActionComponent>(args.Args.Used ?? EntityUid.Invalid, out var power))
            return;

        if (_roundEnd.ExpectedCountdownEnd is not { } end)
            return;

        _audio.PlayPvs(power.Sound, target);
        _shadowling.KillOutright(target, ent);

        var remaining = end - _timing.CurTime;
        _roundEnd.RequestRoundEnd(remaining + power.ShuttleDelay,
            checkCooldown: false,
            text: "shadowling-destroy-engines-announcement",
            name: "shadowling-destroy-engines-announcer",
            cantRecall: true);

        var action = args.Args.Used;
        if (action != null)
        {
            ent.Comp.GrantedActions.Remove(action.Value);
            _actions.RemoveAction(action.Value);
        }
    }
}
