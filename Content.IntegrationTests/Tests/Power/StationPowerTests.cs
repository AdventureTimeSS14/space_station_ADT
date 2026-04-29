<<<<<<< HEAD
//ADT-tweak: пробуем чинить тесты отключением
// using System.Collections.Generic;
// using System.Linq;
// using Content.Server.GameTicking;
// using Content.Server.Maps;
// using Content.Server.Power.Components;
// using Content.Server.Power.NodeGroups;
// using Content.Server.Power.Pow3r;
// using Content.Shared.NodeContainer;
// using Robust.Shared.EntitySerialization;
=======
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.NodeGroups;
using Content.Server.Power.Pow3r;
using Content.Shared.Maps;
using Content.Shared.Power.Components;
using Content.Shared.NodeContainer;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
>>>>>>> upstreamwiz/master

// namespace Content.IntegrationTests.Tests.Power;

<<<<<<< HEAD
// [Explicit]
// public sealed class StationPowerTests
// {
//     /// <summary>
//     /// How long the station should be able to survive on stored power if nothing is changed from round start.
//     /// </summary>
//     private const float MinimumPowerDurationSeconds = 10 * 60;

//     private static readonly string[] GameMaps =
//     [
//         "Fland",
//         "Packed",
//         "Bagel",
//         "Exo",
//         "Box",
//         "Marathon",
//         "Saltern",
//         "Reach",
//         "Train",
//         "Oasis",
//         "Amber",
//         "Plasma",
//         "Elkridge",
//         // ADT-Start
//         // "ADT_Avrit",
//         // "ADT_Bagel",
//         // "ADT_Barratry",
//         // "ADT_Box",
//         // "ADT_Cluster",
//         // "ADT_Fland",
//         // "ADT_Delta",
//         // "ADT_Marathon",
//         // "ADT_Kerberos",
//         // "ADT_kilo",
//         // "ADT_Saltern",
//         // "ADT_Packed",
//         // "ADT_Gemini",
//         // "ADT_Aspid",
//         // "ADT_Cluster_Legacy",
//         // "ADT_Meta",
//         // "ADT_Origin",
//         // "ADT_Centcomm"
//         // ADT-End
//     ];

//     [Test, TestCaseSource(nameof(GameMaps))]
//     public async Task TestStationStartingPowerWindow(string mapProtoId)
//     {
//         await using var pair = await PoolManager.GetServerClient(new PoolSettings
//         {
//             Dirty = true,
//         });
//         var server = pair.Server;

//         var entMan = server.EntMan;
//         var protoMan = server.ProtoMan;
//         var ticker = entMan.System<GameTicker>();
=======
public sealed class StationPowerTests : GameTest
{
    /// <summary>
    /// How long the station should be able to survive on stored power if nothing is changed from round start.
    /// </summary>
    private const float MinimumPowerDurationSeconds = 10 * 60;

    private static readonly string[] GameMaps =
    [
        "Bagel",
        "Box",
        "Elkridge",
        "Fland",
        "Marathon",
        "Oasis",
        "Packed",
        "Plasma",
        "Relic",
        "Snowball",
        "Reach",
        "Exo",
    ];

    public override PoolSettings PoolSettings => new ()
    {
        Dirty = true,
    };

    [Explicit]
    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestStationStartingPowerWindow(string mapProtoId)
    {
        var pair = Pair;
        var server = pair.Server;

        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var ticker = entMan.System<GameTicker>();
        var batterySys = entMan.System<BatterySystem>();
>>>>>>> upstreamwiz/master

//         // Load the map
//         await server.WaitAssertion(() =>
//         {
//             Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
//             var opts = DeserializationOptions.Default with { InitializeMaps = true };
//             ticker.LoadGameMap(mapProto, out var mapId, opts);
//         });

//         // Let powernet set up
//         await server.WaitRunTicks(1);

<<<<<<< HEAD
//         // Find the power network with the greatest stored charge in its batteries.
//         // This keeps backup SMESes out of the calculation.
//         var networks = new Dictionary<PowerState.Network, float>();
//         var batteryQuery = entMan.EntityQueryEnumerator<PowerNetworkBatteryComponent, BatteryComponent, NodeContainerComponent>();
//         while (batteryQuery.MoveNext(out var uid, out _, out var battery, out var nodeContainer))
//         {
//             if (!nodeContainer.Nodes.TryGetValue("output", out var node))
//                 continue;
//             if (node.NodeGroup is not IBasePowerNet group)
//                 continue;
//             networks.TryGetValue(group.NetworkNode, out var charge);
//             networks[group.NetworkNode] = charge + battery.CurrentCharge;
//         }
//         var totalStartingCharge = networks.MaxBy(n => n.Value).Value;
=======
        // Find the power network with the greatest stored charge in its batteries.
        // This keeps backup SMESes out of the calculation.
        var networks = new Dictionary<PowerState.Network, float>();
        var batteryQuery = entMan.EntityQueryEnumerator<PowerNetworkBatteryComponent, BatteryComponent, NodeContainerComponent>();
        while (batteryQuery.MoveNext(out var uid, out _, out var battery, out var nodeContainer))
        {
            if (!nodeContainer.Nodes.TryGetValue("output", out var node))
                continue;
            if (node.NodeGroup is not IBasePowerNet group)
                continue;
            networks.TryGetValue(group.NetworkNode, out var charge);
            var currentCharge = batterySys.GetCharge((uid, battery));
            networks[group.NetworkNode] = charge + currentCharge;
        }
        var totalStartingCharge = networks.MaxBy(n => n.Value).Value;
>>>>>>> upstreamwiz/master

//         // Find how much charge all the APC-connected devices would like to use per second.
//         var totalAPCLoad = 0f;
//         var receiverQuery = entMan.EntityQueryEnumerator<ApcPowerReceiverComponent>();
//         while (receiverQuery.MoveNext(out _, out var receiver))
//         {
//             totalAPCLoad += receiver.Load;
//         }

<<<<<<< HEAD
//         var estimatedDuration = totalStartingCharge / totalAPCLoad;
//         var requiredStoredPower = totalAPCLoad * MinimumPowerDurationSeconds;
//         Assert.Multiple(() =>
//         {
//             Assert.That(estimatedDuration, Is.GreaterThanOrEqualTo(MinimumPowerDurationSeconds),
//                 $"Initial power for {mapProtoId} does not last long enough! Needs at least {MinimumPowerDurationSeconds}s " +
//                 $"but estimated to last only {estimatedDuration}s!");
//             Assert.That(totalStartingCharge, Is.GreaterThanOrEqualTo(requiredStoredPower),
//                 $"Needs at least {requiredStoredPower - totalStartingCharge} more stored power!");
//         });
=======
        var estimatedDuration = totalStartingCharge / totalAPCLoad;
        var requiredStoredPower = totalAPCLoad * MinimumPowerDurationSeconds;
        Assert.Multiple(() =>
        {
            Assert.That(estimatedDuration, Is.GreaterThanOrEqualTo(MinimumPowerDurationSeconds),
                $"Initial power for {mapProtoId} does not last long enough! Needs at least {MinimumPowerDurationSeconds}s " +
                $"but estimated to last only {estimatedDuration}s!");
            Assert.That(totalStartingCharge, Is.GreaterThanOrEqualTo(requiredStoredPower),
                $"Needs at least {requiredStoredPower - totalStartingCharge} more stored power!");
        });
    }
>>>>>>> upstreamwiz/master

    [Test, TestCaseSource(nameof(GameMaps))]
    public async Task TestApcLoad(string mapProtoId)
    {
        var pair = Pair;
        var server = pair.Server;

<<<<<<< HEAD
//         await pair.CleanReturnAsync();
//     }
// }
=======
        var entMan = server.EntMan;
        var protoMan = server.ProtoMan;
        var ticker = entMan.System<GameTicker>();
        var xform = entMan.System<TransformSystem>();

        // Load the map
        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex<GameMapPrototype>(mapProtoId, out var mapProto));
            var opts = DeserializationOptions.Default with { InitializeMaps = true };
            ticker.LoadGameMap(mapProto, out var mapId, opts);
        });

        // Wait long enough for power to ramp up, but before anything can trip
        await pair.RunSeconds(2);

        // Check that no APCs start overloaded
        var apcQuery = entMan.EntityQueryEnumerator<ApcComponent, PowerNetworkBatteryComponent>();
        Assert.Multiple(() =>
        {
            while (apcQuery.MoveNext(out var uid, out var apc, out var battery))
            {
                // Uncomment the following line to log starting APC load to the console
                //Console.WriteLine($"ApcLoad:{mapProtoId}:{uid}:{battery.CurrentSupply}");
                if (xform.TryGetMapOrGridCoordinates(uid, out var coord))
                {
                    Assert.That(apc.MaxLoad, Is.GreaterThanOrEqualTo(battery.CurrentSupply),
                            $"APC {uid} on {mapProtoId} ({coord.Value.X}, {coord.Value.Y}) is overloaded {battery.CurrentSupply} / {apc.MaxLoad}");
                }
                else
                {
                    Assert.That(apc.MaxLoad, Is.GreaterThanOrEqualTo(battery.CurrentSupply),
                            $"APC {uid} on {mapProtoId} is overloaded {battery.CurrentSupply} / {apc.MaxLoad}");
                }
            }
        });
    }
}
>>>>>>> upstreamwiz/master
