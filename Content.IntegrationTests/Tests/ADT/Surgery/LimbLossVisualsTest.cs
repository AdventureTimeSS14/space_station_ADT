using System.Linq;
using Content.Client.Inventory;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.ADT.Surgery;

public sealed class LimbLossVisualsTest : InteractionTest
{
    private const string ClipSuffix = "-adt-limb-clip";

    [SidedDependency(Side.Client)]
    private readonly IResourceCache _cache = null!;

    protected override string PlayerPrototype => "MobVox";

    [Test]
    public async Task ClothingClipFollowsLimb()
    {
        EntityUid foot = default;
        EntityUid leg = default;
        EntityUid shoes = default;

        await Server.WaitAssertion(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            var coordinates = SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates;
            var jumpsuit = SEntMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", coordinates);
            shoes = SEntMan.SpawnEntity("ClothingShoesColorBlack", coordinates);
            Assert.That(inventory.TryEquip(SPlayer, jumpsuit, "jumpsuit", silent: true, force: true), Is.True);
            Assert.That(inventory.TryEquip(SPlayer, shoes, "shoes", silent: true, force: true), Is.True);

            foot = FindOrgan("FootLeft");
            leg = FindOrgan("LegLeft");
        });

        await RunTicks(20);

        await Client.WaitAssertion(() =>
        {
            Assert.That(ClipLayers("jumpsuit"), Is.Empty);
            Assert.That(ClipLayers("shoes"), Is.Empty);
        });

        await Server.WaitAssertion(() =>
        {
            SEntMan.System<DetachableOrganSystem>().Detach(foot);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(SPlayer, "shoes", out _), Is.True);
        });

        await RunTicks(20);

        await Client.WaitAssertion(() =>
        {
            var sprite = CComp<SpriteComponent>(CPlayer);
            foreach (var slot in new[] { "jumpsuit", "shoes" })
            {
                var clips = ClipLayers(slot);
                Assert.That(clips, Is.Not.Empty, slot);

                foreach (var clip in clips)
                {
                    var layer = (SpriteComponent.Layer) sprite[clip[..^ClipSuffix.Length]];
                    Assert.That(layer.ShaderPrototype?.Id, Does.StartWith("ADTLimbClip"));
                    Assert.That(layer.Shader, Is.Not.Null);
                    Assert.That(((SpriteComponent.Layer) sprite[clip]).State.Name, Is.EqualTo("regions-Vox"));
                }
            }
        });

        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<BodySystem>().TryInsertOrgan(SPlayer, foot, leg), Is.True);
        });

        await RunTicks(20);

        await Client.WaitAssertion(() =>
        {
            Assert.That(ClipLayers("jumpsuit"), Is.Empty);
            Assert.That(ClipLayers("shoes"), Is.Empty);
        });
    }

    [Test]
    public async Task EveryDisplacementSpeciesHasClipMap()
    {
        await Client.WaitAssertion(() =>
        {
            var rsi = _cache.GetResource<RSIResource>(new ResPath("/Textures/ADT/Mobs/Effects/limb_clip.rsi")).RSI;
            var factory = CEntMan.ComponentFactory;

            foreach (var species in CProtoMan.EnumeratePrototypes<SpeciesPrototype>())
            {
                if (!CProtoMan.TryIndex(species.Prototype, out var mob)
                    || !mob.TryGetComponent<InventoryComponent>(out var inventory, factory))
                    continue;

                if (inventory.Displacements.Count == 0 && inventory.MaleDisplacements.Count == 0 && inventory.FemaleDisplacements.Count == 0)
                    continue;

                Assert.That(rsi.TryGetState($"regions-{species.ID}", out _), Is.True, species.ID);
            }
        });
    }

    private EntityUid FindOrgan(string category)
    {
        foreach (var organ in SEntMan.System<BodySystem>().EnumerateOrgans<OrganComponent>(SPlayer))
        {
            if (organ.Comp1.Category == category)
                return organ.Owner;
        }

        Assert.Fail($"{category} not found");
        return default;
    }

    private string[] ClipLayers(string slot)
    {
        var slots = CComp<InventorySlotsComponent>(CPlayer);

        return slots.VisualLayerKeys.TryGetValue(slot, out var keys)
            ? keys.Where(key => key.EndsWith(ClipSuffix)).ToArray()
            : [];
    }
}
