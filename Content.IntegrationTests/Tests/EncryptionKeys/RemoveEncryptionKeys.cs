using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio.Components;
using Content.Shared.Wires;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Examine;
using Content.Shared.Radio;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.EncryptionKeys;

public sealed class RemoveEncryptionKeys : InteractionTest
{
    [Test]
    public async Task HeadsetKeys()
    {
        await SpawnTarget("ClothingHeadsetGrey");
        var comp = Comp<EncryptionKeyHolderComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("Common"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("Common"));
        });

        // A closed panel blocks insertion and hides key-removal verbs.
        await InteractUsing("EncryptionKeyCentCom");
        Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
        await Server.WaitAssertion(() =>
        {
            var verbs = SEntMan.System<SharedVerbSystem>().GetLocalVerbs(STarget.Value, SPlayer, typeof(AlternativeVerb));
            Assert.That(verbs.Any(v => v.Category == VerbCategory.Eject), Is.False);
        });

        // The screwdriver opens the panel without removing any keys.
        await InteractUsing(Screw);
        Assert.That(Comp<WiresPanelComponent>().Open, Is.True);
        Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));

        // Remove just the selected key through the standard context menu.
        await Server.WaitAssertion(() =>
        {
            var verbs = SEntMan.System<SharedVerbSystem>().GetLocalVerbs(STarget.Value, SPlayer, typeof(AlternativeVerb));
            var verb = verbs.Single(v => v.Category == VerbCategory.Eject);
            verb.Act!();
        });
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.DefaultChannel, Is.Null);
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Check that the key was ejected and not just deleted or something.
        await AssertEntityLookup(("EncryptionKeyCommon", 1));

        // Re-insert a key.
        await InteractUsing("EncryptionKeyCentCom");
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));
        });

        // Closing the panel preserves installed keys and blocks further insertion.
        await InteractUsing(Screw);
        Assert.That(Comp<WiresPanelComponent>().Open, Is.False);
        await InteractUsing("EncryptionKeyCommon");
        Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HeadsetIndividualKeys()
    {
        await SpawnTarget("ClothingHeadsetCargo");
        var holder = Comp<EncryptionKeyHolderComponent>();
        Assert.That(holder.KeyContainer.ContainedEntities, Has.Count.EqualTo(2));
        await InteractUsing(Screw);

        await Server.WaitAssertion(() =>
        {
            var wires = SEntMan.System<SharedWiresSystem>();
            var verbs = SEntMan.System<SharedVerbSystem>();
            var uid = STarget.Value;
            var panel = Comp<WiresPanelComponent>();
            var key = holder.KeyContainer.ContainedEntities[0];
            var selected = verbs.GetLocalVerbs(uid, SPlayer, typeof(AlternativeVerb))
                .Single(v => v.IconEntity == SEntMan.GetNetEntity(key));

            // An old menu must not extract keys after the panel closes.
            wires.TogglePanel(uid, panel, false);
            selected.Act!();
            Assert.That(holder.KeyContainer.ContainedEntities, Has.Count.EqualTo(2));
            wires.TogglePanel(uid, panel, true);

            // Non-removable keys stay protected.
            SEntMan.EnsureComponent<UnremoveableComponent>(key);
            Assert.That(verbs.GetLocalVerbs(uid, SPlayer, typeof(AlternativeVerb))
                .Any(v => v.IconEntity == SEntMan.GetNetEntity(key)), Is.False);
            selected.Act!();
            Assert.That(holder.KeyContainer.ContainedEntities, Has.Count.EqualTo(2));

            var removable = holder.KeyContainer.ContainedEntities[1];
            verbs.GetLocalVerbs(uid, SPlayer, typeof(AlternativeVerb))
                .Single(v => v.IconEntity == SEntMan.GetNetEntity(removable)).Act!();
            Assert.That(holder.KeyContainer.ContainedEntities, Is.EquivalentTo(new[] { key }));
            Assert.That(holder.Channels, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task HeadsetPanelDisablesRadio()
    {
        await SpawnTarget("ClothingHeadsetGrey");
        await Server.WaitAssertion(() =>
        {
            var uid = STarget.Value;
            var wearer = SEntMan.SpawnEntity("MobHuman", SEntMan.GetComponent<TransformComponent>(uid).Coordinates);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, uid, "ears", force: true), Is.True);
            Assert.That(SEntMan.HasComponent<ActiveRadioComponent>(uid), Is.True);

            var panel = Comp<WiresPanelComponent>();
            var wires = SEntMan.System<SharedWiresSystem>();
            wires.TogglePanel(uid, panel, true);
            Assert.That(SEntMan.HasComponent<ActiveRadioComponent>(uid), Is.False);
            var openDefault = new GetDefaultRadioChannelEvent();
            SEntMan.EventBus.RaiseLocalEvent(uid, new InventoryRelayedEvent<GetDefaultRadioChannelEvent>(openDefault, wearer));
            Assert.That(openDefault.Channel, Is.Null);
            wires.TogglePanel(uid, panel, false);
            Assert.That(SEntMan.HasComponent<ActiveRadioComponent>(uid), Is.True);
            var closedDefault = new GetDefaultRadioChannelEvent();
            SEntMan.EventBus.RaiseLocalEvent(uid, new InventoryRelayedEvent<GetDefaultRadioChannelEvent>(closedDefault, wearer));
            Assert.That(closedDefault.Channel, Is.EqualTo("Common"));

            // Closing a panel must not override another reason for being disabled.
            wires.TogglePanel(uid, panel, true);
            SEntMan.System<Content.Server.Radio.EntitySystems.HeadsetSystem>().SetEnabled(uid, false);
            wires.TogglePanel(uid, panel, false);
            Assert.That(SEntMan.HasComponent<ActiveRadioComponent>(uid), Is.False);
            Assert.That(Comp<HeadsetComponent>().Enabled, Is.False);
        });
    }

    [Test]
    public async Task HeadsetChannelsRequireOpenPanel()
    {
        await SpawnTarget("ClothingHeadsetGrey");
        await Server.WaitAssertion(() =>
        {
            var examine = SEntMan.System<ExamineSystemShared>();
            var prefix = Loc.GetString("examine-encryption-channels-prefix");
            Assert.That(examine.GetExamineText(STarget.Value, SPlayer).ToMarkup(), Does.Not.Contain(prefix));
            var wires = SEntMan.System<SharedWiresSystem>();
            wires.TogglePanel(STarget.Value, Comp<WiresPanelComponent>(), true);
            Assert.That(examine.GetExamineText(STarget.Value, SPlayer).ToMarkup(), Does.Contain(prefix));
            wires.TogglePanel(STarget.Value, Comp<WiresPanelComponent>(), false);
            Assert.That(examine.GetExamineText(STarget.Value, SPlayer).ToMarkup(), Does.Not.Contain(prefix));
        });
    }

    [Test]
    public async Task CommsServerKeys()
    {
        await SpawnTarget("TelecomServerFilled");
        var comp = Comp<EncryptionKeyHolderComponent>();
        var panel = Comp<WiresPanelComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
            Assert.That(panel.Open, Is.False);
        });

        // cannot remove keys without opening panel
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
            Assert.That(panel.Open, Is.False);
        });

        // Open panel
        await InteractUsing(Screw);
        Assert.Multiple(() =>
        {
            Assert.That(panel.Open, Is.True);

            // Keys are still here
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.GreaterThan(0));
            Assert.That(comp.Channels, Has.Count.GreaterThan(0));
        });

        // Now remove the keys
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Reinsert a key
        await InteractUsing("EncryptionKeyCentCom");
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("CentCom"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("CentCom"));
        });

        // Remove it again
        await InteractUsing(Pry);
        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(0));
            Assert.That(comp.Channels, Has.Count.EqualTo(0));
        });

        // Prying again will start deconstructing the machine.
        AssertPrototype("TelecomServerFilled");
        await InteractUsing(Pry);
        AssertPrototype("MachineFrame");
    }
}
