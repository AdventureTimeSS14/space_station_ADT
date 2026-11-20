using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server._Mono.FireControl;
using Content.Shared._Crescent.ShipShields;
using Content.Shared._Mono.FireControl;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.ADT.ShipWeapons;

/// <summary>
/// Checks the ship weapons ported from Monolith: a gunnery server picks up the turrets of its grid,
/// a gunnery console connects to it and fires them, a shield generator shields its grid.
/// </summary>
[TestFixture]
[TestOf(typeof(FireControlSystem))]
public sealed class FireControlTest : GameTest
{
    private const string ServerProto = "GunneryServerHigh";
    private const string ConsoleProto = "ComputerGunneryConsole";
    private const string TurretProto = "WeaponTurretAK570";
    private const string ShieldGeneratorProto = "ShieldGeneratorSmall";

    private Entity<MapGridComponent> _grid;

    private async Task CreateShip()
    {
        var map = await Pair.CreateTestMap();

        await Pair.Server.WaitPost(() =>
        {
            var mapSystem = SEntMan.System<SharedMapSystem>();
            _grid = map.Grid;

            var tiles = new List<(Vector2i, Tile)>();
            for (var x = -4; x <= 4; x++)
            {
                for (var y = -4; y <= 4; y++)
                {
                    tiles.Add((new Vector2i(x, y), map.Tile.Tile));
                }
            }

            mapSystem.SetTiles(_grid.Owner, _grid.Comp, tiles);
        });
    }

    private EntityUid SpawnPowered(string id, Vector2 position)
    {
        var uid = SSpawnAtPosition(id, new EntityCoordinates(_grid, position));
        SEntMan.System<SharedPowerReceiverSystem>().SetNeedsPower(uid, false);
        return uid;
    }

    [Test]
    public async Task ConsoleFiresServerWeapons()
    {
        await CreateShip();

        EntityUid server = default, console = default, turret = default;
        await Pair.Server.WaitPost(() =>
        {
            turret = SSpawnAtPosition(TurretProto, new EntityCoordinates(_grid, new Vector2(-3.5f, 3.5f)));
            server = SpawnPowered(ServerProto, new Vector2(0.5f, -3.5f));
            console = SpawnPowered(ConsoleProto, new Vector2(1.5f, -3.5f));
        });

        await RunTicksSync(10);

        var serverComp = SComp<FireControlServerComponent>(server);
        var fireControl = SEntMan.System<FireControlSystem>();
        Assert.Multiple(() =>
        {
            Assert.That(serverComp.Controlled, Does.Contain(turret), "The server didn't pick up the turret of its grid");
            Assert.That(serverComp.UsedProcessingPower, Is.EqualTo(fireControl.GetProcessingPowerCost(turret)));
            Assert.That(SComp<FireControlConsoleComponent>(console).ConnectedServer, Is.EqualTo(server));
        });

        await Pair.Server.WaitAssertion(() =>
        {
            var containers = SEntMan.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(turret, "gun_magazine", out var slot), "The turret has no magazine slot");
            var magazine = slot!.ContainedEntities.Single();
            Assert.That(SComp<BallisticAmmoProviderComponent>(magazine).InfiniteUnspawned, "Ship ammo loaders should never run dry");
        });

        await Pair.Server.WaitPost(() =>
        {
            // straight up from the turret, away from everything on the grid
            var target = SEntMan.GetNetCoordinates(new EntityCoordinates(_grid, new Vector2(-3.5f, 80f)));
            fireControl.FireWeapons(server, [SEntMan.GetNetEntity(turret)], target);
        });

        await RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(SComp<FireControllableComponent>(turret).LastShot, Is.GreaterThan(TimeSpan.Zero),
                "The turret didn't fire through the gunnery server");
            Assert.That(ShotsFiredBy(turret), Is.GreaterThan(0), "No shell left the turret");
        });
    }

    private int ShotsFiredBy(EntityUid gun)
    {
        var count = 0;
        var query = SEntMan.EntityQueryEnumerator<ProjectileComponent>();
        while (query.MoveNext(out _, out var projectile))
        {
            if (projectile.Weapon == gun)
                count++;
        }

        return count;
    }

    [Test]
    public async Task ServerReleasesWeaponsWhenRemoved()
    {
        await CreateShip();

        EntityUid server = default, turret = default;
        await Pair.Server.WaitPost(() =>
        {
            turret = SSpawnAtPosition(TurretProto, new EntityCoordinates(_grid, new Vector2(-3.5f, 3.5f)));
            server = SpawnPowered(ServerProto, new Vector2(0.5f, -3.5f));
        });

        await RunTicksSync(10);
        Assert.That(SComp<FireControlServerComponent>(server).Controlled, Does.Contain(turret));

        // the server has a backup battery, so losing the power net isn't enough
        await Pair.Server.WaitPost(() => SDeleteNow(server));

        await RunTicksSync(10);

        Assert.That(SComp<FireControllableComponent>(turret).ControllingServer, Is.Null);
    }

    [Test]
    public async Task ShieldGeneratorShieldsGrid()
    {
        await CreateShip();

        EntityUid generator = default;
        await Pair.Server.WaitPost(() => generator = SpawnPowered(ShieldGeneratorProto, new Vector2(0.5f, 0.5f)));

        // the emitter updates every 1.5 seconds
        await RunSeconds(2);

        var emitter = SComp<ShipShieldEmitterComponent>(generator);
        Assert.Multiple(() =>
        {
            Assert.That(emitter.Shield, Is.Not.Null, "The generator didn't raise a shield");
            Assert.That(SEntMan.HasComponent<ShipShieldedComponent>(_grid), "The grid isn't marked as shielded");
        });

        // overload it
        await Pair.Server.WaitPost(() => emitter.Damage = emitter.DamageLimit * 2);
        await RunSeconds(2);

        Assert.Multiple(() =>
        {
            Assert.That(emitter.Shield, Is.Null, "An overloaded generator kept its shield up");
            Assert.That(SEntMan.HasComponent<ShipShieldedComponent>(_grid), Is.False);
        });
    }
}
