using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class BlobObserverTest
{
    /// <summary>
    /// Blob observer must survive its own startup: it deletes itself if picking up
    /// the virtual controller item fails, which silently breaks blob transformation.
    /// </summary>
    [Test]
    public async Task BlobObserverSpawnAndSurviveTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid observer = default;
        await server.WaitPost(() =>
        {
            observer = server.EntMan.SpawnEntity("ADTMobObserverBlob", map.GridCoords);
        });

        await pair.RunTicksSync(5);

        Assert.That(server.EntMan.Deleted(observer), Is.False,
            "ADTMobObserverBlob deleted itself right after spawning (virtual item pickup failed in BlobObserverSystem.OnStartup)");

        await server.WaitPost(() => server.EntMan.DeleteEntity(observer));
        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Blob core should spawn and initialize without errors.
    /// </summary>
    [Test]
    public async Task BlobCoreSpawnTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        EntityUid core = default;
        await server.WaitPost(() =>
        {
            core = server.EntMan.SpawnEntity("ADTCoreBlobTile", map.GridCoords);
        });

        await pair.RunTicksSync(5);

        Assert.That(server.EntMan.Deleted(core), Is.False, "ADTCoreBlobTile got deleted right after spawning");

        await server.WaitPost(() => server.EntMan.DeleteEntity(core));
        await pair.CleanReturnAsync();
    }
}
