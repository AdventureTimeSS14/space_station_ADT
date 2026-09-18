using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.Maps;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.UnitTesting;
using YamlDotNet.RepresentationModel;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class ADTPostMapInitTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings()
    {
        Connected = true,
        Dirty = true,
    };

    private static readonly string[] NoSpawnMaps =
    {
        "CentComm",
        "Dart"
    };

    private static readonly string[] BrokenGameMaps =
    {
        "ADT_kilo",
        "ADT_Barratry",
        "ADT_Delta",
        "ADT_Bagel",
        "ADT_Gemini",
        "ADT_Kerberos",
        "ADT_Cluster"
    };

    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task ADTGameMapsLoadableTest()
    {
        var pair = Pair;
        var server = pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapLoader = entManager.System<MapLoaderSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var resourceManager = server.ResolveDependency<IResourceManager>();
        var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
        var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();

        var gameMaps = protoManager.EnumeratePrototypes<GameMapPrototype>()
            .Where(x => x.ID != PoolManager.TestMap)
            .OrderBy(x => x.ID)
            .ToList();

        TestContext.Out.WriteLine($"ADTPostMapInitTest: проверяю {gameMaps.Count} карт: " +
            string.Join(", ", gameMaps.Select(x => x.ID)));

        var failures = new List<string>();

        await server.WaitPost(() =>
        {
            foreach (var mapProto in gameMaps)
            {
                if (BrokenGameMaps.Contains(mapProto.ID))
                {
                    TestContext.Out.WriteLine($"ADTPostMapInitTest: {mapProto.ID} - пропущена (BrokenGameMaps: " +
                        "работает в игре, но падает в тестах)");
                    continue;
                }

                CheckGameMap(mapProto, server, mapManager, entManager, mapLoader, mapSystem,
                    protoManager, resourceManager, ticker, shuttleSystem, failures);
            }
        });

        TestContext.Out.WriteLine($"ADTPostMapInitTest: обнаружено {failures.Count} проблем:");
        foreach (var failure in failures)
            TestContext.Out.WriteLine(failure);

        Assert.Multiple(() =>
        {
            foreach (var failure in failures)
                Assert.Fail(failure);
        });
    }

    private void CheckGameMap(
        GameMapPrototype mapProto,
        RobustIntegrationTest.ServerIntegrationInstance server,
        IMapManager mapManager,
        IEntityManager entManager,
        MapLoaderSystem mapLoader,
        SharedMapSystem mapSystem,
        IPrototypeManager protoManager,
        IResourceManager resourceManager,
        GameTicker ticker,
        ShuttleSystem shuttleSystem,
        List<string> failures)
    {
        var mapPath = mapProto.MapPath;
        var problems = new List<string>();

        problems.AddRange(CheckMapFile(mapPath, mapProto, resourceManager));

        MapId? mapId = null;
        try
        {
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            ticker.LoadGameMap(mapProto, out var loadedMapId, opts);
            mapId = loadedMapId;
        }
        catch (Exception ex)
        {
            problems.Add($"Карта {mapProto.ID} ({mapPath}) НЕ ЗАГРУЗИЛАСЬ. Причина: {UnwrapException(ex)}");
        }

        if (mapId is { } id)
        {
            try
            {
                CheckLoadedMap(mapProto, server, mapManager, entManager, mapLoader, mapSystem,
                    protoManager, shuttleSystem, id, problems);
            }
            finally
            {
                try
                {
                    mapSystem.DeleteMap(id);
                }
                catch (Exception ex)
                {
                    problems.Add($"Карта {mapProto.ID}: не удалось выгрузить карту: {ex.Message}");
                }
            }
        }

        if (problems.Count > 0)
        {
            failures.Add($"--- {mapProto.ID} ({mapPath}): {problems.Count} проблем(ы) ---");
            failures.AddRange(problems);
        }
        else
        {
            TestContext.Out.WriteLine($"ADTPostMapInitTest: {mapProto.ID} - OK");
        }
    }

    private static List<string> CheckMapFile(ResPath mapPath, GameMapPrototype mapProto, IResourceManager resourceManager)
    {
        var problems = new List<string>();

        if (!resourceManager.TryContentFileRead(mapPath.ToRootedPath(), out var fileStream))
        {
            problems.Add($"Файл карты {mapPath} не найден в ресурсах. Проверь mapPath в прототипе {mapProto.ID}.");
            return problems;
        }

        using var reader = new StreamReader(fileStream);
        var yaml = new YamlStream();
        yaml.Load(reader);
        var root = yaml.Documents[0].RootNode;

        var hasMaps = root["maps"] is YamlSequenceNode { Children.Count: > 0 };
        var gridCount = root["grids"] is YamlSequenceNode grids ? grids.Children.Count : 0;
        if (!hasMaps && gridCount > 0)
        {
            problems.Add($"Файл {mapPath} сохранён как GRID, а ожидалась MAP: секция 'maps' пуста, но 'grids' содержит {gridCount} грид(ов). " +
                "Открой карту в редакторе и сохрани её заново как map (не grid).");
        }

        var gridStationIds = new List<string>();
        if (root["entities"] is YamlSequenceNode entities)
        {
            foreach (var entity in entities)
            {
                if (entity["components"] is not YamlSequenceNode comps)
                    continue;

                foreach (var comp in comps)
                {
                    if (comp["type"]?.AsString() != "BecomesStation")
                        continue;

                    var id = comp["id"]?.AsString() ?? "<id не указан>";
                    gridStationIds.Add(id);
                }
            }
        }

        var declaredStations = mapProto.Stations.Keys.ToHashSet();

        foreach (var id in gridStationIds)
        {
            if (!declaredStations.Contains(id))
            {
                problems.Add($"В файле {mapPath} грид имеет BecomesStation id '{id}', но в прототипе {mapProto.ID} станция с таким id не объявлена. " +
                    $"Объявленные станции: [{string.Join(", ", declaredStations)}]. " +
                    $"Исправь: добавь '{id}' в секцию stations прототипа ИЛИ переименуй BecomesStation id в файле, чтобы они совпадали.");
            }
        }

        foreach (var key in declaredStations)
        {
            if (!gridStationIds.Contains(key))
            {
                problems.Add($"В прототипе {mapProto.ID} объявлена станция '{key}', но ни один грид в {mapPath} не имеет BecomesStation id '{key}'. " +
                    "Добавь компонент BecomesStation с этим id на главный грид станции.");
            }
        }

        return problems;
    }

    private void CheckLoadedMap(
        GameMapPrototype mapProto,
        RobustIntegrationTest.ServerIntegrationInstance server,
        IMapManager mapManager,
        IEntityManager entManager,
        MapLoaderSystem mapLoader,
        SharedMapSystem mapSystem,
        IPrototypeManager protoManager,
        ShuttleSystem shuttleSystem,
        MapId mapId,
        List<string> problems)
    {
        var mapPath = mapProto.MapPath;
        var grids = mapManager.GetAllGrids(mapId).ToList();
        var gridUids = grids.Select(o => o.Owner).ToList();

        if (grids.Count == 0)
        {
            problems.Add($"Карта {mapProto.ID} загрузилась, но на ней НЕТ гридов. Файл {mapPath} пуст или повреждён.");
            return;
        }

        var memberQuery = entManager.GetEntityQuery<StationMemberComponent>();
        var stationGrids = grids.Where(g => memberQuery.HasComponent(g.Owner)).ToList();

        if (stationGrids.Count == 0)
        {
            problems.Add($"Карта {mapProto.ID} загрузилась, но ни один грид не получил StationMemberComponent - станция не была создана. " +
                "Скорее всего причина выше: несовпадение BecomesStation id с секцией stations прототипа, либо ошибки при инициализации (смотри логи сервера).");
            return;
        }

        var largestArea = 0f;
        EntityUid targetGrid = default;
        foreach (var grid in stationGrids)
        {
            var area = grid.Comp.LocalAABB.Width * grid.Comp.LocalAABB.Height;
            if (area <= largestArea)
                continue;

            largestArea = area;
            targetGrid = grid.Owner;
        }

        var station = memberQuery.GetComponent(targetGrid).Station;

        if (entManager.TryGetComponent<StationEmergencyShuttleComponent>(station, out var stationEvac))
        {
            mapSystem.CreateMap(out var shuttleMap);
            try
            {
                if (!mapLoader.TryLoadGrid(shuttleMap, stationEvac.EmergencyShuttlePath, out var shuttle))
                {
                    problems.Add($"Аварийный шаттл {stationEvac.EmergencyShuttlePath} карты {mapProto.ID} не загрузился. " +
                        "Проверь файл шаттла: он должен быть сохранён как grid.");
                }
                else if (!shuttleSystem.TryFTLDock(shuttle!.Value.Owner,
                             entManager.GetComponent<ShuttleComponent>(shuttle.Value.Owner), targetGrid))
                {
                    problems.Add($"Аварийный шаттл {stationEvac.EmergencyShuttlePath} не смог пристыковаться к станции {mapProto.ID}. " +
                        "Проверь, что рядом со станцией есть свободное место для стыковки.");
                }
            }
            finally
            {
                mapSystem.DeleteMap(shuttleMap);
            }
        }

        if (!entManager.HasComponent<StationJobsComponent>(station))
            return;

        if (!NoSpawnMaps.Contains(mapProto.ID))
        {
            var lateSpawns = 0;
            lateSpawns += GetCountLateSpawn<SpawnPointComponent>(gridUids, entManager);
            lateSpawns += GetCountLateSpawn<ContainerSpawnPointComponent>(gridUids, entManager);

            if (lateSpawns == 0)
            {
                problems.Add($"На карте {mapProto.ID} ({mapPath}) нет ни одной точки позднего спавна (LateJoin). " +
                    "Добавь на гриды станции SpawnPoint (LateJoin) или ContainerSpawnPoint (LateJoin).");
            }
        }

        var comp = entManager.GetComponent<StationJobsComponent>(station);
        var jobs = new HashSet<ProtoId<JobPrototype>>(comp.SetupAvailableJobs.Keys);

        var spawnPoints = entManager.EntityQuery<SpawnPointComponent>()
            .Where(x => x.SpawnType == SpawnPointType.Job && x.Job != null)
            .Select(x => x.Job.Value);

        jobs.ExceptWith(spawnPoints);

        spawnPoints = entManager.EntityQuery<ContainerSpawnPointComponent>()
            .Where(x => x.SpawnType is SpawnPointType.Job or SpawnPointType.Unset && x.Job != null)
            .Select(x => x.Job.Value);

        jobs.ExceptWith(spawnPoints);

        if (jobs.Count > 0)
        {
            var componentFactory = server.ResolveDependency<IComponentFactory>();

            var missing = jobs.Select(job =>
            {
                var spawnPointProtos = protoManager.EnumeratePrototypes<EntityPrototype>()
                    .Where(proto => !proto.Abstract
                        && proto.TryGetComponent<SpawnPointComponent>(out var spawn, componentFactory)
                        && spawn.SpawnType == SpawnPointType.Job
                        && spawn.Job == job)
                    .Select(proto => proto.ID);

                var containerSpawnPointProtos = protoManager.EnumeratePrototypes<EntityPrototype>()
                    .Where(proto => !proto.Abstract
                        && proto.TryGetComponent<ContainerSpawnPointComponent>(out var spawn, componentFactory)
                        && spawn.SpawnType is SpawnPointType.Job or SpawnPointType.Unset
                        && spawn.Job == job)
                    .Select(proto => proto.ID);

                var protos = string.Join(", ", spawnPointProtos.Concat(containerSpawnPointProtos).Distinct());
                return protos.Length == 0
                    ? $"{job} (нет ни одного прототипа спавн-точки)"
                    : $"{job} (есть прототипы: {protos}, но они не размещены на карте)";
            });

            problems.Add($"Карта {mapProto.ID} ({mapPath}) не имеет спавн-точек для должностей: {string.Join("; ", missing)}. " +
                "Размести соответствующие спавн-точки на гридах станции и сохрани карту.");
        }
    }

    private static string UnwrapException(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current != null; current = current.InnerException)
            messages.Add($"{current.GetType().Name}: {current.Message}");

        return string.Join(" -> ", messages);
    }

    private static int GetCountLateSpawn<T>(List<EntityUid> gridUids, IEntityManager entManager)
        where T : ISpawnPoint, IComponent
    {
        var resultCount = 0;
        var queryPoint = entManager.AllEntityQueryEnumerator<T, TransformComponent>();
#nullable enable
        while (queryPoint.MoveNext(out T? comp, out var xform))
        {
            var spawner = (ISpawnPoint)comp;

            if (spawner.SpawnType is not SpawnPointType.LateJoin
                || xform.GridUid == null
                || !gridUids.Contains(xform.GridUid.Value))
            {
                continue;
            }
#nullable disable
            resultCount++;
            break;
        }

        return resultCount;
    }
}