#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Lathe;
using Content.Shared.Access.Components;
using Content.Shared.Lathe;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Lathe;

[TestOf(typeof(LatheSystem))]
public sealed class LatheProductionAccessTest : GameTest
{
    [SidedDependency(Side.Server)] private LatheSystem _lathe = null!;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestProductionAccessLathe
  components:
  - type: Lathe
  - type: AccessReader
    access:
    - [Research]
  - type: UserInterface
    interfaces:
      enum.LatheUiKey.Key:
        type: LatheBoundUserInterface

- type: entity
  id: TestProductionAccessOperator
  components:
  - type: Access
    tags: [Research]
";

    [Test]
    [RunOnSide(Side.Server)]
    public void ProductionRequiresAccess()
    {
        var machine = SSpawn("TestProductionAccessLathe");
        var visitor = SSpawn(null);
        var authorized = SSpawn("TestProductionAccessOperator");

        Assert.That(_lathe.CanUseProduction(machine, visitor), Is.False);
        Assert.That(_lathe.CanUseProduction(machine, authorized), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LathesWithoutAccessReadersRemainUnrestricted()
    {
        var machine = SSpawn("TestProductionAccessLathe");
        var visitor = SSpawn(null);
        SEntMan.RemoveComponent<AccessReaderComponent>(machine);
        Assert.That(_lathe.CanUseProduction(machine, visitor), Is.True);
    }

    [TestCase("Protolathe")]
    [TestCase("ProtolatheHyperConvection")]
    [TestCase("CircuitImprinter")]
    [TestCase("CircuitImprinterHyperConvection")]
    [RunOnSide(Side.Server)]
    public void RestrictedMachinesAllowRecipeBrowsing(string id)
    {
        var prototype = SProtoMan.Index<EntityPrototype>(id);
        var factory = Server.ResolveDependency<IComponentFactory>();
        Assert.That(prototype.HasComp<ActivatableUIRequiresAccessComponent>(factory), Is.False);
        Assert.That(prototype.HasComp<AccessReaderComponent>(factory), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void ProductionRejectsVisitorQueueMessages()
    {
        var machine = SSpawn("TestProductionAccessLathe");
        var visitor = SSpawn(null);
        var component = SComp<LatheComponent>(machine);
        var first = new LatheRecipeBatch("SheetSteel", 0, 1);
        var second = new LatheRecipeBatch("SheetSteel30", 0, 1);
        component.Queue.AddLast(first);
        component.Queue.AddLast(second);
        component.CurrentRecipe = "SheetSteel";

        SEntMan.EventBus.RaiseLocalEvent(machine, new LatheQueueRecipeMessage("SheetSteel", 1) { Actor = visitor });
        var delete = new LatheDeleteRequestMessage(0) { Actor = visitor };
        _lathe.OnLatheDeleteRequestMessage(machine, component, ref delete);
        var move = new LatheMoveRequestMessage(0, 1) { Actor = visitor };
        _lathe.OnLatheMoveRequestMessage(machine, component, ref move);
        var abort = new LatheAbortFabricationMessage { Actor = visitor };
        _lathe.OnLatheAbortFabricationMessage(machine, component, ref abort);

        Assert.That(component.Queue, Is.EqualTo(new[] { first, second }));
        Assert.That(component.CurrentRecipe!.Value.Id, Is.EqualTo("SheetSteel"));
        component.Queue.Clear();
        component.CurrentRecipe = null;
    }
}
