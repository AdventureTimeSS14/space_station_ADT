using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.ADT.Procedural;
using Content.Shared.ADT.Procedural;
using Content.Shared.Decals;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class DungeonRoomTest : GameTest
{
    [Test]
    public async Task RoomTileMapIsConsistent()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var room in protoMan.EnumeratePrototypes<ADTDungeonRoomPrototype>())
                {
                    Assert.That(room.Size.X, Is.GreaterThan(0), $"{room.ID}: ширина комнаты должна быть больше нуля.");
                    Assert.That(room.Size.Y, Is.GreaterThan(0), $"{room.ID}: высота комнаты должна быть больше нуля.");

                    Assert.That(room.Tiles, Has.Count.EqualTo(room.Size.Y),
                        $"{room.ID}: строк тайлов {room.Tiles.Count}, а размер обещает {room.Size.Y}.");

                    for (var i = 0; i < room.Tiles.Count; i++)
                    {
                        Assert.That(room.Tiles[i], Has.Length.EqualTo(room.Size.X),
                            $"{room.ID}: в строке {i} символов {room.Tiles[i].Length}, а размер обещает {room.Size.X}.");
                    }

                    var unknown = room.Tiles
                        .SelectMany(row => row)
                        .Where(symbol => symbol != '.' && !room.Legend.ContainsKey(symbol.ToString()))
                        .Distinct()
                        .ToList();

                    Assert.That(unknown, Is.Empty, $"{room.ID}: символы без легенды: {string.Join(", ", unknown)}");

                    foreach (var (symbol, _) in room.Legend)
                    {
                        Assert.That(symbol, Has.Length.EqualTo(1),
                            $"{room.ID}: ключ легенды \"{symbol}\" должен быть одним символом.");
                    }

                    if (room.Areas.Count > 0)
                    {
                        Assert.That(room.Areas, Has.Count.EqualTo(room.Size.Y),
                            $"{room.ID}: строк областей {room.Areas.Count}, а размер обещает {room.Size.Y}.");

                        for (var i = 0; i < room.Areas.Count; i++)
                        {
                            Assert.That(room.Areas[i], Has.Length.EqualTo(room.Size.X),
                                $"{room.ID}: в строке областей {i} символов {room.Areas[i].Length}, а размер обещает {room.Size.X}.");
                        }

                        var unknownAreas = room.Areas
                            .SelectMany(row => row)
                            .Where(symbol => symbol != '.' && !room.AreaLegend.ContainsKey(symbol.ToString()))
                            .Distinct()
                            .ToList();

                        Assert.That(unknownAreas, Is.Empty,
                            $"{room.ID}: символы областей без легенды: {string.Join(", ", unknownAreas)}");
                    }

                    foreach (var (symbol, _) in room.AreaLegend)
                    {
                        Assert.That(symbol, Has.Length.EqualTo(1),
                            $"{room.ID}: ключ легенды областей \"{symbol}\" должен быть одним символом.");
                    }
                }
            });
        });
    }

    [Test]
    public async Task RoomPrototypesExist()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var tileDefManager = server.ResolveDependency<ITileDefinitionManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var room in protoMan.EnumeratePrototypes<ADTDungeonRoomPrototype>())
                {
                    foreach (var tag in room.Tags)
                    {
                        Assert.That(protoMan.HasIndex<TagPrototype>(tag),
                            $"{room.ID}: неизвестный тег {tag}.");
                    }

                    foreach (var (symbol, tile) in room.Legend)
                    {
                        Assert.That(tileDefManager.TryGetDefinition(tile.Tile.Id, out _),
                            $"{room.ID}: символ \"{symbol}\" ссылается на неизвестный тайл {tile.Tile.Id}.");
                    }

                    foreach (var group in room.Entities)
                    {
                        Assert.That(protoMan.HasIndex<EntityPrototype>(group.Proto),
                            $"{room.ID}: неизвестная сущность {group.Proto}.");
                    }

                    foreach (var group in room.Decals)
                    {
                        Assert.That(protoMan.HasIndex<DecalPrototype>(group.Id),
                            $"{room.ID}: неизвестная декаль {group.Id}.");
                    }

                    foreach (var (symbol, area) in room.AreaLegend)
                    {
                        Assert.That(protoMan.HasIndex<EntityPrototype>(area.Id),
                            $"{room.ID}: символ областей \"{symbol}\" ссылается на неизвестную область {area.Id}.");
                    }
                }
            });
        });
    }

    [Test]
    public async Task RoomContentStaysInBounds()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var room in protoMan.EnumeratePrototypes<ADTDungeonRoomPrototype>())
                {
                    foreach (var group in room.Entities)
                    {
                        foreach (var position in group.Positions)
                        {
                            Assert.That(position.X, Is.InRange(0f, (float)room.Size.X),
                                $"{room.ID}: {group.Proto} вылез за комнату по X.");
                            Assert.That(position.Y, Is.InRange(0f, (float)room.Size.Y),
                                $"{room.ID}: {group.Proto} вылез за комнату по Y.");
                        }
                    }

                    foreach (var group in room.Decals)
                    {
                        foreach (var position in group.Positions)
                        {
                            Assert.That(position.X, Is.InRange(0f, (float)room.Size.X),
                                $"{room.ID}: декаль {group.Id} вылезла за комнату по X.");
                            Assert.That(position.Y, Is.InRange(0f, (float)room.Size.Y),
                                $"{room.ID}: декаль {group.Id} вылезла за комнату по Y.");
                        }
                    }
                }
            });
        });
    }

    [Test]
    public async Task EveryRoomSpawns()
    {
        var server = Pair.Server;
        var entMan = server.EntMan;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var random = server.ResolveDependency<IRobustRandom>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var rooms = entMan.System<ADTDungeonRoomSystem>();

        var roomIds = protoMan.EnumeratePrototypes<ADTDungeonRoomPrototype>()
            .Select(room => room.ID)
            .ToList();

        foreach (var roomId in roomIds)
        {
            await server.WaitPost(() =>
            {
                var room = protoMan.Index<ADTDungeonRoomPrototype>(roomId);
                var mapUid = mapSystem.CreateMap(out var mapId);
                var grid = mapManager.CreateGridEntity(mapId);

                rooms.SpawnRoom(grid.Owner, grid.Comp, Vector2i.Zero, room, random, clearExisting: false, rotation: false);

                var tiles = mapSystem.GetAllTiles(grid.Owner, grid.Comp).Count();

                Assert.That(tiles, Is.GreaterThan(0), $"{roomId}: после установки на гриде не оказалось тайлов.");

                entMan.DeleteEntity(mapUid);
            });
        }
    }
}
