//

using System.Linq;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class LabyrinthPortalSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;
    [Dependency] private readonly EntityQuery<MindComponent> _mindQuery = default!;
    [Dependency] private readonly EntityQuery<HereticComponent> _hereticQuery = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private TimeSpan _nextSpawn;
    private readonly TimeSpan _spawnDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<Entity<PhysicsComponent>> _lookupPhysics = new();

    private const int CollisionMask = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable |
                                             CollisionGroup.LowImpassable | CollisionGroup.MidImpassable);

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextSpawn)
            return;

        _nextSpawn = now + _spawnDelay;

        var queue = EntityQueryEnumerator<LabyrinthPortalComponent, TransformComponent>();
        while (queue.MoveNext(out _, out var portal, out var xform))
        {
            if (portal.Paused)
                continue;

            if (!_random.Prob(portal.SpawnChance))
                continue;

            portal.SpawnedMobs = portal.SpawnedMobs.Where(Exists).ToList();

            if (portal.SpawnedMobs.Count >= portal.MaxMobs)
                continue;

            portal.SpawnChance = MathF.Max(portal.MinSpawnChance, portal.SpawnChance - portal.ChanceReduction);

            _lookupPhysics.Clear();
            _look.GetEntitiesInRange(xform.Coordinates, 1.5f, _lookupPhysics, LookupFlags.Static);
            foreach (var ent in _lookupPhysics)
            {
                if (!ent.Comp.Hard)
                    continue;

                if ((ent.Comp.CollisionLayer & CollisionMask) == 0)
                    continue;

                QueueDel(ent);
            }

            var table = _proto.Index(portal.ToSpawn);
            var mob = table.Pick(_random);
            var spawned = Spawn(mob, xform.Coordinates);
            portal.SpawnedMobs.Add(spawned);
        }
    }
}
