using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Server.Maps;
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
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly StealthSystem _stealth = default!;
    public GhostBarMapPrototype? _GhostBarMap = null; /// его значение нельзя объявить методом. Много проб, ни одна из них не успешна, даже в гугле информации про такое нету.
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeNetworkEvent<GhostBarSpawnEvent>(SpawnPlayer);
        SubscribeLocalEvent<GhostBarPlayerComponent, MindRemovedMessage>(OnPlayerGhosted);
    }
    private GhostBarMapPrototype GetRandomMapProto() ///метод нужен на старте всего этого чтобы выбрать карту и передать её прототип в _GhostBarMap
    {
        List<GhostBarMapPrototype> maplist = new List<GhostBarMapPrototype>();

        foreach (var proto in _prototypeManager.EnumeratePrototypes<GhostBarMapPrototype>()) ///костыльный метод, т.к. прототип пулла карт ломал тесты
            maplist.Add(proto);
        var mapprotostr = _random.Pick(maplist);
        var mapproto = _prototypeManager.Index<GhostBarMapPrototype>(mapprotostr); ///да, я знаю, что тут лишняя переменная, но при её удалении вылезает куча ошибок
        return mapproto;
    }
    private void OnRoundStart(RoundStartingEvent ev)
    {
        _mapSystem.CreateMap(out var mapId);
        var options = new MapLoadOptions { LoadMap = true };
        var mapProto = GetRandomMapProto();
        _GhostBarMap = mapProto;
        if (mapProto == null) /// тут проверки и загрузки переменных
            return;

        if (_mapLoader.TryLoad(mapId, mapProto.Path, out _, options)) 
        {
            _mapSystem.SetPaused(mapId, false);
            if (_prototypeManager.TryIndex<WeatherPrototype>(mapProto.Weather, out var weatherProto)) 
            {
                TimeSpan? endTime = null;
                _weathersystem.SetWeather(mapId, weatherProto, endTime);
            }
        }
    }

    public void SpawnPlayer(GhostBarSpawnEvent msg, EntitySessionEventArgs args)
    {
        if (!_entityManager.HasComponent<GhostComponent>(args.SenderSession.AttachedEntity))
        {
            Log.Warning($"User {args.SenderSession.Name} tried to spawn at ghost bar without being a ghost.");
            return;
        }
        if (_GhostBarMap == null)
            return;
        var spawnPoints = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<GhostBarSpawnPointComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            spawnPoints.Add(_entityManager.GetComponent<TransformComponent>(ent).Coordinates);
        }

        if (spawnPoints.Count == 0)
        {
            Log.Warning("No spawn points found for ghost bar.");
            return;
        }


        var randomSpawnPoint = _random.Pick(spawnPoints);
        var randomJob = _random.Pick(_GhostBarMap.Jobs);
        var profile = _ticker.GetPlayerProfile(args.SenderSession);
        var mobUid = _spawningSystem.SpawnPlayerMob(randomSpawnPoint, randomJob, profile, null);

        _entityManager.EnsureComponent<GhostBarPlayerComponent>(mobUid);
        _entityManager.EnsureComponent<MindShieldComponent>(mobUid);
        _entityManager.EnsureComponent<AntagImmuneComponent>(mobUid);
        if (_GhostBarMap.Pacified)
            _entityManager.EnsureComponent<PacifiedComponent>(mobUid);
        if (_GhostBarMap.Ghosted != 1f)
        {
            _entityManager.EnsureComponent<StealthComponent>(mobUid);
            _stealth.SetVisibility(mobUid, _GhostBarMap.Ghosted);

        }
        var targetMind = _mindSystem.GetMind(args.SenderSession.UserId);


        if (targetMind != null)
        {
            _mindSystem.TransferTo(targetMind.Value, mobUid, true);
        }
    }

    private void OnPlayerGhosted(EntityUid uid, GhostBarPlayerComponent component, MindRemovedMessage args)
    {
        _entityManager.DeleteEntity(uid);
    }
}

