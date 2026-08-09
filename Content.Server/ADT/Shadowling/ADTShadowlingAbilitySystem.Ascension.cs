using Content.Server.Chat.Systems;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Gibbing;
using Content.Shared.DoAfter;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.ADT.Shadowling;

public sealed partial class ADTShadowlingAbilitySystem
{
    private void InitializeAscension()
    {
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingAscendEvent>(OnAscend);
        SubscribeLocalEvent<ADTShadowlingComponent, ADTShadowlingAscendDoAfterEvent>(OnAscendDoAfter);

        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTAscendantAnnihilateEvent>(OnAnnihilate);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTAscendantHypnosisEvent>(OnHypnosis);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTAscendantPhaseShiftEvent>(OnPhaseShift);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTAscendantLightningStormEvent>(OnLightningStorm);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTAscendantBroadcastEvent>(OnBroadcast);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTShadowlingBlackRecuperationEvent>(OnAscendantRecuperation);

        SubscribeLocalEvent<ADTAscendantPhasedComponent, ADTAscendantUnphaseEvent>(OnUnphase);

        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTShadowlingEmpowerDoAfterEvent>(OnAscendantEmpowerDoAfter);
        SubscribeLocalEvent<ADTAscendantShadowlingComponent, ADTShadowlingReviveDoAfterEvent>(OnAscendantReviveDoAfter);
    }

    private void OnAscend(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingAscendEvent args)
    {
        if (args.Handled || !CanUsePower(ent) || !ent.Comp.AscensionUnlocked)
            return;

        if (!TryComp<ADTShadowlingAscendActionComponent>(args.Action, out var ascend))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-ascend-begin-self"), ent, ent, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("shadowling-ascend-begin-others", ("user", ent.Owner)), ent, Filter.PvsExcept(ent.Owner), true, PopupType.LargeCaution);

        StartAscendStage(ent, args.Action, ascend, 0);
        args.Handled = true;
    }

    private void StartAscendStage(EntityUid user, EntityUid action, ADTShadowlingAscendActionComponent ascend, int stage)
    {
        var args = new DoAfterArgs(EntityManager, user, ascend.Stages[stage], new ADTShadowlingAscendDoAfterEvent(stage), user, used: action)
        {
            BreakOnDamage = false,
            BreakOnHandChange = false,
            BreakOnMove = false,
            CancelDuplicate = false,
            NeedHand = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnAscendDoAfter(Entity<ADTShadowlingComponent> ent, ref ADTShadowlingAscendDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used is not { } action)
            return;

        if (!TryComp<ADTShadowlingAscendActionComponent>(action, out var ascend))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("shadowling-ascend-stage", ("stage", args.Stage)), ent, ent, PopupType.LargeCaution);

        if (args.Stage + 1 < ascend.Stages.Count)
        {
            StartAscendStage(ent, action, ascend, args.Stage + 1);
            return;
        }

        FinishAscend(ent, ascend);
    }

    private void FinishAscend(Entity<ADTShadowlingComponent> ent, ADTShadowlingAscendActionComponent ascend)
    {
        _audio.PlayGlobal(ascend.Sound, Filter.Broadcast(), true);

        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, ascend.ShockwaveRange))
        {
            if (!HasComp<MobStateComponent>(nearby) || IsHiveMember(nearby))
                continue;

            _stun.TryKnockdown(nearby, ascend.ShockwaveKnockdown, true);
            _popup.PopupEntity(Loc.GetString("shadowling-ascend-shockwave"), nearby, nearby, PopupType.LargeCaution);
        }

        if (ascend.BreakAllLights)
            BreakEveryLight();

        var ascendant = _polymorph.PolymorphEntity(ent, ascend.Polymorph);
        if (ascendant == null)
            return;

        _chat.DispatchGlobalAnnouncement(Loc.GetString(ascend.Announcement),
            Loc.GetString(ascend.AnnouncementSender),
            playSound: true,
            colorOverride: Color.MediumPurple);

        var ev = new ADTShadowlingAscendedEvent(ascendant.Value);
        RaiseLocalEvent(ref ev);
    }

    private void BreakEveryLight()
    {
        var query = EntityQueryEnumerator<PoweredLightComponent>();
        while (query.MoveNext(out var uid, out var light))
        {
            _poweredLight.TryDestroyBulb(uid, light);
        }
    }

    private void OnAnnihilate(Entity<ADTAscendantShadowlingComponent> ent, ref ADTAscendantAnnihilateEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTAscendantAnnihilateActionComponent>(args.Action, out var power))
            return;

        if (!HasComp<MobStateComponent>(args.Target))
            return;

        if (HasComp<ADTShadowlingComponent>(args.Target) || HasComp<ADTAscendantShadowlingComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-annihilate-ally"), ent, ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString("shadowling-annihilate-cast", ("user", ent.Owner), ("target", args.Target)), ent, PopupType.LargeCaution);
        _audio.PlayPvs(power.Sound, args.Target);
        _gibbing.Gib(args.Target, user: ent);

        args.Handled = true;
    }

    private void OnHypnosis(Entity<ADTAscendantShadowlingComponent> ent, ref ADTAscendantHypnosisEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTAscendantHypnosisActionComponent>(args.Action, out var power))
            return;

        if (!CanEnthrall(ent, args.Target) || !_shadowling.TryMakeThrall(args.Target, ent))
            return;

        _audio.PlayPvs(power.Sound, args.Target);
        _popup.PopupEntity(Loc.GetString("shadowling-hypnosis-cast", ("target", args.Target)), ent, ent, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("shadowling-hypnosis-victim"), args.Target, args.Target, PopupType.LargeCaution);

        args.Handled = true;
    }

    private void OnPhaseShift(Entity<ADTAscendantShadowlingComponent> ent, ref ADTAscendantPhaseShiftEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTAscendantPhaseShiftActionComponent>(args.Action, out var power))
            return;

        _audio.PlayPvs(power.Sound, ent);
        _popup.PopupEntity(Loc.GetString("shadowling-phase-shift-enter", ("user", ent.Owner)), ent, PopupType.Medium);

        args.Handled = _polymorph.PolymorphEntity(ent, power.Polymorph) != null;
    }

    private void OnUnphase(Entity<ADTAscendantPhasedComponent> ent, ref ADTAscendantUnphaseEvent args)
    {
        if (args.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-phase-shift-exit"), ent, ent);
        _polymorph.Revert(ent.Owner);
        args.Handled = true;
    }

    private void OnLightningStorm(Entity<ADTAscendantShadowlingComponent> ent, ref ADTAscendantLightningStormEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTAscendantLightningStormActionComponent>(args.Action, out var power))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-lightning-cast", ("user", ent.Owner)), ent, PopupType.LargeCaution);
        _audio.PlayPvs(power.Sound, ent);

        foreach (var nearby in _lookup.GetEntitiesInRange(ent.Owner, power.Range))
        {
            if (!HasComp<MobStateComponent>(nearby) || IsHiveMember(nearby) || _mobState.IsDead(nearby))
                continue;

            _popup.PopupEntity(Loc.GetString("shadowling-lightning-hit"), nearby, nearby, PopupType.LargeCaution);
            _stun.TryKnockdown(nearby, power.Knockdown, true);
            _damageable.TryChangeDamage(nearby, power.Damage, true, origin: ent.Owner);
        }

        args.Handled = true;
    }

    private void OnBroadcast(Entity<ADTAscendantShadowlingComponent> ent, ref ADTAscendantBroadcastEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ADTAscendantBroadcastActionComponent>(args.Action, out var power))
            return;

        if (!_mind.TryGetMind(ent.Owner, out _, out var mind)
            || mind.UserId == null
            || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        var sound = power.Sound;
        var sender = Loc.GetString(power.Sender, ("name", Name(ent)));

        _dialog.OpenDialog(session,
            Loc.GetString("shadowling-broadcast-title"),
            Loc.GetString("shadowling-broadcast-prompt"),
            (string message) =>
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;

                if (message.Length > 256)
                {
                    _popup.PopupEntity(
                        Loc.GetString("shadowling-broadcast-too-long"),
                        ent,
                        ent,
                        PopupType.MediumCaution);

                    return;
                }

                _chat.DispatchGlobalAnnouncement(message, sender, playSound: false, colorOverride: Color.MediumPurple);
                _audio.PlayGlobal(sound, Filter.Broadcast(), true);
            });

        args.Handled = true;
    }

    private void OnAscendantRecuperation(Entity<ADTAscendantShadowlingComponent> ent, ref ADTShadowlingBlackRecuperationEvent args)
    {
        if (args.Handled || !HasComp<ADTShadowlingThrallComponent>(args.Target))
            return;

        if (!TryComp<ADTShadowlingBlackRecuperationActionComponent>(args.Action, out var power))
            return;

        if (_mobState.IsDead(args.Target))
        {
            StartRecuperation(ent, args.Target, power.ReviveTime, new ADTShadowlingReviveDoAfterEvent(), args.Action);
            args.Handled = true;
            return;
        }

        if (HasComp<ADTLesserShadowlingComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("shadowling-recuperation-already-empowered"), ent, ent);
            return;
        }

        StartRecuperation(ent, args.Target, power.EmpowerTime, new ADTShadowlingEmpowerDoAfterEvent(), args.Action);
        args.Handled = true;
    }

    private void OnAscendantEmpowerDoAfter(Entity<ADTAscendantShadowlingComponent> ent, ref ADTShadowlingEmpowerDoAfterEvent args)
    {
        DoEmpower(ref args);
    }

    private void OnAscendantReviveDoAfter(Entity<ADTAscendantShadowlingComponent> ent, ref ADTShadowlingReviveDoAfterEvent args)
    {
        DoRevive(ref args);
    }

}

[ByRefEvent]
public readonly record struct ADTShadowlingAscendedEvent(EntityUid Ascendant);
