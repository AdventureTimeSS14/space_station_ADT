using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Botany.Components;
using Content.Server.Construction.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.Lathe;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT.Construction.Interaction;

public sealed class MachinePartInteractionTests : InteractionTest
{
    private const string MachineFrame = "MachineFrame";
    private const string Unfinished = "UnfinishedMachineFrame";
    private const string Protolathe = "Protolathe";
    private const string Autolathe = "Autolathe";
    private const string ProtolatheBoard = "ProtolatheMachineCircuitboard";
    private const string AutolatheBoard = "AutolatheMachineCircuitboard";
    private const string SeedExtractor = "SeedExtractor";
    private const string Beaker = "Beaker";

    [Test]
    public async Task RpedCanCompleteAutolatheFromMachineFrame()
    {
        await PrepareMachineFrame();
        await PlaceInHands((RapidPartExchanger, 1));
        var rped = await GetActiveHeldItemNetEntity();
        await FillRped(rped, AutolatheBoard, MatterBin1, MatterBin1, MatterBin1, Servo1, ("SheetGlass", 2));

        await Interact();
        await AwaitDoAfters();

        Assert.That(Comp<MachineFrameComponent>().HasBoard, Is.True);
        var frame = Comp<MachineFrameComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(CountMachinePart(frame.PartContainer, MachinePartIds.MatterBin), Is.EqualTo(3));
            Assert.That(CountMachinePart(frame.PartContainer, MachinePartIds.Servo), Is.EqualTo(1));
        });
        AssertNoOverfilledRequirements(frame);

        await InteractUsing(Screw);
        AssertPrototype(Autolathe);

        var machine = Comp<MachineComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(CountMachinePart(machine, MachinePartIds.MatterBin), Is.EqualTo(3));
            Assert.That(CountMachinePart(machine, MachinePartIds.Servo), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task BluespaceRpedCanCompleteProtolatheFromMachineFrame()
    {
        await PrepareMachineFrame();
        await PlaceInHands((BluespaceRapidPartExchanger, 1));
        var rped = await GetActiveHeldItemNetEntity();
        await FillRped(rped, ProtolatheBoard, Servo1, Servo1, MatterBin1, MatterBin1, Beaker, Beaker);

        await Interact();
        await AwaitDoAfters();

        var frame = Comp<MachineFrameComponent>();
        AssertNoOverfilledRequirements(frame);

        await InteractUsing(Screw);
        AssertPrototype(Protolathe);

        var machine = Comp<MachineComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(CountMachinePart(machine, MachinePartIds.MatterBin), Is.EqualTo(2));
            Assert.That(CountMachinePart(machine, MachinePartIds.Servo), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task RpedUpgradesProtolatheSpeedAndCost()
    {
        await SpawnTarget(Protolathe);
        await InteractUsing(Screw);

        var latheBefore = Comp<LatheComponent>();
        var baseTime = latheBefore.FinalTimeMultiplier;
        var baseMaterial = latheBefore.FinalMaterialMultiplier;

        await PlaceInHands((RapidPartExchanger, 1));
        var rped = await GetActiveHeldItemNetEntity();
        await FillRped(rped, Servo4, MatterBin4);

        await Interact();
        await AwaitDoAfters();

        var machine = Comp<MachineComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(CountMachinePart(machine, MachinePartIds.Servo, 4f), Is.EqualTo(1));
            Assert.That(CountMachinePart(machine, MachinePartIds.MatterBin, 4f), Is.EqualTo(1));
        });

        await AssertRpedContains(rped, Servo1, 1);
        await AssertRpedContains(rped, MatterBin1, 1);

        var latheAfter = Comp<LatheComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(latheAfter.FinalTimeMultiplier, Is.LessThan(baseTime - 0.0001f));
            Assert.That(latheAfter.FinalMaterialMultiplier, Is.LessThan(baseMaterial - 0.0001f));
        });
    }

    [Test]
    public async Task RpedDoesNotDowngradeInstalledMachineParts()
    {
        await SpawnTarget(Protolathe);
        await InteractUsing(Screw);

        await PlaceInHands((RapidPartExchanger, 1));
        var upgrader = await GetActiveHeldItemNetEntity();
        await FillRped(upgrader, Servo4, MatterBin4);
        await Interact();
        await AwaitDoAfters();

        var latheBefore = Comp<LatheComponent>();
        var upgradedTime = latheBefore.FinalTimeMultiplier;
        var upgradedMaterial = latheBefore.FinalMaterialMultiplier;

        await PlaceInHands((RapidPartExchanger, 1));
        var downgradeRped = await GetActiveHeldItemNetEntity();
        await FillRped(downgradeRped, Servo1, MatterBin1);
        await Interact();
        await AwaitDoAfters();

        var machine = Comp<MachineComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(CountMachinePart(machine, MachinePartIds.Servo, 4f), Is.EqualTo(1));
            Assert.That(CountMachinePart(machine, MachinePartIds.MatterBin, 4f), Is.EqualTo(1));
        });

        await AssertRpedContains(downgradeRped, Servo1, 1);
        await AssertRpedContains(downgradeRped, MatterBin1, 1);

        var latheAfter = Comp<LatheComponent>();
        Assert.Multiple(() =>
        {
            Assert.That(latheAfter.FinalTimeMultiplier, Is.EqualTo(upgradedTime).Within(0.0001f));
            Assert.That(latheAfter.FinalMaterialMultiplier, Is.EqualTo(upgradedMaterial).Within(0.0001f));
        });
    }

    [Test]
    public async Task RpedServoUpgradeChangesSeedExtractorMultiplier()
    {
        await SpawnTarget(SeedExtractor);
        await InteractUsing(Screw);

        var before = Comp<SeedExtractorComponent>();
        var baseMultiplier = before.SeedMultiplier;

        await PlaceInHands((RapidPartExchanger, 1));
        var rped = await GetActiveHeldItemNetEntity();
        await FillRped(rped, Servo4);
        await Interact();
        await AwaitDoAfters();

        var machine = Comp<MachineComponent>();
        Assert.That(CountMachinePart(machine, MachinePartIds.Servo, 4f), Is.EqualTo(1));
        await AssertRpedContains(rped, Servo1, 1);

        var after = Comp<SeedExtractorComponent>();
        Assert.That(after.SeedMultiplier, Is.GreaterThan(baseMultiplier));
        Assert.That(after.SeedMultiplier, Is.EqualTo(1.6f).Within(0.0001f));
    }

    [Test]
    public async Task DeconstructingUpgradedMachineReturnsInstalledHighTierParts()
    {
        await SpawnTarget(Protolathe);
        await InteractUsing(Screw);

        await PlaceInHands((RapidPartExchanger, 1));
        var rped = await GetActiveHeldItemNetEntity();
        await FillRped(rped, Servo4, MatterBin4);
        await Interact();
        await AwaitDoAfters();

        await InteractUsing(Pry);
        AssertPrototype(MachineFrame);
        await Interact(Pry, Cut);
        AssertPrototype(Unfinished);
        await Interact(Wrench, Screw);
        AssertDeleted();

        await AssertEntityLookup((Servo4, 1), (MatterBin4, 1), (Servo1, 1), (MatterBin1, 1), (Beaker, 2), (Steel, 5), (Cable, 1), (ProtolatheBoard, 1));
    }

    private async Task PrepareMachineFrame()
    {
        await StartConstruction(MachineFrame);
        await InteractUsing(Steel, 5);
        ClientAssertPrototype(Unfinished, Target);
        await Interact(Wrench, Cable);
        AssertPrototype(MachineFrame);
    }

    private async Task<NetEntity> GetActiveHeldItemNetEntity()
    {
        NetEntity result = default;
        await Server.WaitPost(() =>
        {
            var held = HandSys.GetActiveItem((ToServer(Player), Hands));
            Assert.That(held, Is.Not.Null);
            result = SEntMan.GetNetEntity(held!.Value);
        });

        return result;
    }

    private async Task FillRped(NetEntity rped, params object[] entries)
    {
        await Server.WaitPost(() =>
        {
            var storage = SEntMan.GetComponent<StorageComponent>(ToServer(rped));
            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case string prototype:
                        var entity = SEntMan.SpawnEntity(prototype, SEntMan.GetCoordinates(PlayerCoords));
                        SEntMan.System<SharedContainerSystem>().Insert(entity, storage.Container, force: true);
                        break;
                    case ValueTuple<string, int> stackEntry:
                        var stack = SEntMan.SpawnEntity(stackEntry.Item1, SEntMan.GetCoordinates(PlayerCoords));
                        var stackComp = SEntMan.GetComponent<Content.Shared.Stacks.StackComponent>(stack);
                        Stack.SetCount((stack, stackComp), stackEntry.Item2);
                        SEntMan.System<SharedContainerSystem>().Insert(stack, storage.Container, force: true);
                        break;
                }
            }
        });

        await RunTicks(1);
    }

    private static void AssertNoOverfilledRequirements(MachineFrameComponent frame)
    {
        foreach (var (type, required) in frame.MaterialRequirements)
        {
            var progress = frame.MaterialProgress.GetValueOrDefault(type);
            Assert.That(progress, Is.LessThanOrEqualTo(required), $"Material requirement overflow for {type}: {progress}/{required}");
        }

        foreach (var (type, required) in frame.PartRequirements)
        {
            var progress = frame.PartProgress.GetValueOrDefault(type);
            Assert.That(progress, Is.LessThanOrEqualTo(required), $"Part requirement overflow for {type}: {progress}/{required}");
        }

        foreach (var (name, info) in frame.ComponentRequirements)
        {
            var progress = frame.ComponentProgress.GetValueOrDefault(name);
            Assert.That(progress, Is.LessThanOrEqualTo(info.Amount), $"Component requirement overflow for {name}: {progress}/{info.Amount}");
        }

        foreach (var (tag, info) in frame.TagRequirements)
        {
            var progress = frame.TagProgress.GetValueOrDefault(tag);
            Assert.That(progress, Is.LessThanOrEqualTo(info.Amount), $"Tag requirement overflow for {tag}: {progress}/{info.Amount}");
        }
    }

    private int CountMachinePart(Container partContainer, ProtoId<MachinePartPrototype> partId, float? tier = null)
    {
        var count = 0;
        foreach (var uid in partContainer.ContainedEntities)
        {
            if (!SEntMan.TryGetComponent<MachinePartComponent>(uid, out var part))
                continue;

            if (part.Part != partId)
                continue;

            if (tier != null && Math.Abs(part.Tier - tier.Value) > 0.0001f)
                continue;

            count++;
        }

        return count;
    }

    private int CountMachinePart(MachineComponent machine, ProtoId<MachinePartPrototype> partId, float? tier = null)
    {
        var count = 0;
        if (!SEntMan.TryGetComponent<MachinePartStorageComponent>(machine.Owner, out var storage))
            return count;

        foreach (var slot in storage.Parts)
        {
            if (slot.Part != partId)
                continue;

            if (tier != null && Math.Abs(slot.Tier - tier.Value) > 0.0001f)
                continue;

            count += slot.Quantity;
        }

        return count;
    }

    private async Task AssertRpedContains(NetEntity rped, string prototype, int expectedCount)
    {
        await Server.WaitPost(() =>
        {
            var storage = SEntMan.GetComponent<StorageComponent>(ToServer(rped));
            var actual = storage.Container.ContainedEntities.Count(uid => Meta(uid).EntityPrototype?.ID == prototype);
            Assert.That(actual, Is.EqualTo(expectedCount));
        });
    }
}
