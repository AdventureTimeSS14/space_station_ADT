using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Destructible;
using Content.Shared.Destructible.Thresholds.Triggers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
[TestOf(typeof(DestructibleComponent))]
public sealed class DestructibleThresholdsOverrideTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: ADTDestructibleOverrideTestParent
  components:
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 50
      behaviors:
      - !type:DoActsBehavior
        acts: [ ""Destruction"" ]

- type: entity
  id: ADTDestructibleOverrideTestAdditive
  parent: ADTDestructibleOverrideTestParent
  components:
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 100
      behaviors:
      - !type:DoActsBehavior
        acts: [ ""Destruction"" ]

- type: entity
  id: ADTDestructibleOverrideTestOverride
  parent: ADTDestructibleOverrideTestParent
  components:
  - type: Destructible
    thresholdsOverride:
    - trigger:
        !type:DamageTrigger
        damage: 200
      behaviors:
      - !type:DoActsBehavior
        acts: [ ""Destruction"" ]

- type: entity
  id: ADTDestructibleOverrideTestOverrideChild
  parent: ADTDestructibleOverrideTestOverride
";

    private static int[] Damages(DestructibleComponent comp)
    {
        return comp.Thresholds
            .Select(t => ((DamageTrigger) t.Trigger).Damage.Int())
            .OrderBy(x => x)
            .ToArray();
    }

    [Test]
    public async Task ThresholdsOverrideReplacesInherited()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var factory = SEntMan.ComponentFactory;

            int[] Proto(string id)
            {
                Assert.That(SProtoMan.Index<EntityPrototype>(id).TryComp<DestructibleComponent>(out var comp, factory));
                return Damages(comp!);
            }

            Assert.Multiple(() =>
            {
                Assert.That(Proto("ADTDestructibleOverrideTestAdditive"), Is.EqualTo(new[] { 50, 100 }));
                Assert.That(Proto("ADTDestructibleOverrideTestOverride"), Is.EqualTo(new[] { 200 }));
                Assert.That(Proto("ADTDestructibleOverrideTestOverrideChild"), Is.EqualTo(new[] { 200 }));
            });

            var uid = SEntMan.SpawnEntity("ADTDestructibleOverrideTestOverride", map.GridCoords);
            var spawned = SEntMan.GetComponent<DestructibleComponent>(uid);
            Assert.That(spawned.Thresholds.Select(t => ((DamageTrigger) t.Trigger).Damage.Int()), Does.Contain(200));
            Assert.That(spawned.Thresholds.Select(t => ((DamageTrigger) t.Trigger).Damage.Int()), Does.Not.Contain(50));
            SEntMan.DeleteEntity(uid);
        });
    }
}
