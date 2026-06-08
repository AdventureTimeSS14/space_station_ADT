using System.Linq;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Fluids.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum.Abilities;

public sealed class BubblegumBloodWarpSystem : EntitySystem
{
    [Dependency] private readonly BubblegumSystem _bubblegum = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MobStateComponent>> _mobBuffer = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumBloodWarpComponent, BubblegumBloodWarpActionEvent>(OnAction);
    }

    private void OnAction(Entity<BubblegumBloodWarpComponent> ent, ref BubblegumBloodWarpActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindNearestHostile(ent.Owner, ent.Comp.TargetSearchRange, out var target))
            return;

        if (TryBloodWarp(ent, _transform.GetMapCoordinates(target)))
            args.Handled = true;
    }

    public bool TryBloodWarp(Entity<BubblegumBloodWarpComponent> ent, MapCoordinates targetCoords)
    {
        if (HasComp<BubblegumPendingWarpComponent>(ent))
            return false;

        var bossCoords = _transform.GetMapCoordinates(ent);
        if (targetCoords.MapId == MapId.Nullspace || bossCoords.MapId != targetCoords.MapId)
            return false;

        // if(Adjacent(target))
        if ((bossCoords.Position - targetCoords.Position).Length() <= ent.Comp.AdjacentRange)
            return false;

        // can_jaunt = get_pools(get_turf(src), 1)
        var selfPools = _lookup.GetEntitiesInRange<PuddleComponent>(bossCoords, ent.Comp.SelfRange);
        if (selfPools.Count == 0)
            return false;

        // pools = get_pools(target, 5) - get_pools(target, 4)
        var outerPools = _lookup.GetEntitiesInRange<PuddleComponent>(targetCoords, ent.Comp.OuterRange);
        if (outerPools.Count == 0)
            return false;

        var innerPools = _lookup.GetEntitiesInRange<PuddleComponent>(targetCoords, ent.Comp.InnerRange);
        var innerSet = innerPools.Select(p => p.Owner).ToHashSet();
        var ringCandidates = outerPools.Where(p => !innerSet.Contains(p.Owner)).ToList();
        var candidates = ringCandidates.Count > 0
            ? ringCandidates
            : [.. outerPools];

        var destinationPool = _random.Pick(candidates);
        var destination = _transform.GetMapCoordinates(destinationPool.Owner);

        if (TryComp<BubblegumComponent>(ent, out var bossComp))
        {
            var decoy = Spawn(bossComp.DecoyPrototype, bossCoords);
            EnsureComp<TimedDespawnComponent>(decoy).Lifetime = ent.Comp.SinkTime;
            EnsureComp<BubblegumMinionComponent>(decoy).Summoner = ent.Owner;
        }

        _audio.PlayPvs(ent.Comp.EnterSound, ent);

        var pending = EnsureComp<BubblegumPendingWarpComponent>(ent);
        pending.ExecuteAt = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SinkTime);
        pending.Destination = destination;
        Dirty(ent.Owner, pending);
        return true;
    }

    private bool TryFindNearestHostile(EntityUid boss, float range, out EntityUid target)
    {
        target = default;
        _mobBuffer.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(boss), range, _mobBuffer);

        var bestDistance = float.MaxValue;
        var bossPos = _transform.GetMapCoordinates(boss).Position;
        foreach (var mob in _mobBuffer)
        {
            if (mob.Owner == boss)
                continue;

            if (_mobState.IsDead(mob))
                continue;

            if (_npcFaction.IsEntityFriendly(boss, mob.Owner))
                continue;

            var dist = (_transform.GetMapCoordinates(mob.Owner).Position - bossPos).LengthSquared();
            if (dist >= bestDistance)
                continue;

            bestDistance = dist;
            target = mob.Owner;
        }

        return target != default;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BubblegumPendingWarpComponent, BubblegumBloodWarpComponent>();
        while (query.MoveNext(out var uid, out var pending, out var warp))
        {
            if (now < pending.ExecuteAt)
                continue;

            _transform.SetMapCoordinates(uid, pending.Destination);
            _audio.PlayPvs(warp.ExitSound, uid);

            if (TryComp<BubblegumComponent>(uid, out var boss))
                _bubblegum.TryEnterEnrage((uid, boss));

            RemCompDeferred<BubblegumPendingWarpComponent>(uid);
        }
    }
}
