using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Fluids.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumBloodAttackSystem : EntitySystem
{
    [Dependency] private readonly BubblegumSystem _bubblegum = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobBuffer = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var attemptQuery = EntityQueryEnumerator<BubblegumBloodAttackComponent, BubblegumComponent>();
        while (attemptQuery.MoveNext(out var uid, out var comp, out _))
        {
            if (now < comp.NextAttemptAt)
                continue;

            comp.NextAttemptAt = now + comp.AttemptInterval;
            TryStartBloodAttack((uid, comp));
        }

        var hitsQuery = EntityQueryEnumerator<BubblegumPendingBloodHitsComponent, BubblegumBloodAttackComponent>();
        while (hitsQuery.MoveNext(out var uid, out var pending, out var comp))
        {
            for (var i = pending.Queue.Count - 1; i >= 0; i--)
            {
                var hit = pending.Queue[i];
                if (now < hit.ExecuteAt)
                    continue;

                if (hit.IsGrab)
                    ResolveBloodGrab(uid, comp, hit);
                else
                    ResolveBloodSmack(uid, comp, hit);

                pending.Queue.RemoveAt(i);
            }

            if (pending.Queue.Count == 0)
                RemCompDeferred<BubblegumPendingBloodHitsComponent>(uid);
        }
    }

    private void TryStartBloodAttack(Entity<BubblegumBloodAttackComponent> ent)
    {
        if (_mobState.IsDead(ent))
            return;

        List<(EntityUid Target, MapCoordinates PuddleCoords)> candidates = [];
        _mobBuffer.Clear();

        var bossCoords = _transform.GetMapCoordinates(ent);
        _lookup.GetEntitiesInRange(bossCoords, ent.Comp.SearchRange, _mobBuffer);
        foreach (var mob in _mobBuffer)
        {
            if (mob.Owner == ent.Owner)
                continue;

            if (_mobState.IsDead(mob))
                continue;

            if (_npcFaction.IsEntityFriendly(ent.Owner, mob.Owner))
                continue;

            var mobCoords = _transform.GetMapCoordinates(mob);
            var pools = _lookup.GetEntitiesInRange<PuddleComponent>(mobCoords, ent.Comp.PuddleCheckRange);
            if (pools.Count == 0)
                continue;

            var puddleCoords = _transform.GetMapCoordinates(_random.Pick(pools).Owner);
            candidates.Add((mob, puddleCoords));
            if (candidates.Count >= 2)
                break;
        }

        if (candidates.Count == 0)
            return;

        var enraged = TryComp<BubblegumComponent>(ent, out var bossComp)
                      && _bubblegum.IsEnraged((ent.Owner, bossComp));
        var damage = enraged ? ent.Comp.SmackDamageEnraged : ent.Comp.SmackDamage;
        var handedness = _random.Next(2) == 0;

        foreach (var (target, puddleCoords) in candidates)
        {
            var isIncapacitated = _mobState.IsIncapacitated(target);
            var doGrab = isIncapacitated || _random.Prob(ent.Comp.GrabConsciousChance);

            SpawnHandTelegraph(ent.Comp, puddleCoords, handedness, doGrab);
            QueueHit(ent.Owner, target, puddleCoords, damage, doGrab,
                doGrab ? ent.Comp.GrabHitDelay : ent.Comp.SmackHitDelay);

            handedness = !handedness;
        }
    }

    private void SpawnHandTelegraph(BubblegumBloodAttackComponent comp, MapCoordinates coords, bool right, bool grab)
    {
        if (grab)
        {
            Spawn(right ? comp.RightPawProto : comp.LeftPawProto, coords, rotation: Angle.Zero);
            Spawn(right ? comp.RightThumbProto : comp.LeftThumbProto, coords, rotation: Angle.Zero);
            return;
        }

        Spawn(right ? comp.RightSmackProto : comp.LeftSmackProto, coords, rotation: Angle.Zero);
    }

    private void QueueHit(EntityUid boss, EntityUid target, MapCoordinates at, float damage, bool isGrab, TimeSpan delay)
    {
        var pending = EnsureComp<BubblegumPendingBloodHitsComponent>(boss);
        pending.Queue.Add(new PendingBloodHit
        {
            ExecuteAt = _timing.CurTime + delay,
            Target = target,
            At = at,
            Damage = damage,
            IsGrab = isGrab
        });
        Dirty(boss, pending);
    }

    private void ResolveBloodSmack(EntityUid boss, BubblegumBloodAttackComponent comp, PendingBloodHit hit)
    {
        if (TerminatingOrDeleted(hit.Target))
            return;

        if (_mobState.IsDead(hit.Target))
            return;

        if (_npcFaction.IsEntityFriendly(boss, hit.Target))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", hit.Damage);
        _damageable.TryChangeDamage(hit.Target, damage, false, origin: boss);
        _audio.PlayPvs(comp.HitSound, hit.Target);
    }

    private void ResolveBloodGrab(EntityUid boss, BubblegumBloodAttackComponent comp, PendingBloodHit hit)
    {
        if (TerminatingOrDeleted(hit.Target))
            return;

        if (_mobState.IsAlive(hit.Target))
            return;

        if (_npcFaction.IsEntityFriendly(boss, hit.Target))
            return;

        _audio.PlayPvs(comp.GrabSound, hit.Target);

        var bossCoords = _transform.GetMapCoordinates(boss);
        _transform.SetMapCoordinates(hit.Target, bossCoords);

        var devour = EnsureComp<BubblegumPendingDevourComponent>(boss);
        devour.Target = hit.Target;
        devour.ExecuteAt = _timing.CurTime + comp.DevourDelay;
        Dirty(boss, devour);
    }
}
