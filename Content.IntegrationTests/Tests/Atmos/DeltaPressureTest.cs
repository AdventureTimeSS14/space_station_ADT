<<<<<<< HEAD
using System.Linq;
=======
>>>>>>> upstreamwiz/master
using System.Numerics;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
<<<<<<< HEAD
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
=======
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
>>>>>>> upstreamwiz/master
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Tests for AtmosphereSystem.DeltaPressure and surrounding systems
/// handling the DeltaPressureComponent.
/// </summary>
[TestFixture]
[TestOf(typeof(DeltaPressureSystem))]
<<<<<<< HEAD
public sealed class DeltaPressureTest
=======
public sealed class DeltaPressureTest : AtmosTest
>>>>>>> upstreamwiz/master
{
    #region Prototypes

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: BaseStructure
  id: DeltaPressureSolidTest
  placement:
    mode: SnapgridCenter
    snap:
    - Wall
  components:
  - type: Physics
    bodyType: Static
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeAabb
          bounds: ""-0.5,-0.5,0.5,0.5""
        mask:
        - FullTileMask
        layer:
        - WallLayer
        density: 1000
  - type: Airtight
  - type: DeltaPressure
    minPressure: 15000
    minPressureDelta: 10000
    scalingType: Threshold
    baseDamage:
      types:
        Structural: 1000
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 300
      behaviors:
      - !type:SpawnEntitiesBehavior
        spawn:
          Girder:
            min: 1
            max: 1
      - !type:DoActsBehavior
        acts: [ ""Destruction"" ]

- type: entity
  parent: DeltaPressureSolidTest
  id: DeltaPressureSolidTestNoAutoJoin
  components:
  - type: DeltaPressure
    autoJoinProcessingList: false

- type: entity
  parent: DeltaPressureSolidTest
  id: DeltaPressureSolidTestAbsolute
  components:
  - type: DeltaPressure
    minPressure: 10000
    minPressureDelta: 15000
    scalingType: Threshold
    baseDamage:
      types:
        Structural: 1000
";

    #endregion

<<<<<<< HEAD
    private readonly ResPath _testMap = new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");
=======
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");
>>>>>>> upstreamwiz/master

    // TODO ATMOS TESTS
    // - Check for directional windows (partial airtight ents) properly computing pressure differences
    // - Check for multi-tick damage (window with n damage threshold should take n ticks to destroy)
    // - Check that all maps do not explode into a million pieces on load due to dP
    // - Ensure that all tests work for a map that has an origin at a non zero coordinate

    /// <summary>
    /// Asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to true is automatically added to the DeltaPressure processing list
    /// on the grid's GridAtmosphereComponent.
    ///
    /// Also asserts that an entity with a DeltaPressureComponent with autoJoinProcessingList
    /// set to false is not automatically added to the DeltaPressure processing list.
    /// </summary>
    [Test]
    public async Task ProcessingListAutoJoinTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<DeltaPressureComponent> dpEnt;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045

            grid = gridSet.First();
        });

        await server.WaitAssertion(() =>
        {
            var uid = entMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(grid.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));

            Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was not in processing list when it should have automatically joined!");
            entMan.DeleteEntity(uid);
            Assert.That(!atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was still in processing list after deletion!");
        });

        await pair.CleanReturnAsync();
=======
        await Server.WaitAssertion(() =>
        {
            var uid = SEntMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(ProcessEnt, Vector2.Zero));
            var dpEnt = new Entity<DeltaPressureComponent>(uid, SEntMan.GetComponent<DeltaPressureComponent>(uid));

            Assert.That(SAtmos.IsDeltaPressureEntityInList(RelevantAtmos, dpEnt), "Entity was not in processing list when it should have automatically joined!");
            SEntMan.DeleteEntity(uid);
            Assert.That(!SAtmos.IsDeltaPressureEntityInList(RelevantAtmos, dpEnt), "Entity was still in processing list after deletion!");
        });
>>>>>>> upstreamwiz/master
    }

    /// <summary>
    /// Asserts that an entity that doesn't need to be damaged by DeltaPressure
    /// is not damaged by DeltaPressure.
    /// </summary>
    [Test]
    public async Task ProcessingDeltaStandbyTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
=======
>>>>>>> upstreamwiz/master
        Entity<DeltaPressureComponent> dpEnt = default;
        TileAtmosphere tile = null!;
        AtmosDirection direction = default;

        // Load our test map in and assert that it exists.
<<<<<<< HEAD
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045

            grid = gridSet.First();
            var uid = entMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(grid.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));
            Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was not in processing list when it should have been added!");
=======
        await Server.WaitPost(() =>
        {
            var uid = SEntMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(ProcessEnt, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, SEntMan.GetComponent<DeltaPressureComponent>(uid));
            Assert.That(SAtmos.IsDeltaPressureEntityInList(ProcessEnt, dpEnt), "Entity was not in processing list when it should have been added!");
>>>>>>> upstreamwiz/master
        });

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
<<<<<<< HEAD
            await server.WaitPost(() =>
            {
                var indices = transformSystem.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = entMan.GetComponent<GridAtmosphereComponent>(grid);

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                tile = gridAtmosComp.Tiles[offsetIndices];
=======
            await Server.WaitPost(() =>
            {
                var indices = Transform.GetGridOrMapTilePosition(dpEnt);

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                tile = RelevantAtmos.Comp.Tiles[offsetIndices];
>>>>>>> upstreamwiz/master

                Assert.That(tile.Air, Is.Not.Null, $"Tile at {offsetIndices} should have air!");

                var toPressurize = dpEnt.Comp!.MinPressureDelta - 10;
                var moles = (toPressurize * tile.Air.Volume) / (Atmospherics.R * Atmospherics.T20C);

                tile.Air!.AdjustMoles(Gas.Nitrogen, moles);
            });

<<<<<<< HEAD
            await server.WaitRunTicks(30);

            // Entity should exist, if it took one tick of damage then it should be instantly destroyed.
            await server.WaitAssertion(() =>
            {
                Assert.That(!entMan.Deleted(dpEnt), $"{dpEnt} should still exist after experiencing non-threshold pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await server.WaitRunTicks(30);
        }

        await pair.CleanReturnAsync();
=======
            await Server.WaitRunTicks(30);

            // Entity should exist, if it took one tick of damage then it should be instantly destroyed.
            await Server.WaitAssertion(() =>
            {
                Assert.That(!SEntMan.Deleted(dpEnt), $"{dpEnt} should still exist after experiencing non-threshold pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await Server.WaitRunTicks(30);
        }
>>>>>>> upstreamwiz/master
    }

    /// <summary>
    /// Asserts that an entity that needs to be damaged by DeltaPressure
    /// is damaged by DeltaPressure when the pressure is above the threshold.
    /// </summary>
    [Test]
    public async Task ProcessingDeltaDamageTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
        Entity<DeltaPressureComponent> dpEnt = default;
        TileAtmosphere tile = null!;
        AtmosDirection direction = default;

        // Load our test map in and assert that it exists.
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045

            grid = gridSet.First();
=======
        Entity<DeltaPressureComponent> dpEnt = default;
        AtmosDirection direction = default;

        // Load our test map in and assert that it exists.
        await Server.WaitPost(() =>
        {
            SAtmos.SetAtmosphereSimulation(ProcessEnt, false);
>>>>>>> upstreamwiz/master
        });

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
<<<<<<< HEAD
            await server.WaitPost(() =>
            {
                // Need to spawn an entity each run to ensure it works for all directions.
                var uid = entMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(grid.Owner, Vector2.Zero));
                dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));
                Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was not in processing list when it should have been added!");

                var indices = transformSystem.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = entMan.GetComponent<GridAtmosphereComponent>(grid);

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                tile = gridAtmosComp.Tiles[offsetIndices];
=======
            /*
             RUNNING REGULAR TICKS USING WaitRunTicks AND GUESSING AS TO HOW MANY ATMOS SIMULATION TICKS ARE HAPPENING
             WILL CAUSE A RACE CONDITION THAT IS A PAIN IN THE ASS TO DEBUG

             AN ENTITY MAY BE REMOVED AND ADDED BETWEEN A SUBTICK. IF LINDA PROCESSING IS ENABLED IT MIGHT CAUSE
             AN EQUALIZATION TO PUT AIR IN OTHER TILES IN THE SMALL WINDOW WHERE THE TILE IS NOT AIRTIGHT
             WHICH WILL THROW OFF DELTAS
             */

            await Server.WaitPost(() =>
            {
                SAtmos.RunProcessingFull(ProcessEnt,ProcessEnt.Owner, SAtmos.AtmosTickRate);
            });

            await Server.WaitPost(() =>
            {
                // Need to spawn an entity each run to ensure it works for all directions.
                var uid = SEntMan.SpawnAtPosition("DeltaPressureSolidTest", new EntityCoordinates(ProcessEnt.Owner, Vector2.Zero));
                dpEnt = new Entity<DeltaPressureComponent>(uid, SEntMan.GetComponent<DeltaPressureComponent>(uid));
                Assert.That(SAtmos.IsDeltaPressureEntityInList(ProcessEnt.Owner, dpEnt), "Entity was not in processing list when it should have been added!");

                var indices = Transform.GetGridOrMapTilePosition(dpEnt);

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                var tile = ProcessEnt.Comp1.Tiles[offsetIndices];
>>>>>>> upstreamwiz/master

                Assert.That(tile.Air, Is.Not.Null, $"Tile at {offsetIndices} should have air!");

                var toPressurize = dpEnt.Comp!.MinPressureDelta + 10;
                var moles = (toPressurize * tile.Air.Volume) / (Atmospherics.R * Atmospherics.T20C);

                tile.Air!.AdjustMoles(Gas.Nitrogen, moles);
            });

<<<<<<< HEAD
            await server.WaitRunTicks(30);

            // Entity should exist, if it took one tick of damage then it should be instantly destroyed.
            await server.WaitAssertion(() =>
            {
                Assert.That(entMan.Deleted(dpEnt), $"{dpEnt} still exists after experiencing threshold pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await server.WaitRunTicks(30);
        }

        await pair.CleanReturnAsync();
=======
            // get jiggy with it! hit that dance white boy!
            await Server.WaitPost(() =>
            {
                SAtmos.RunProcessingFull(ProcessEnt,ProcessEnt.Owner, SAtmos.AtmosTickRate);
            });

            // need to run some ticks as deleted entities are queued for removal
            // and not removed instantly
            await Server.WaitRunTicks(30);

            // Entity shouldn't exist, if it took one tick of damage then it should be instantly destroyed.
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.Deleted(dpEnt), $"{dpEnt} still exists after experiencing threshold pressure from {direction} side!");

                // Double whammy: in case any unintended gas leak occured due to a race condition,
                // clear out all the tiles.
                foreach (var mix in SAtmos.GetAllMixtures(ProcessEnt))
                {
                    mix.Clear();
                }
            });
        }
>>>>>>> upstreamwiz/master
    }

    /// <summary>
    /// Asserts that an entity that doesn't need to be damaged by DeltaPressure
    /// is not damaged by DeltaPressure when using absolute pressure thresholds.
    /// </summary>
    [Test]
    public async Task ProcessingAbsoluteStandbyTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
=======
>>>>>>> upstreamwiz/master
        Entity<DeltaPressureComponent> dpEnt = default;
        TileAtmosphere tile = null!;
        AtmosDirection direction = default;

<<<<<<< HEAD
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045
            grid = gridSet.First();
            var uid = entMan.SpawnAtPosition("DeltaPressureSolidTestAbsolute", new EntityCoordinates(grid.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));
            Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was not in processing list when it should have been added!");
=======
        await Server.WaitPost(() =>
        {
            var uid = SEntMan.SpawnAtPosition("DeltaPressureSolidTestAbsolute", new EntityCoordinates(ProcessEnt.Owner, Vector2.Zero));
            dpEnt = new Entity<DeltaPressureComponent>(uid, SEntMan.GetComponent<DeltaPressureComponent>(uid));
            Assert.That(SAtmos.IsDeltaPressureEntityInList(ProcessEnt.Owner, dpEnt), "Entity was not in processing list when it should have been added!");
>>>>>>> upstreamwiz/master
        });

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
<<<<<<< HEAD
            await server.WaitPost(() =>
            {
                var indices = transformSystem.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = entMan.GetComponent<GridAtmosphereComponent>(grid);
=======
            await Server.WaitPost(() =>
            {
                var indices = Transform.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = SEntMan.GetComponent<GridAtmosphereComponent>(ProcessEnt);
>>>>>>> upstreamwiz/master

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                tile = gridAtmosComp.Tiles[offsetIndices];
                Assert.That(tile.Air, Is.Not.Null, $"Tile at {offsetIndices} should have air!");

                var toPressurize = dpEnt.Comp!.MinPressure - 10; // just below absolute threshold
                var moles = (toPressurize * tile.Air.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air!.AdjustMoles(Gas.Nitrogen, moles);
            });

<<<<<<< HEAD
            await server.WaitRunTicks(30);

            await server.WaitAssertion(() =>
            {
                Assert.That(!entMan.Deleted(dpEnt), $"{dpEnt} should still exist after experiencing non-threshold absolute pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await server.WaitRunTicks(30);
        }

        await pair.CleanReturnAsync();
=======
            await Server.WaitRunTicks(30);

            await Server.WaitAssertion(() =>
            {
                Assert.That(!SEntMan.Deleted(dpEnt), $"{dpEnt} should still exist after experiencing non-threshold absolute pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await Server.WaitRunTicks(30);
        }
>>>>>>> upstreamwiz/master
    }

    /// <summary>
    /// Asserts that an entity that needs to be damaged by DeltaPressure
    /// is damaged by DeltaPressure when the pressure is above the absolute threshold.
    /// </summary>
    [Test]
    public async Task ProcessingAbsoluteDamageTest()
    {
<<<<<<< HEAD
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.EntMan;
        var mapLoader = entMan.System<MapLoaderSystem>();
        var atmosphereSystem = entMan.System<AtmosphereSystem>();
        var transformSystem = entMan.System<SharedTransformSystem>();
        var deserializationOptions = DeserializationOptions.Default with { InitializeMaps = true };

        Entity<MapGridComponent> grid = default;
=======
>>>>>>> upstreamwiz/master
        Entity<DeltaPressureComponent> dpEnt = default;
        TileAtmosphere tile = null!;
        AtmosDirection direction = default;

<<<<<<< HEAD
        await server.WaitPost(() =>
        {
#pragma warning disable NUnit2045
            Assert.That(mapLoader.TryLoadMap(_testMap, out _, out var gridSet, deserializationOptions),
                $"Failed to load map {_testMap}.");
            Assert.That(gridSet, Is.Not.Null, "There were no grids loaded from the map!");
#pragma warning restore NUnit2045
            grid = gridSet.First();
        });

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            await server.WaitPost(() =>
            {
                // Spawn fresh entity each iteration to verify all directions work
                var uid = entMan.SpawnAtPosition("DeltaPressureSolidTestAbsolute", new EntityCoordinates(grid.Owner, Vector2.Zero));
                dpEnt = new Entity<DeltaPressureComponent>(uid, entMan.GetComponent<DeltaPressureComponent>(uid));
                Assert.That(atmosphereSystem.IsDeltaPressureEntityInList(grid.Owner, dpEnt), "Entity was not in processing list when it should have been added!");

                var indices = transformSystem.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = entMan.GetComponent<GridAtmosphereComponent>(grid);
=======
        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            await Server.WaitPost(() =>
            {
                // Spawn fresh entity each iteration to verify all directions work
                var uid = SEntMan.SpawnAtPosition("DeltaPressureSolidTestAbsolute", new EntityCoordinates(ProcessEnt.Owner, Vector2.Zero));
                dpEnt = new Entity<DeltaPressureComponent>(uid, SEntMan.GetComponent<DeltaPressureComponent>(uid));
                Assert.That(SAtmos.IsDeltaPressureEntityInList(ProcessEnt.Owner, dpEnt), "Entity was not in processing list when it should have been added!");

                var indices = Transform.GetGridOrMapTilePosition(dpEnt);
                var gridAtmosComp = SEntMan.GetComponent<GridAtmosphereComponent>(ProcessEnt);
>>>>>>> upstreamwiz/master

                direction = (AtmosDirection)(1 << i);
                var offsetIndices = indices.Offset(direction);
                tile = gridAtmosComp.Tiles[offsetIndices];
                Assert.That(tile.Air, Is.Not.Null, $"Tile at {offsetIndices} should have air!");

                // Above absolute threshold but below delta threshold to ensure absolute alone causes damage
                var toPressurize = dpEnt.Comp!.MinPressure + 10;
                var moles = (toPressurize * tile.Air.Volume) / (Atmospherics.R * Atmospherics.T20C);
                tile.Air!.AdjustMoles(Gas.Nitrogen, moles);
            });

<<<<<<< HEAD
            await server.WaitRunTicks(30);

            await server.WaitAssertion(() =>
            {
                Assert.That(entMan.Deleted(dpEnt), $"{dpEnt} still exists after experiencing threshold absolute pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await server.WaitRunTicks(30);
        }

        await pair.CleanReturnAsync();
=======
            await Server.WaitRunTicks(30);

            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.Deleted(dpEnt), $"{dpEnt} still exists after experiencing threshold absolute pressure from {direction} side!");
                tile.Air!.Clear();
            });

            await Server.WaitRunTicks(30);
        }
>>>>>>> upstreamwiz/master
    }
}
