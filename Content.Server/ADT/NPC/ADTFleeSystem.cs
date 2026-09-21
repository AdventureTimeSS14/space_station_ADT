using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.NPC;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed class ADTFleeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSteeringSystem _steering = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        var query = EntityQueryEnumerator<ADTFleeComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (_mobState.IsDead(uid) || HasComp<ActorComponent>(uid))
                continue;

            UpdateFlee((uid, comp), htn);
        }
    }

    private void UpdateFlee(Entity<ADTFleeComponent> ent, HTNComponent htn)
    {
        var blackboard = htn.Blackboard;

        if (GetFleeCoordinates(ent, blackboard) is not { } coordinates)
        {
            blackboard.Remove<EntityCoordinates>(ent.Comp.CoordinatesKey);
            return;
        }

        blackboard.SetValue(ent.Comp.CoordinatesKey, coordinates);

        if (!TryComp<NPCSteeringComponent>(ent.Owner, out var steering))
            return;

        if (steering.Coordinates.TryDistance(EntityManager, coordinates, out var moved)
            && moved < ent.Comp.UpdateThreshold)
        {
            return;
        }

        _steering.Register(ent.Owner, coordinates, steering);
    }

    private EntityCoordinates? GetFleeCoordinates(Entity<ADTFleeComponent> ent, NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(ent.Comp.TargetKey, out var target, EntityManager))
            return null;

        if (!target.IsValid() || TerminatingOrDeleted(target) || _mobState.IsDead(target))
            return null;

        var xform = Transform(ent.Owner);

        if (!xform.ParentUid.IsValid())
            return null;

        var ourPos = _transform.GetMapCoordinates(ent.Owner, xform);
        var targetPos = _transform.GetMapCoordinates(target);

        if (ourPos.MapId == MapId.Nullspace || ourPos.MapId != targetPos.MapId)
            return null;

        var away = ourPos.Position - targetPos.Position;
        var distance = away.Length();

        var range = blackboard.TryGetValue<float>(ent.Comp.RangeKey, out var fleeRange, EntityManager)
            ? fleeRange
            : ent.Comp.DefaultRange;

        if (distance > range)
            return null;

        var direction = distance > 0.01f
            ? away / distance
            : _random.NextAngle().ToVec();

        var destination = new MapCoordinates(ourPos.Position + direction * ent.Comp.Distance, ourPos.MapId);

        return _transform.ToCoordinates(xform.ParentUid, destination);
    }
}
