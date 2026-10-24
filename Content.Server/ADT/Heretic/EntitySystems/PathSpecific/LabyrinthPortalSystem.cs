//

using System.Linq;
using System.Numerics;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
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
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

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

            portal.SpawnedMobs = portal.SpawnedMobs.Where(e => Exists(e) && !_mobState.IsDead(e)).ToList();

            EntityUid? hereticBody = null;
            if (portal.HereticMind != null)
            {
                if (_mindQuery.TryComp(portal.HereticMind.Value, out var mind) && mind.CurrentEntity is { } body && Exists(body))
                    hereticBody = body;
                else if (Exists(portal.HereticMind.Value))
                    hereticBody = portal.HereticMind.Value;
            }

            if (hereticBody != null && portal.SpawnedMobs.Count > 0)
            {
                var amount = -0.5f * portal.SpawnedMobs.Count;
                var heal = new DamageSpecifier
                {
                    DamageDict =
                    {
                        { "Blunt", amount },
                        { "Slash", amount },
                        { "Piercing", amount },
                        { "Heat", amount },
                        { "Cold", amount },
                        { "Shock", amount },
                        { "Asphyxiation", amount },
                        { "Bloodloss", amount },
                        { "Caustic", amount },
                        { "Poison", amount },
                        { "Radiation", amount },
                        { "Cellular", amount },
                        { "Holy", amount },
                    }
                };
                _damage.TryChangeDamage((hereticBody.Value, null), heal, true, false);
            }

            if (!_random.Prob(portal.SpawnChance))
                continue;

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

            if (hereticBody != null)
            {
                var minion = EnsureComp<HereticMinionComponent>(spawned);
                minion.BoundHeretic = hereticBody;
                Dirty(spawned, minion);
                _npc.SetBlackboard(spawned, NPCBlackboard.FollowTarget, new EntityCoordinates(hereticBody.Value, Vector2.Zero));
            }
        }
    }
}
