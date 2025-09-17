using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Mapping;

[TestFixture]
public sealed class ADTStationsMappingLoadTest
{
    [Test]
    public async Task ADTStations_AllMaps_LoadWithMappingCommand()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, Connected = true, DummyTicker = false });
        var server = pair.Server;

        var res = server.ResolveDependency<IResourceManager>();
        var mapSys = server.System<MapSystem>();

        var folder = new ResPath("/Maps/ADTMaps/ADTStations");
        var maps = res.ContentFindFiles(folder)
            .Where(p => p.Extension == "yml" && !p.Filename.StartsWith(".", StringComparison.Ordinal))
            .OrderBy(p => p.Filename)
            .ToArray();

        Assert.That(maps.Length, Is.GreaterThan(0), $"No maps found in {folder}");

        await pair.RunTicksSync(5);

        foreach (var path in maps)
        {
            var mapId = 1;
            while (mapSys.MapExists(new MapId(mapId)))
                mapId++;

            // Use the mapping command to mirror in-game usage; this sets up autosave etc.
            await pair.WaitClientCommand($"mapping {mapId} {path}");

            var loaded = mapSys.GetMap(new MapId(mapId));
            Assert.That(mapSys.MapExists(new MapId(mapId)), $"mapping did not create map for {path}");

            // Clean up after each to avoid buildup
            await server.WaitPost(() => server.EntMan.DeleteEntity(loaded));

            await pair.RunTicksSync(1);
        }

        await pair.CleanReturnAsync();
    }
}
