using System.Diagnostics.CodeAnalysis;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Systems;

[ByRefEvent]
public readonly record struct ADTMegafaunaOpenFireEvent(EntityUid Target);

[ByRefEvent]
public record struct ADTMegafaunaDevourEvent(EntityUid Target, bool Handled = false);

[ByRefEvent]
public record struct ADTMegafaunaAfterMeleeHitEvent(EntityUid Target, bool Handled = false);

[ByRefEvent]
public record struct ADTMegafaunaAttemptOpenFireEvent(bool Cancelled = false);

[ByRefEvent]
public readonly record struct ADTMegafaunaTargetChangedEvent(EntityUid Target);

public sealed partial class MegafaunaSystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStationSystem _station = default!;

    private const string BusyKey = "MegafaunaBusy";

    private void InitializeCombat()
    {
        SubscribeLocalEvent<ADTMegafaunaAIComponent, MapInitEvent>(OnAIMapInit);
        SubscribeLocalEvent<ADTMegafaunaAIComponent, AttemptMeleeEvent>(OnAIAttemptMelee);
        SubscribeLocalEvent<ADTMegafaunaAIComponent, MeleeHitEvent>(OnAIMeleeHit);
    }

    private void OnAIMapInit(Entity<ADTMegafaunaAIComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.Actions)
        {
            _actions.AddAction(ent.Owner, action);
        }
    }

    private void OnAIAttemptMelee(Entity<ADTMegafaunaAIComponent> ent, ref AttemptMeleeEvent args)
    {
        if (IsRecovering(ent))
        {
            args.Cancelled = true;
            return;
        }

        if (!ent.Comp.MeleeOncePerDecision || HasComp<ActorComponent>(ent))
            return;

        if (!ent.Comp.MeleeReady)
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.MeleeReady = false;
    }

    private void OnAIMeleeHit(Entity<ADTMegafaunaAIComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.HandleMelee || !args.IsHit)
            return;

        EntityUid? victim = null;

        foreach (var hit in args.HitEntities)
        {
            if (hit == ent.Owner || !HasComp<MobStateComponent>(hit))
                continue;

            victim = hit;
            break;
        }

        if (victim is not { } target)
            return;

        Schedule(ent.Owner, TimeSpan.Zero, () => AfterMeleeHit(ent, target));
    }

    private void AfterMeleeHit(Entity<ADTMegafaunaAIComponent> ent, EntityUid target)
    {
        if (TerminatingOrDeleted(target))
            return;

        var ev = new ADTMegafaunaAfterMeleeHitEvent(target);
        RaiseLocalEvent(ent.Owner, ref ev);

        if (ev.Handled || TerminatingOrDeleted(target))
            return;

        if (_mobState.IsDead(target))
        {
            Devour(ent, target);
            return;
        }

        if (HasComp<ActorComponent>(ent.Owner) || !IsRangedReady(ent))
            return;

        if (IsOpenFireBlocked(ent.Owner))
            return;

        OpenFire(ent, target);
    }

    private void UpdateCombat(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTMegafaunaAIComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var ai, out var htn))
        {
            if (ai.DecideWhileInactive && !TryConsumeDecision(ai, now))
                continue;

            if (HasComp<ActorComponent>(uid))
                continue;

            if (!_npc.IsAwake(uid, htn))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (!ai.DecideWhileInactive && !TryConsumeDecision(ai, now))
                continue;

            if (IsOpenFireBlocked(uid))
                continue;

            if (now < ai.NextRangedAt)
                continue;

            if (!TryGetTarget(uid, out var found) || found is not { } target)
            {
                ai.LastTarget = null;
                continue;
            }

            if (_transform.GetGrid(uid) != _transform.GetGrid(target))
            {
                htn.Blackboard.Remove<EntityUid>(TargetKey);
                ai.LastTarget = null;
                continue;
            }

            if (ai.LastTarget != target)
            {
                ai.LastTarget = target;

                var changed = new ADTMegafaunaTargetChangedEvent(target);
                RaiseLocalEvent(uid, ref changed);
            }

            if (!ai.RangedWhenAdjacent && IsAdjacent(uid, target, ai.AdjacentOnlyOnGrid))
                continue;

            OpenFire((uid, ai), target);
        }
    }

    private bool TryConsumeDecision(ADTMegafaunaAIComponent ai, TimeSpan now)
    {
        if (now < ai.NextDecisionAt)
            return false;

        var jitter = ai.DecisionJitterMin;

        if (ai.DecisionJitterMax > ai.DecisionJitterMin)
            jitter += (ai.DecisionJitterMax - ai.DecisionJitterMin) * _random.NextDouble();

        ai.NextDecisionAt = now + ai.DecisionInterval + jitter;
        ai.MeleeReady = true;
        return true;
    }

    public bool IsOpenFireBlocked(EntityUid uid)
    {
        var ev = new ADTMegafaunaAttemptOpenFireEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Cancelled;
    }

    public void OpenFire(Entity<ADTMegafaunaAIComponent> ent, EntityUid target)
    {
        var ev = new ADTMegafaunaOpenFireEvent(target);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    public bool TryGetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        TryComp<ADTMegafaunaAIComponent>(uid, out var ai);

        if (ai?.ManualTarget is { } manual && !TerminatingOrDeleted(manual))
        {
            target = manual;
            return true;
        }

        if (TryComp<HTNComponent>(uid, out var htn)
            && htn.Blackboard.TryGetValue<EntityUid>(TargetKey, out var found, EntityManager)
            && !TerminatingOrDeleted(found))
        {
            target = found;
            return true;
        }

        if (ai?.TargetAggressor != false
            && TryComp<MegafaunaComponent>(uid, out var megafauna)
            && megafauna.Aggressor is { } aggressor
            && !TerminatingOrDeleted(aggressor))
        {
            target = aggressor;
            return true;
        }

        return false;
    }

    public void RefreshBusy(EntityUid uid)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        var ev = new ADTMegafaunaBusyCheckEvent();
        RaiseLocalEvent(uid, ref ev);

        if (ev.Busy)
            htn.Blackboard.SetValue(BusyKey, true);
        else
            htn.Blackboard.Remove<bool>(BusyKey);
    }

    public bool IsRangedReady(Entity<ADTMegafaunaAIComponent> ent)
    {
        return _timing.CurTime >= ent.Comp.NextRangedAt;
    }

    public void SetRangedCooldown(Entity<ADTMegafaunaAIComponent> ent, TimeSpan cooldown)
    {
        ent.Comp.NextRangedAt = _timing.CurTime + cooldown;
    }

    public bool IsRecovering(Entity<ADTMegafaunaAIComponent> ent)
    {
        return _timing.CurTime < ent.Comp.RecoveryUntil;
    }

    public void SetRecoveryTime(Entity<ADTMegafaunaAIComponent> ent, TimeSpan buffer)
    {
        var until = _timing.CurTime + buffer;
        ent.Comp.RecoveryUntil = until;
        ent.Comp.NextRangedAt = until;
    }

    public float CalculateAnger(Entity<ADTMegafaunaAIComponent> ent)
    {
        var damageTaken = (float) _damageable.GetTotalDamage(ent.Owner);
        ent.Comp.AngerModifier = Math.Clamp(damageTaken / ent.Comp.AngerDamageDivisor, 0f, ent.Comp.MaxAnger);
        return ent.Comp.AngerModifier;
    }

    public float GetMaxHealth(EntityUid uid)
    {
        if (!_thresholds.TryGetDeadThreshold(uid, out var threshold))
            return 0f;

        return (float) threshold.Value;
    }

    public float GetHealth(EntityUid uid)
    {
        return GetMaxHealth(uid) - (float) _damageable.GetTotalDamage(uid);
    }

    public void Devour(Entity<ADTMegafaunaAIComponent> ent, EntityUid target)
    {
        var ev = new ADTMegafaunaDevourEvent(target);
        RaiseLocalEvent(ent.Owner, ref ev);

        if (ev.Handled)
            return;

        _popup.PopupEntity(Loc.GetString("adt-megafauna-devour", ("boss", ent.Owner), ("target", target)), ent.Owner, PopupType.LargeCaution);

        var onStation = _station.GetOwningStation(ent.Owner) != null;
        if ((!onStation || HasComp<ActorComponent>(ent.Owner)) && _thresholds.TryGetDeadThreshold(target, out var maxHealth))
        {
            var group = _proto.Index(ent.Comp.DevourHealGroup);
            var heal = new DamageSpecifier(group, -maxHealth.Value * ent.Comp.DevourHealFraction);
            _damageable.TryChangeDamage(ent.Owner, heal, true, origin: ent.Owner);
        }

        if (_gibbing.Gib(target, true, ent.Owner).Count == 0)
            QueueDel(target);
    }

    public bool IsAdjacent(EntityUid uid, EntityUid target, bool onlyOnGrid = false)
    {
        var ours = _transform.GetMapCoordinates(uid);
        var theirs = _transform.GetMapCoordinates(target);

        if (ours.MapId != theirs.MapId || ours.MapId == MapId.Nullspace)
            return false;

        var xform = Transform(uid);

        if (xform.GridUid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var delta = _map.TileIndicesFor(gridUid, grid, theirs) - _map.TileIndicesFor(gridUid, grid, ours);
            return Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) <= 1;
        }

        if (onlyOnGrid)
            return false;

        return ADTMegafaunaGeometry.ChebyshevDistance(ours, theirs) <= 1.5f;
    }
}
