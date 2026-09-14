using Content.Shared.ADT.NightVision;
using Content.Shared.ADT.Shadowling;
using Content.Shared.DoAfter;
using Content.Shared.Mindshield.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private void InitializeThrall()
    {
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingEnthrallEvent>(OnEnthrall);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingEnthrallDoAfterEvent>(OnEnthrallDoAfter);

        SubscribeLocalEvent<ADTShadowlingThrallComponent, ADTShadowlingGuiseEvent>(OnGuise);
        SubscribeLocalEvent<ADTShadowlingThrallComponent, ADTShadowlingDarksightEvent>(OnDarksight);
    }

    private void OnEnthrall(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingEnthrallEvent args)
    {
        if (args.Handled || !CanUsePower(ent))
            return;

        var target = args.Target;

        if (!CanEnthrall(ent, target))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-begin-self"), ent, ent);
        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-begin-target", ("user", ent.Owner)), target, target, PopupType.LargeCaution);

        StartEnthrallStage(ent, target, 1);
        args.Handled = true;
    }

    private void StartEnthrallStage(Entity<ADTShadowlingComponent> ent, EntityUid target, int stage)
    {
        var args = new DoAfterArgs(EntityManager, ent, ent.Comp.EnthrallStageTime, new ADTShadowlingEnthrallDoAfterEvent(stage), ent, target)
        {
            BreakOnDamage = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            CancelDuplicate = false,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnEnthrallDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingEnthrallDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-interrupted-self"), ent, ent, PopupType.MediumCaution);
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-interrupted-target", ("user", ent.Owner)), target, target);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-stage", ("stage", args.Stage)), ent, ent);

        if (args.Stage == ent.Comp.EnthrallStages - 1)
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-collapse"), target, target, PopupType.LargeCaution);
            _stun.TryKnockdown(target, ent.Comp.EnthrallKnockdown, true);
        }

        if (args.Stage < ent.Comp.EnthrallStages)
        {
            StartEnthrallStage(ent, target, args.Stage + 1);
            return;
        }

        if (!CanEnthrall(ent, target) || !_shadowling.TryMakeThrall(target, ent))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-enthrall-invalid"), ent, ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.EnthrallSound, target);
        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-success", ("target", target)), ent, ent, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("shadowling-enthrall-converted"), target, target, PopupType.LargeCaution);
    }

    public bool CanEnthrall(EntityUid user, EntityUid target, bool quiet = false)
    {
        if (IsHiveMember(target))
            return false;

        if (!HasComp<HumanoidProfileComponent>(target)
            || !HasComp<MobStateComponent>(target)
            || _mobState.IsDead(target)
            || !HasPlayer(target))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("shadowling-enthrall-invalid"), user, user);

            return false;
        }

        if (HasComp<MindShieldComponent>(target))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("shadowling-enthrall-mindshield"), user, user, PopupType.MediumCaution);

            return false;
        }

        return true;
    }

    private bool HasPlayer(EntityUid target)
    {
    #if DEBUG
        return true;
    #else
        return _mind.TryGetMind(target, out _, out var mind)
            && mind.UserId != null
            && _player.TryGetSessionById(mind.UserId, out _);
    #endif
    }

    private void OnGuise(Entity<ADTShadowlingThrallComponent> ent, ref ADTShadowlingGuiseEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTShadowlingGuiseActionComponent>(args.Action, out var guise))
            return;

        _audio.PlayPvs(guise.Sound, ent);
        _popup.PopupEntity(Loc.GetString("shadowling-guise-enter"), ent, ent);

        var stealth = EnsureComp<StealthComponent>(ent);
        _stealth.SetVisibility(ent, guise.Visibility, stealth);

        var timer = EnsureComp<ADTShadowlingGuiseComponent>(ent);
        timer.EndTime = _timing.CurTime + guise.Duration;
        Dirty(ent.Owner, timer);

        args.Handled = true;
    }

    private void UpdateGuise()
    {
        var query = EntityQueryEnumerator<ADTShadowlingGuiseComponent>();
        while (query.MoveNext(out var uid, out var guise))
        {
            if (guise.EndTime > _timing.CurTime)
                continue;

            if (TryComp<StealthComponent>(uid, out var stealth))
            {
                _stealth.SetVisibility(uid, 1f, stealth);
                RemComp<StealthComponent>(uid);
            }

            _popup.PopupEntity(Loc.GetString("shadowling-guise-exit"), uid, uid);
            RemComp<ADTShadowlingGuiseComponent>(uid);
        }
    }

    private void OnDarksight(Entity<ADTShadowlingThrallComponent> ent, ref ADTShadowlingDarksightEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<NightVisionComponent>(ent))
        {
            var vision = EnsureComp<NightVisionComponent>(ent);
            _nightVision.SetActive((ent.Owner, vision), false);
        }

        _nightVision.Toggle(ent.Owner);
        args.Handled = true;
    }
}
