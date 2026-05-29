using System.Linq;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.Fluids.Components;
using Robust.Server.GameObjects;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumBloodWarpComponent, BubblegumBloodWarpActionEvent>(OnAction);
    }

    private void OnAction(Entity<BubblegumBloodWarpComponent> ent, ref BubblegumBloodWarpActionEvent args)
    {
        if (args.Handled)
            return;
        if (HasComp<BubblegumPendingWarpComponent>(ent))
            return;

        var bossCoords = _transform.GetMapCoordinates(ent);
        var targetCoords = _transform.ToMapCoordinates(args.Target);

        if (targetCoords.MapId == MapId.Nullspace || bossCoords.MapId != targetCoords.MapId)
            return;

        if ((bossCoords.Position - targetCoords.Position).Length() <= ent.Comp.AdjacentRange)
            return;

        var selfPools = _lookup.GetEntitiesInRange<PuddleComponent>(bossCoords, ent.Comp.SelfRange);
        if (selfPools.Count == 0)
            return;

        var outerPools = _lookup.GetEntitiesInRange<PuddleComponent>(targetCoords, ent.Comp.OuterRange);
        var innerPools = _lookup.GetEntitiesInRange<PuddleComponent>(targetCoords, ent.Comp.InnerRange);
        var innerSet = innerPools.Select(p => p.Owner).ToHashSet();
        var candidates = outerPools.Where(p => !innerSet.Contains(p.Owner)).ToList();
        if (candidates.Count == 0)
            return;

        args.Handled = true;

        var destinationPool = _random.Pick(candidates);
        var destination = _transform.GetMapCoordinates(destinationPool.Owner);

        if (TryComp<BubblegumComponent>(ent, out var bossComp))
        {
            var decoy = Spawn(bossComp.DecoyPrototype, bossCoords);
            EnsureComp<TimedDespawnComponent>(decoy).Lifetime = ent.Comp.SinkTime;
        }

        _audio.PlayPvs(ent.Comp.EnterSound, ent);

        var pending = EnsureComp<BubblegumPendingWarpComponent>(ent);
        pending.ExecuteAt = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SinkTime);
        pending.Destination = destination;
        Dirty(ent.Owner, pending);
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
