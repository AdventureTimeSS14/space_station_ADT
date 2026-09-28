using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Stack;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
<<<<<<< ours
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
||||||| base
using JetBrains.Annotations;
using Robust.Shared.Map;
=======
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
>>>>>>> theirs
using Robust.Shared.Random;

<<<<<<< ours
// TODO: This whole system is a mess. A lot of this should be marked obsolete.
// TODO: It should probably use interfaces with entity tables *if* more than one component is needed.
// TODO: Remove the TransformSystem Dependency when engine SpawnAtPosition EntityCoordinates override is fixed.
namespace Content.Server.Spawners.EntitySystems
||||||| base
namespace Content.Server.Spawners.EntitySystems
=======
namespace Content.Server.Spawners.EntitySystems;

// TODO: This whole system is a mess. A lot of this should be marked obsolete.
// TODO: It should probably use interfaces with entity tables *if* more than one component is needed.
// TODO: Remove the TransformSystem Dependency when engine SpawnAtPosition EntityCoordinates override is fixed.
public sealed partial class ConditionalSpawnerSystem : EntitySystem
>>>>>>> theirs
{
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private TransformSystem _xform = default!;

    [SubscribeLocalEvent]
    private void OnCondSpawnMapInit(Entity<ConditionalSpawnerComponent> ent, ref MapInitEvent args)
    {
<<<<<<< ours
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly EntityTableSystem _entityTable = default!;
        [Dependency] private readonly TransformSystem _xform = default!;
||||||| base
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly EntityTableSystem _entityTable = default!;
=======
        TrySpawn(ent, ent);
    }
>>>>>>> theirs

    [SubscribeLocalEvent]
    private void OnRandSpawnMapInit(Entity<RandomSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent, ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn)
            QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnEntityTableSpawnMapInit(Entity<EntityTableSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn && !TerminatingOrDeleted(ent) && Exists(ent))
            QueueDel(ent);
    }

    [SubscribeLocalEvent]
    private void OnRuleStarted(ref GameRuleStartedEvent args)
    {
        var query = EntityQueryEnumerator<ConditionalSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            RuleStarted(uid, spawner, args);
        }
    }

    public void RuleStarted(EntityUid uid, ConditionalSpawnerComponent component, GameRuleStartedEvent obj)
    {
        if (component.GameRules.Contains(obj.RuleId))
            Spawn(uid, component);
    }

    private void TrySpawn(EntityUid uid, ConditionalSpawnerComponent component)
    {
        if (component.GameRules.Count == 0)
        {
            Spawn(uid, component);
            return;
        }

        foreach (var rule in component.GameRules)
        {
            if (!_ticker.IsGameRuleActive(rule))
                continue;
            Spawn(uid, component);
            return;
        }
    }

    private void Spawn(EntityUid uid, ConditionalSpawnerComponent component)
    {
        if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
            return;

        if (component.Prototypes.Count == 0)
        {
            Log.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {ToPrettyString(uid)}");
            return;
        }

        if (Deleted(uid))
            return;

        var xform = Transform(uid);
        var coords = _xform.GetMapCoordinates(uid, xform);
        var rotation = _xform.GetWorldRotation(xform);

        var toSpawn = _robustRandom.Pick(component.Prototypes);
        Spawn(toSpawn, coords, rotation: rotation);
    }

    private void Spawn(EntityUid uid, RandomSpawnerComponent component)
    {
        if (Deleted(uid))
            return;

        if (GetPrototype((uid, component)) is not { } proto)
            return;

        var xform = Transform(uid);
        var coords = _xform.GetMapCoordinates(uid, xform);
        var coordinates = GetRandomOffset(coords, component.Offset);
        var rotation = _xform.GetWorldRotation(xform);

        Spawn(_robustRandom.Pick(component.Prototypes), coordinates, rotation: rotation);
    }

    private void Spawn(Entity<EntityTableSpawnerComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;

        var xform = Transform(ent);
        var coords = _xform.GetMapCoordinates(ent, xform);
        var rotation = _xform.GetWorldRotation(xform);

        EntityTableSpawnerComponent comp = ent;
        var spawns = _entityTable.GetSpawns(comp.Table);
        if (comp.AutoStack)
        {
            SpawnStackedWhenPossible(spawns, ent, coords, comp.Offset, rotation);
        }
        else
        {
            SpawnAtRandomOffset(spawns, coords, comp.Offset, rotation);
        }
    }

    private void SpawnStackedWhenPossible(IEnumerable<EntProtoId> spawns,
        Entity<EntityTableSpawnerComponent> ent,
        MapCoordinates coords,
        float offset,
        Angle rotation)
    {
        Dictionary<ProtoId<StackPrototype>, (EntProtoId Proto, int Count)> prototypeStacks = new();
        ValueList<EntProtoId> nonStackable = [];
        foreach (var protoId in spawns)
        {
            var prototype = ProtoMan.Index(protoId);
            if (!prototype.TryComp<StackComponent>(out var stack, Factory))
            {
                nonStackable.Add(protoId);
                continue;
            }

<<<<<<< ours
            if (Deleted(uid))
                return;

            var xform = Transform(uid);
            var coords = _xform.GetMapCoordinates(uid, xform);
            var rotation = _xform.GetWorldRotation(xform);

            Spawn(_robustRandom.Pick(component.Prototypes), coords, rotation: rotation);
||||||| base
            if (!Deleted(uid))
                Spawn(_robustRandom.Pick(component.Prototypes), Transform(uid).Coordinates);
=======
            prototypeStacks[stack.StackTypeId] = prototypeStacks.TryGetValue(stack.StackTypeId, out var found)
                ? (protoId, found.Count + 1)
                : (protoId, 1);
>>>>>>> theirs
        }

        SpawnAtRandomOffset(nonStackable, coords, offset, rotation);

        foreach (var (protoId, count) in prototypeStacks.Values)
        {
<<<<<<< ours
            if (Deleted(uid))
                return;
||||||| base
            if (component.RarePrototypes.Count > 0 && (component.RareChance == 1.0f || _robustRandom.Prob(component.RareChance)))
            {
                Spawn(_robustRandom.Pick(component.RarePrototypes), Transform(uid).Coordinates);
                return;
            }
=======
            var trueCoords = GetRandomOffset(coords, offset);
            var entCoordinates = _xform.ToCoordinates((ent, null), trueCoords);
            _stack.SpawnMultipleAtPosition(protoId, count, entCoordinates);
        }
    }
>>>>>>> theirs

<<<<<<< ours
            if (GetPrototype((uid, component)) is not { } proto)
                return;
||||||| base
            if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
                return;
=======
    private void SpawnAtRandomOffset(IEnumerable<EntProtoId> spawns, MapCoordinates coords, float offset, Angle rotation)
    {
        foreach (var proto in spawns)
        {
            SpawnAtRandomOffset(proto, coords, offset, rotation);
        }
    }
>>>>>>> theirs

<<<<<<< ours
            var offset = component.Offset;
            var vOffset = _robustRandom.NextVector2Box(-offset, offset);

            var xform = Transform(uid);
            var coords = _xform.GetMapCoordinates(uid, xform).Offset(vOffset);
            var rotation = _xform.GetWorldRotation(xform);

            Spawn(proto, coords, rotation: rotation);
        }

        private EntProtoId? GetPrototype(Entity<RandomSpawnerComponent> spawner)
        {
            if (GetPrototypes(spawner) is not { } list)
                return null;

            return _robustRandom.Pick(list);
        }

        private List<EntProtoId>? GetPrototypes(Entity<RandomSpawnerComponent> spawner)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (spawner.Comp.RarePrototypes.Count > 0 &&
                (spawner.Comp.RareChance == 1.0f || _robustRandom.Prob(spawner.Comp.RareChance)))
            {
                return spawner.Comp.RarePrototypes;
            }
||||||| base
            if (component.Prototypes.Count == 0)
            {
                Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(uid)}");
                return;
            }
=======
    private EntityUid SpawnAtRandomOffset(EntProtoId proto, MapCoordinates coords, float offset, Angle rotation)
    {
        var trueCoords = GetRandomOffset(coords, offset);
>>>>>>> theirs

<<<<<<< ours
            if (spawner.Comp.Prototypes.Count == 0)
            {
                Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(spawner)}");
                return null;
            }
||||||| base
            if (Deleted(uid))
                return;

            var offset = component.Offset;
            var xOffset = _robustRandom.NextFloat(-offset, offset);
            var yOffset = _robustRandom.NextFloat(-offset, offset);
=======
        return Spawn(proto, trueCoords, rotation: rotation);
    }

    private EntProtoId? GetPrototype(Entity<RandomSpawnerComponent> spawner)
    {
        if (GetPrototypes(spawner) is not { } list)
            return null;
>>>>>>> theirs

<<<<<<< ours
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (spawner.Comp.Chance == 1.0f || !_robustRandom.Prob(spawner.Comp.Chance))
            {
                return spawner.Comp.Prototypes;
            }
||||||| base
            var coordinates = Transform(uid).Coordinates.Offset(new Vector2(xOffset, yOffset));
=======
        return _robustRandom.Pick(list);
    }
>>>>>>> theirs

<<<<<<< ours
            return null;
||||||| base
            Spawn(_robustRandom.Pick(component.Prototypes), coordinates);
=======
    private List<EntProtoId>? GetPrototypes(Entity<RandomSpawnerComponent> spawner)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (spawner.Comp.RarePrototypes.Count > 0 &&
            (spawner.Comp.RareChance == 1.0f || _robustRandom.Prob(spawner.Comp.RareChance)))
        {
            return spawner.Comp.RarePrototypes;
>>>>>>> theirs
        }

        if (spawner.Comp.Prototypes.Count == 0)
        {
            Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(spawner)}");
            return null;
        }

<<<<<<< ours
            var xform = Transform(ent);
            var coords = _xform.GetMapCoordinates(ent, xform);
            var rotation = _xform.GetWorldRotation(xform);
            var offset = ent.Comp.Offset;
||||||| base
            var coords = Transform(ent).Coordinates;
            var offset = ent.Comp.Offset;
=======
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (spawner.Comp.Chance == 1.0f || _robustRandom.Prob(spawner.Comp.Chance))
        {
            return spawner.Comp.Prototypes;
        }
>>>>>>> theirs

<<<<<<< ours
            var spawns = _entityTable.GetSpawns(ent.Comp.Table);
            foreach (var proto in spawns)
            {
                var vOffset = _robustRandom.NextVector2(-offset, offset);
                var trueCoords = coords.Offset(vOffset);
||||||| base
            var spawns = _entityTable.GetSpawns(ent.Comp.Table);
            foreach (var proto in spawns)
            {
                var xOffset = _robustRandom.NextFloat(-offset, offset);
                var yOffset = _robustRandom.NextFloat(-offset, offset);
                var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));
=======
        return null;
    }
>>>>>>> theirs

<<<<<<< ours
                Spawn(proto, trueCoords, rotation: rotation);
            }
        }
||||||| base
                SpawnAttachedTo(proto, trueCoords);
            }
        }
=======
    private MapCoordinates GetRandomOffset(MapCoordinates coords, float offset)
    {
        var vOffset = _robustRandom.NextVector2Box(offset, offset);
        return coords.Offset(vOffset);
>>>>>>> theirs
    }
}