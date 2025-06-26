using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Eye;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.IntegrationTests.Tests.Actions;

[TestFixture]
public sealed class ActionPvsDetachTest
{
    [Test]
    public async Task TestActionDetach()
    {
        var pair = await PoolManager.GetServerClient();

        var netMan = pair.Client.ResolveDependency<INetManager>();
        if (!netMan.IsConnected)
        {
            await pair.CleanReturnAsync();
            Assert.Ignore("Пропущено: клиент не подключён.");
        }
        var (server, client) = pair;
        var sys = server.System<SharedActionsSystem>();
        var cSys = client.System<SharedActionsSystem>();

        EntityUid ent = default;
        var map = await pair.CreateTestMap();
        await server.WaitPost(() => ent = server.EntMan.SpawnAtPosition("MobHuman", map.GridCoords));
        await pair.RunTicksSync(5);
        var cEnt = pair.ToClientUid(ent);

        var initActions = sys.GetActions(ent).Count();
        Assert.That(initActions, Is.GreaterThan(0));
        Assert.That(initActions, Is.EqualTo(cSys.GetActions(cEnt).Count()));

        var visSys = server.System<VisibilitySystem>();
        server.Post(() =>
        {
            var enumerator = server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                visSys.AddLayer(child, (int) VisibilityFlags.Ghost);
            }
        });
        await pair.RunTicksSync(5);

        Assert.That(sys.GetActions(ent).Count(), Is.EqualTo(initActions));
        Assert.That(cSys.GetActions(cEnt).Count(), Is.EqualTo(initActions));

        server.Post(() =>
        {
            var enumerator = server.Transform(ent).ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                visSys.RemoveLayer(child, (int) VisibilityFlags.Ghost);
            }
        });
        await pair.RunTicksSync(5);
        Assert.That(sys.GetActions(ent).Count(), Is.EqualTo(initActions));
        Assert.That(cSys.GetActions(cEnt).Count(), Is.EqualTo(initActions));

        await server.WaitPost(() => server.EntMan.DeleteEntity(map.MapUid));
        await pair.CleanReturnAsync();
    }
}
