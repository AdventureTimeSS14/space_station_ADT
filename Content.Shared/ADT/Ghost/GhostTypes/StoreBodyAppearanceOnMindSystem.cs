using Content.Shared.Body;
using Content.Shared.Destructible;
using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Ghost.GhostTypes;

public sealed class StoreBodyAppearanceOnMindSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StoreBodyAppearanceOnMindComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<StoreBodyAppearanceOnMindComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDestruction(Entity<StoreBodyAppearanceOnMindComponent> ent, ref DestructionEventArgs args)
    {
        CapAppearance(ent.Owner);
    }

    private void OnMobStateChanged(Entity<StoreBodyAppearanceOnMindComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        CapAppearance(ent.Owner);
    }

    public void CapAppearance(EntityUid body)
    {
        if (!TryComp<MindContainerComponent>(body, out var mindContainer) || mindContainer.Mind is not { } mind)
            return;

        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var organs))
            return;

        var layers = new Dictionary<HumanoidVisualLayers, PrototypeLayerData>();
        var markings = new Dictionary<HumanoidVisualLayers, List<Marking>>();

        foreach (var organ in organs.ContainedEntities)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visual) && visual.Layer is HumanoidVisualLayers layer)
                layers[layer] = visual.Data;

            if (!TryComp<VisualOrganMarkingsComponent>(organ, out var organMarkings))
                continue;

            foreach (var (markingLayer, markingList) in organMarkings.Markings)
                markings[markingLayer] = new List<Marking>(markingList);
        }

        if (layers.Count == 0)
            return;

        var sex = Sex.Unsexed;

        if (TryComp<HumanoidProfileComponent>(body, out var profile))
            sex = profile.Sex;

        var displacements = new Dictionary<string, DisplacementData>();
        var femaleDisplacements = new Dictionary<string, DisplacementData>();
        var maleDisplacements = new Dictionary<string, DisplacementData>();

        if (TryComp<InventoryComponent>(body, out var inventory))
        {
            displacements = new Dictionary<string, DisplacementData>(inventory.Displacements);
            femaleDisplacements = new Dictionary<string, DisplacementData>(inventory.FemaleDisplacements);
            maleDisplacements = new Dictionary<string, DisplacementData>(inventory.MaleDisplacements);
        }

        var appearance = EnsureComp<GhostBodyAppearanceComponent>(mind);
        appearance.Layers = layers;
        appearance.Markings = markings;
        appearance.Sex = sex;
        appearance.Displacements = displacements;
        appearance.FemaleDisplacements = femaleDisplacements;
        appearance.MaleDisplacements = maleDisplacements;
        Dirty(mind, appearance);
    }
}
