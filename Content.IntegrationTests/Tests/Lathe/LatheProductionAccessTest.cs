#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Lathe;
using Content.Shared.Lathe;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Lathe;

[TestOf(typeof(LatheSystem))]
public sealed class LatheProductionAccessTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly LatheSystem _lathe = null!;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestProductionLockLathe
  components:
  - type: Lathe
    productionLocked: true
  - type: AccessReader
    access:
    - [Research]
  - type: UserInterface
    interfaces:
      enum.LatheUiKey.Key:
        type: LatheBoundUserInterface

- type: entity
  id: TestProductionLockOperator
  components:
  - type: Access
    tags: [Research]
";

    [Test]
    [RunOnSide(Side.Server)]
    public void LockRestrictsVisitorsButNotAuthorizedOperators()
    {
        var machine = SSpawn("TestProductionLockLathe");
        var visitor = SSpawn(null);
        var authorized = SSpawn("TestProductionLockOperator");
        var component = SComp<LatheComponent>(machine);

        Assert.That(_lathe.CanUseProduction(machine, visitor), Is.False);
        Assert.That(_lathe.CanUseProduction(machine, authorized), Is.True);
        Assert.That(_lathe.TrySetProductionLocked(machine, visitor, false), Is.False);
        Assert.That(component.ProductionLocked, Is.True);

        Assert.That(_lathe.TrySetProductionLocked(machine, authorized, false), Is.True);
        Assert.That(_lathe.CanUseProduction(machine, visitor), Is.True);
        Assert.That(_lathe.TrySetProductionLocked(machine, visitor, true), Is.False);
        Assert.That(component.ProductionLocked, Is.False);

        Assert.That(_lathe.TrySetProductionLocked(machine, authorized, true), Is.True);
        Assert.That(_lathe.CanUseProduction(machine, visitor), Is.False);
        Assert.That(_lathe.CanUseProduction(machine, authorized), Is.True);
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void LockedLatheRejectsVisitorQueueMessages()
    {
        var machine = SSpawn("TestProductionLockLathe");
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
