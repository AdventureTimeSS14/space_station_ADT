using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.ADT.LogicCircuit;
using Content.Shared.ADT.LogicCircuit.Components;
using Content.Shared.CCVar;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class LogicCircuitSaveLoadTest : GameTest
{
    private const string BoxProto = "ADTLogicCircuitBoxCodeLock";

    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task SaveLoadCircuitBox()
    {
        var mapPath = new ResPath("/Maps/Test/LogicCircuitTest.yml");

        var pair = Pair;
        var server = pair.Server;
        var mapManager = server.ResolveDependency<IMapManager>();
        var entities = server.ResolveDependency<IEntityManager>();
        var mapLoader = entities.System<MapLoaderSystem>();
        var mapSystem = entities.System<SharedMapSystem>();
        var resManager = server.ResolveDependency<IResourceManager>();

        var expectedNodes = 0;
        var expectedWires = 0;

        await server.WaitAssertion(() =>
        {
            resManager.UserData.CreateDir(mapPath.Directory);

            mapSystem.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, new Vector2i(0, 0), new Tile(typeId: 1, flags: 1, variant: 255));

            var box = entities.SpawnEntity(BoxProto, new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
            var comp = entities.GetComponent<ADTLogicCircuitComponent>(box);

            expectedNodes = comp.Layout.Nodes.Count;
            expectedWires = comp.Layout.Wires.Count;

            Assert.Multiple(() =>
            {
                Assert.That(expectedNodes, Is.GreaterThan(0), "Схема из прототипа не прочиталась.");
                Assert.That(expectedWires, Is.GreaterThan(0), "Провода из прототипа не прочитались.");
                Assert.That(comp.Broken, Is.False, "Схема из прототипа не прошла проверку.");
                Assert.That(comp.Compiled, Is.Not.Null, "Схема из прототипа не скомпилировалась.");
            });

            var register = comp.Layout.Nodes.First(node => node.Id == "reg");
            register.State[0] = LogicSignal.FromText("1234");

            Assert.That(mapLoader.TrySaveMap(mapId, mapPath));
            mapSystem.DeleteMap(mapId);
        });

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            Assert.That(mapLoader.TryLoadMap(mapPath, out _, out _));
        });

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            ADTLogicCircuitComponent? loaded = null;
            var query = entities.EntityQueryEnumerator<ADTLogicCircuitComponent>();

            while (query.MoveNext(out _, out var comp))
            {
                loaded = comp;
                break;
            }

            Assert.That(loaded, Is.Not.Null, "После загрузки карты коробка не нашлась.");

            Assert.Multiple(() =>
            {
                Assert.That(loaded!.Layout.Nodes, Has.Count.EqualTo(expectedNodes));
                Assert.That(loaded.Layout.Wires, Has.Count.EqualTo(expectedWires));
                Assert.That(loaded.Broken, Is.False, "Схема сломалась при загрузке карты.");
                Assert.That(loaded.Compiled, Is.Not.Null, "Схема не скомпилировалась при загрузке карты.");

                var register = loaded.Layout.Nodes.First(node => node.Id == "reg");
                Assert.That(register.State[0].AsText(), Is.EqualTo("1234"), "Состояние элемента потерялось.");

                var input = loaded.Layout.Nodes.First(node => node.Id == "in5");
                Assert.That(input.Config[1].AsText(), Is.EqualTo("5"), "Настройка элемента потерялась.");
            });
        });
    }
}
