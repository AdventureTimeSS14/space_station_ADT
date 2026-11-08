using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.ADT.Lavaland.Dungeon;
using Content.Server.ADT.Lavaland.Helios;
using Content.Shared.ADT.Lavaland.Helios;
using Content.Shared.Item;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.ADT.Indestructible;
using Content.Shared.ADT.Lavaland.Puzzle;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Gravity;
using Content.Shared.Light.Components;
using Content.Server.Power.Components;
using Content.Shared.Teleportation.Components;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class HeliosComplexTest : GameTest
{
    private static readonly ResPath MapPath = new("/Maps/ADT/Lavaland/helios_complex.yml");

    private IEntityManager _entities = default!;

    private EntityUid? EntityAt(string proto, Vector2i tile)
    {
        var query = _entities.EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var meta, out var xform))
        {
            if (meta.EntityPrototype?.ID != proto)
                continue;

            var pos = xform.LocalPosition;
            if (new Vector2i((int) MathF.Floor(pos.X), (int) MathF.Floor(pos.Y)) == tile)
                return uid;
        }

        return null;
    }

    private bool IsOpen(string proto, Vector2i tile)
    {
        var door = EntityAt(proto, tile);
        Assert.That(door, Is.Not.Null, $"Нет двери {proto} на {tile}");
        var state = _entities.GetComponent<DoorComponent>(door!.Value).State;
        return state is DoorState.Open or DoorState.Opening;
    }

    private async Task Type(DeviceLinkSystem link, params (Vector2i Keypad, string Code)[] locks)
    {
        var length = locks.Max(l => l.Code.Length);
        for (var i = 0; i < length; i++)
        {
            await Pair.Server.WaitAssertion(() =>
            {
                foreach (var (keypad, code) in locks)
                {
                    if (i >= code.Length)
                        continue;

                    var key = EntityAt($"ADTHeliosKey{code[i]}", keypad);
                    Assert.That(key, Is.Not.Null, $"Нет клавиши {code[i]} на замке {keypad}");
                    link.InvokePort(key!.Value, "Pressed");
                }
            });

            await RunTicksSync(20);
        }
    }

    [Test]
    [EnsureCVar(Side.Server, typeof(CCVars), nameof(CCVars.GridFill), false)]
    public async Task HeliosComplexWorks()
    {
        var server = Pair.Server;
        _entities = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var mapLoader = _entities.System<MapLoaderSystem>();
        var mapSystem = _entities.System<SharedMapSystem>();
        var link = _entities.System<DeviceLinkSystem>();
        var damageable = _entities.System<DamageableSystem>();

        EntityUid complexMap = default;

        await server.WaitAssertion(() =>
        {
            var opts = new DeserializationOptions { InitializeMaps = true };
            Assert.That(mapLoader.TryLoadMap(MapPath, out var map, out var grids, opts), "Карта комплекса не загрузилась.");
            Assert.That(grids!.Count, Is.EqualTo(1), "В карте комплекса должен быть ровно один грид.");
            complexMap = map!.Value.Owner;
            mapSystem.SetPaused(map.Value.Comp.MapId, false);
        });

        await RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            var emblem = _entities.EntityQuery<ADTSlidingPuzzleElementComponent>()
                .Count(e => _entities.GetComponent<MetaDataComponent>(e.Owner).EntityPrototype!.ID.StartsWith("ADTHeliosEmblemPillar"));
            var glyph = _entities.EntityQuery<ADTSlidingPuzzleElementComponent>()
                .Count(e => _entities.GetComponent<MetaDataComponent>(e.Owner).EntityPrototype!.ID.StartsWith("ADTPuzzlePillar"));
            Assert.Multiple(() =>
            {
                Assert.That(emblem, Is.EqualTo(8), "Пятнашки с эмблемой не сгенерировались.");
                Assert.That(glyph, Is.EqualTo(8), "Пятнашки с печатью не сгенерировались.");
            });

            Assert.Multiple(() =>
            {
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-11, -15)), Is.False, "Замок склада открыт с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -33)), Is.False, "Створка Г1-1 открыта с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -80)), Is.False, "Створка Г2-А открыта с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -109)), Is.False, "Дверь к ядру открыта с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -59)), Is.False, "Дверь в центральный коридор открыта с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(6, -63)), Is.False, "Щитовая открыта с самого начала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-14, -66)), Is.False, "Зал печати открыт с самого начала.");
            });

            Assert.Multiple(() =>
            {
                Assert.That(_entities.EntityQuery<ADTLightsOutPanelComponent>().Count(), Is.EqualTo(16), "Поле калибровки не создалось.");
                Assert.That(_entities.EntityQuery<ADTConduitPieceComponent>().Count(), Is.EqualTo(20), "Поле трассировки не создалось.");
                Assert.That(_entities.EntityQuery<ADTGlyphPlateComponent>().Count(), Is.EqualTo(4), "Не хватает ячеек хранилища.");
                Assert.That(_entities.EntityQuery<ADTGlyphSampleComponent>().Count(), Is.EqualTo(4), "Не хватает контейнеров с образцами.");
                Assert.That(_entities.EntityQuery<ADTLightsOutPuzzleComponent>().Single().Lit.All(l => l), Is.False, "Калибровка уже решена при старте.");
            });

            var docTiles = _entities.EntityQuery<MetaDataComponent>()
                .Where(m => m.EntityPrototype?.ID is { } id && (id.StartsWith("ADTHeliosDoc") || id.StartsWith("ADTHeliosTape")) && !id.StartsWith("ADTHeliosTapeRecorder") && id != "ADTHeliosTapeEvac")
                .Select(m => _entities.GetComponent<TransformComponent>(m.Owner))
                .Where(x => x.ParentUid != x.MapUid || x.GridUid != null)
                .Where(x => x.GridUid != null && x.ParentUid == x.GridUid)
                .Select(x => new Vector2i((int) MathF.Floor(x.LocalPosition.X), (int) MathF.Floor(x.LocalPosition.Y)))
                .ToList();
            var randomTiles = docTiles.GroupBy(t => t).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.That(randomTiles, Is.Empty, $"Документы легли на одни и те же клетки: {string.Join(", ", randomTiles)}");

            var medkits = _entities.EntityQuery<MetaDataComponent>().Count(m => m.EntityPrototype?.ID == "MedkitFilled");
            Assert.That(medkits, Is.GreaterThanOrEqualTo(3), "В комплексе мало аптечек.");

            var areas = _entities.System<Content.Shared.ADT.Areas.AreaSystem>();
            var grid = _entities.EntityQuery<MapGridComponent>().Select(g => g.Owner)
                .First(g => _entities.GetComponent<TransformComponent>(g).MapUid == complexMap);
            var caveId = areas.GetAreaPrototypeId(new EntityCoordinates(grid, new Vector2(0.5f, -119.5f)));
            Assert.That(caveId?.Id, Is.EqualTo("ADTAreaHeliosCave"), "Пещера ядра не размечена зоной с эхом.");
            Assert.That(_entities.EntityQuery<MetaDataComponent>().Any(m => m.EntityPrototype?.ID == "ADTHeliosTapeEvac"),
                "У входа нет кассеты с аварийным оповещением.");

            EntityUid? doc = _entities.EntityQuery<MetaDataComponent>()
                .FirstOrDefault(m => m.EntityPrototype?.ID == "ADTHeliosDocCheckpointLog")?.Owner;
            Assert.That(doc, Is.Not.Null, "Нет журнала КПП.");
            Assert.That(_entities.HasComponent<ItemComponent>(doc!.Value), Is.False, "Документ можно поднять.");
            Assert.That(_entities.HasComponent<DamageableComponent>(doc.Value), Is.False, "Документ можно повредить.");
            foreach (var frame in _entities.EntityQuery<MetaDataComponent>()
                         .Where(m => m.EntityPrototype?.ID == "BlastDoorFrame").Select(m => m.Owner))
            {
                Assert.That(_entities.HasComponent<ADTIndestructibleComponent>(frame), "Каркас гермозатвора можно открутить.");
            }
        });

        var heliosPuzzles = _entities.System<ADTHeliosPuzzleSystem>();
        await server.WaitAssertion(() =>
        {
            var lights = _entities.EntityQuery<ADTLightsOutPuzzleComponent>().Single();
            foreach (var index in lights.ScramblePresses.ToList())
            {
                var panel = lights.Panels[index];
                heliosPuzzles.PressPanel((panel, _entities.GetComponent<ADTLightsOutPanelComponent>(panel)));
            }
            Assert.That(lights.Finished, "Калибровка не решилась повторными нажатиями.");

            var conduit = _entities.EntityQuery<ADTConduitPuzzleComponent>().Single();
            for (var i = 0; i < conduit.Pieces.Count && !conduit.Finished; i++)
            {
                if (conduit.Solution[i] < 0)
                    continue;

                var piece = _entities.GetComponent<ADTConduitPieceComponent>(conduit.Pieces[i]);
                var target = ADTHeliosPuzzleSystem.RotateMask(piece.BaseMask, conduit.Solution[i]);
                for (var r = 0; r < 4 && ADTHeliosPuzzleSystem.RotateMask(piece.BaseMask, piece.Rotation) != target; r++)
                {
                    if (!heliosPuzzles.RotatePiece((conduit.Pieces[i], piece)))
                        break;
                }
            }
            Assert.That(conduit.Finished, "Трассировка не решилась по сохранённому решению.");

            var xformSys = _entities.System<SharedTransformSystem>();
            foreach (var plate in _entities.EntityQuery<ADTGlyphPlateComponent>().ToList())
            {
                var sample = _entities.EntityQuery<ADTGlyphSampleComponent>().First(s => s.Glyph == plate.Glyph);
                xformSys.SetCoordinates(sample.Owner, _entities.GetComponent<TransformComponent>(plate.Owner).Coordinates);
            }
        });

        await RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            Assert.That(_entities.EntityQuery<ADTGlyphPlatePuzzleComponent>().Single().Finished, "Хранилище образцов не засчитало контейнеры.");
            Assert.Multiple(() =>
            {
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -59)), "Трассировка не открыла дверь в центральный коридор.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(6, -63)), "Калибровка не открыла щитовую.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-14, -66)), "Хранилище образцов не открыло зал печати.");
            });
        });

        var mechanics = _entities.System<ADTHeliosMechanicsSystem>();
        Dictionary<string, string> secrets = default!;
        await server.WaitAssertion(() =>
        {
            secrets = mechanics.GetSecrets(complexMap);
            foreach (var key in new[] { "WAREHOUSE_CODE", "MCB_CODE", "VORN_CODE", "JOULE_CODE" })
            {
                Assert.That(secrets[key], Has.Length.EqualTo(4), $"Секрет {key} имеет неверную длину.");
                Assert.That(secrets[key].Contains('0'), Is.False, $"В секрете {key} есть ноль, которого нет на клавиатуре.");
            }

            Assert.That(_entities.EntityQuery<ADTHeliosRandomSpotComponent>().Any(), Is.False, "Остались неразыгранные случайные места.");
            var roster = _entities.EntityQuery<MetaDataComponent>()
                .Where(m => m.EntityPrototype?.ID == "ADTHeliosDocShiftRoster").Select(m => m.Owner).ToList();
            Assert.That(roster, Has.Count.EqualTo(1), "График смен должен лежать ровно в одном месте.");
            var content = _entities.GetComponent<Content.Shared.Paper.PaperComponent>(roster[0]).Content;
            Assert.That(content, Does.Contain(secrets["VORN_ID"]), "В график смен не подставился номер Ворн.");
            Assert.That(content, Does.Not.Contain("{"), "В графике смен осталась неподставленная метка.");
        });

        var wrong = (secrets["WAREHOUSE_CODE"][0] == '1' ? "2" : "1") + secrets["WAREHOUSE_CODE"][1..];
        await Type(link, (new Vector2i(-8, -14), wrong));

        await server.WaitAssertion(() =>
            Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-11, -15)), Is.False, "Неверный код открыл склад."));

        await Type(link,
            (new Vector2i(-8, -14), secrets["WAREHOUSE_CODE"]),  // Замок склада
            (new Vector2i(8, -14), secrets["MCB_CODE"]),         // Замок блока персонала
            (new Vector2i(2, -61), secrets["VORN_CODE"]),        // Замок энергосекции
            (new Vector2i(-3, -109), secrets["JOULE_CODE"]));    // Замок между камерой и ядром

        await server.WaitAssertion(() =>
        {
            var btn = EntityAt("ADTHeliosButton", new Vector2i(16, -26));
            Assert.That(btn, Is.Not.Null, "Нет кнопки разблокировки спуска.");
            link.InvokePort(btn!.Value, "Pressed");

            foreach (var puzzle in _entities.EntityQuery<ADTSlidingPuzzleComponent>().Select(p => p.Owner).ToList())
                link.InvokePort(puzzle, "ADTPuzzleSolved");

            var mask = secrets["BRK_MASK"];
            for (var i = 0; i < 4; i++)
            {
                var x = 8 + i * 2;
                var sw = EntityAt("ADTHeliosSwitch", new Vector2i(x, -61));
                Assert.That(sw, Is.Not.Null, $"Нет рубильника на x={x}");
                if (mask[i] == '1')
                    link.SendSignal(sw!.Value, "Status", true);
            }
        });

        await RunTicksSync(60);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-11, -15)), "Код склада не открыл дезкамеру.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(-13, -18)), "Код склада не открыл дверь склада.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(13, -18)), "Код персонала не открыл блок персонала.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(13, -27)), "Код персонала не открыл нижнюю дверь блока.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -33)), "Пятнашки склада не открыли створку Г1-1.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -35)), "Кнопка не открыла створку Г1-2.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(2, -63)), "Код энергосекции не открыл дверь.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -80)), "Пятнашки лаборатории не открыли створку Г2-А.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -82)), "Щит рубильников не открыл створку Г2-Б.");
                Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -109)), "Код камеры не открыл дверь к ядру.");
            });

            var wall = EntityAt("ADTHeliosWall", new Vector2i(-12, -50));
            Assert.That(wall, Is.Not.Null);
            var dmg = new DamageSpecifier(protoMan.Index<DamageTypePrototype>("Blunt"), 100000);
            damageable.TryChangeDamage(wall!.Value, dmg, ignoreResistances: true);
            Assert.That(_entities.EntityExists(wall.Value), "Неразрушимую стену удалось уничтожить.");
            Assert.That(_entities.HasComponent<ADTIndestructibleComponent>(wall.Value));

        });

        EntityUid holder = default;
        await server.WaitAssertion(() =>
        {
            var plate = _entities.EntityQuery<ADTHeliosHoldPlateComponent>().Single().Owner;
            holder = _entities.SpawnEntity("MobHuman", _entities.GetComponent<TransformComponent>(plate).Coordinates);
        });
        await RunSeconds(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -91)), "Плита удержания не открыла шлюз в кольцо камеры.");
            _entities.DeleteEntity(holder);
        });
        await RunSeconds(3);
        await server.WaitAssertion(() =>
        {
            Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -91)), Is.False, "Шлюз не закрылся, когда человек сошёл с плиты.");
            var btn = EntityAt("ADTHeliosButton", new Vector2i(-5, -92));
            Assert.That(btn, Is.Not.Null, "Нет аварийной разблокировки.");
            link.InvokePort(btn!.Value, "Pressed");

            var keys = _entities.EntityQuery<ADTHeliosDualKeyComponent>().Select(k => (k.Owner, k)).ToList();
            Assert.That(keys, Has.Count.EqualTo(2), "Пультов двух ключей должно быть два.");
            mechanics.TurnKey((keys[0].Owner, keys[0].k));
            Assert.That(keys[0].k.Finished, Is.False, "Один ключ не должен открывать спуск.");
            mechanics.TurnKey((keys[1].Owner, keys[1].k));
        });
        await RunSeconds(3);
        await server.WaitAssertion(() =>
        {
            Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -91)), "Аварийная разблокировка не удержала шлюз открытым.");
            Assert.That(IsOpen("ADTHeliosBlastDoor", new Vector2i(0, -114)), "Два ключа не открыли спуск к ядру.");
        });

        await RunSeconds(40);
        await server.WaitAssertion(() =>
        {
            var lights = 0;
            var powered = 0;
            var q = _entities.EntityQueryEnumerator<PoweredLightComponent, ApcPowerReceiverComponent, TransformComponent>();
            while (q.MoveNext(out _, out _, out var receiver, out var xform))
            {
                if (xform.MapUid != complexMap || receiver.Load <= 0)
                    continue;

                lights++;
                if (receiver.Powered)
                    powered++;
            }

            Assert.That(lights, Is.GreaterThan(40), "Слишком мало ламп в комплексе.");
            Assert.That(powered, Is.EqualTo(lights), $"Запитано {powered} ламп из {lights}.");

            var gridGravity = _entities.EntityQuery<GravityComponent>()
                .FirstOrDefault(g => _entities.GetComponent<TransformComponent>(g.Owner).MapUid == complexMap);
            Assert.That(gridGravity, Is.Not.Null);
            Assert.That(gridGravity!.Enabled, "Генератор гравитации в техотсеке не включился.");

            var batteries = _entities.System<Content.Shared.Power.EntitySystems.SharedBatterySystem>();
            var cells = 0;
            var bq = _entities.EntityQueryEnumerator<Content.Shared.Power.Components.BatterySelfRechargerComponent, Content.Shared.Power.Components.BatteryComponent, TransformComponent>();
            while (bq.MoveNext(out var uid, out var recharger, out var battery, out var xform))
            {
                if (xform.MapUid != complexMap || recharger.AutoRechargeRate < 1e6f)
                    continue;

                cells++;
                Assert.That(batteries.GetChargeLevel((uid, battery)), Is.GreaterThan(0.99f), $"Батарея {_entities.ToPrettyString(uid)} не заряжена.");
                batteries.SetCharge((uid, battery), 0);
            }
            Assert.That(cells, Is.GreaterThanOrEqualTo(30), "В комплексе слишком мало бесконечных батарей.");
        });

        await RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            var batteries = _entities.System<Content.Shared.Power.EntitySystems.SharedBatterySystem>();
            var bq = _entities.EntityQueryEnumerator<Content.Shared.Power.Components.BatterySelfRechargerComponent, Content.Shared.Power.Components.BatteryComponent, TransformComponent>();
            while (bq.MoveNext(out var uid, out var recharger, out var battery, out var xform))
            {
                if (xform.MapUid == complexMap && recharger.AutoRechargeRate >= 1e6f)
                    Assert.That(batteries.GetChargeLevel((uid, battery)), Is.GreaterThan(0.99f), $"Батарея {_entities.ToPrettyString(uid)} не восполнилась после разрядки.");
            }

            mapSystem.DeleteMap(_entities.GetComponent<MapComponent>(complexMap).MapId);
        });

        await RunTicksSync(5);

        EntityUid entrance = default;
        await server.WaitAssertion(() =>
        {
            mapSystem.CreateMap(out var mapId);
            var grid = mapSystem.CreateGridEntity(mapId);
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            entrance = _entities.SpawnEntity("ADTHeliosGatewayEntrance", new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
        });

        await RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(_entities.TryGetComponent<LinkedEntityComponent>(entrance, out var linked), "Вход не связался с выходом.");
            Assert.That(linked!.LinkedEntities, Has.Count.EqualTo(1));
            var exit = linked.LinkedEntities.First();
            Assert.That(_entities.HasComponent<ADTDungeonGatewayExitComponent>(exit));
            Assert.That(_entities.GetComponent<TransformComponent>(exit).MapUid,
                Is.Not.EqualTo(_entities.GetComponent<TransformComponent>(entrance).MapUid),
                "Выход должен быть на отдельной карте.");
        });
    }
}
