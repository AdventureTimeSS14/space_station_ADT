using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Ghost;
using Content.Server.ADT.Ghostbar.Components;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Antag.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Random.Helpers;
using Content.Shared.Weather;
using Content.Shared.Stealth.Components;
using Content.Server.Stealth;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map.Components;
using System.Linq;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Ghostbar;

public sealed class GhostBarSystem : EntitySystem
{
    [Dependency] private readonly SharedWeatherSystem _weathersystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationSpawningSystem _spawningSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    public GhostBarMapPrototype? GhostBarMap;   // Существует для того, чтобы посетители гост бара спавнились соответственно его настройкам. Если значение равно null во время раунда - что-то сломано


    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<GhostBarPlayerComponent, MindRemovedMessage>(OnPlayerGhosted);

        SubscribeNetworkEvent<GhostBarSpawnEvent>(SpawnPlayer);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        // _mapSystem.CreateMap(out var mapId);
        var opts = new DeserializationOptions
        {
            StoreYamlUids = true,
            InitializeMaps = true,
        };

        var mapList = _prototypeManager.EnumeratePrototypes<GhostBarMapPrototype>().ToList();
        GhostBarMap = _random.Pick(mapList);
        if (GhostBarMap == null)
            return;

        if (!_mapLoader.TryLoadMap(new ResPath(GhostBarMap.Path), out var mapId, out _, opts))
            return;

        if (!TryComp<MapComponent>(mapId, out var mapComponent))
            return;

        _mapSystem.SetPaused(mapComponent.MapId, false);

        if (GhostBarMap.Weather.HasValue)
            _weathersystem.SetWeather(mapComponent.MapId, _prototypeManager.Index(GhostBarMap.Weather.Value), null);
    }

    private void OnPlayerGhosted(EntityUid uid, GhostBarPlayerComponent component, MindRemovedMessage args)
    {
        QueueDel(uid);
        Spawn("PolymorphAshJauntAnimation", Transform(uid).Coordinates);    // Сугубо визуал
    }

    public void SpawnPlayer(GhostBarSpawnEvent msg, EntitySessionEventArgs args)
    {
        if (!HasComp<GhostComponent>(args.SenderSession.AttachedEntity))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to spawn at ghost bar without being a ghost.");
            return;
        }
        if (GhostBarMap == null)
            return;
        var spawnPoints = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<GhostBarSpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out var xform))
        {
            spawnPoints.Add(xform.Coordinates);
        }

        if (spawnPoints.Count == 0)
        {
            Log.Warning("No spawn points found for ghost bar.");
            return;
        }


        var randomSpawnPoint = _random.Pick(spawnPoints);
        var randomJob = _random.Pick(GhostBarMap.Jobs);
        var profile = _ticker.GetPlayerProfile(args.SenderSession);
        var mobUid = _spawningSystem.SpawnPlayerMob(randomSpawnPoint, randomJob, profile, null);

        EnsureComp<GhostBarPlayerComponent>(mobUid);
        EnsureComp<MindShieldComponent>(mobUid);
        EnsureComp<AntagImmuneComponent>(mobUid);
        if (GhostBarMap.Pacified)
            EnsureComp<PacifiedComponent>(mobUid);
        if (GhostBarMap.Ghosted != 1f)
        {
            EnsureComp<StealthComponent>(mobUid);
            _stealth.SetVisibility(mobUid, GhostBarMap.Ghosted);
        }
        var targetMind = _mindSystem.GetMind(args.SenderSession.UserId);

        if (targetMind.HasValue)
            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
    }
}

