using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.GameTicking;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.ADT.Lavaland.Dungeon;

public sealed class ADTDungeonGatewaySystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private LinkedEntitySystem _link = default!;
    [Dependency] private MapLoaderSystem _mapLoader = default!;
    [Dependency] private SharedMapSystem _map = default!;

    private readonly Dictionary<string, EntityUid> _loaded = new();
    private readonly HashSet<string> _failed = new();
    private readonly Queue<EntityUid> _pending = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDungeonGatewayComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnMapInit(Entity<ADTDungeonGatewayComponent> ent, ref MapInitEvent args)
    {
        _pending.Enqueue(ent);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _loaded.Clear();
        _failed.Clear();
        _pending.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_pending.TryDequeue(out var uid))
        {
            if (TerminatingOrDeleted(uid) || !TryComp<ADTDungeonGatewayComponent>(uid, out var gateway))
                continue;

            TryLinkGateway((uid, gateway));
        }
    }

    private void TryLinkGateway(Entity<ADTDungeonGatewayComponent> ent)
    {
        if (ent.Comp.Linked)
            return;

        var key = ent.Comp.Key;

        if (!_loaded.TryGetValue(key, out var dungeonMap) || TerminatingOrDeleted(dungeonMap))
        {
            if (_failed.Contains(key) || !TryLoadDungeon(ent, out dungeonMap))
                return;
        }

        var dungeonMapId = Comp<MapComponent>(dungeonMap).MapId;
        var query = EntityQueryEnumerator<ADTDungeonGatewayExitComponent, TransformComponent>();
        var linked = false;

        while (query.MoveNext(out var exitUid, out var exit, out var xform))
        {
            if (exit.Key != key || xform.MapID != dungeonMapId)
                continue;

            _link.TryLink(ent, exitUid);
            linked = true;
        }

        if (!linked)
            return;

        ent.Comp.Linked = true;
    }

    private bool TryLoadDungeon(Entity<ADTDungeonGatewayComponent> ent, out EntityUid dungeonMap)
    {
        dungeonMap = default;

        var opts = new DeserializationOptions
        {
            InitializeMaps = true,
        };

        if (!_mapLoader.TryLoadMap(ent.Comp.Map, out var map, out _, opts))
        {
            _failed.Add(ent.Comp.Key);
            return false;
        }

        dungeonMap = map.Value.Owner;
        _map.SetPaused(map.Value.Comp.MapId, false);

        if (ent.Comp.CopyAtmosphere
            && Transform(ent).MapUid is { } sourceMap
            && TryComp<MapAtmosphereComponent>(sourceMap, out var sourceAtmos))
        {
            _atmos.SetMapAtmosphere(dungeonMap, sourceAtmos.Space, sourceAtmos.Mixture);
        }

        _loaded[ent.Comp.Key] = dungeonMap;
        return true;
    }
}
