using Content.Shared.ADT.Cooking.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Cooking.Systems;

public abstract partial class SharedADTCookingSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private IGameTiming _cookingTiming = default!;
    [Dependency] private SharedItemSystem _cookingItem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCookwareComponent, ComponentInit>(OnCookwareInit);
        SubscribeLocalEvent<ADTCookwareComponent, ExaminedEvent>(OnCookwareExamined);
        SubscribeLocalEvent<ADTStoveComponent, ComponentInit>(OnStoveInit);
        SubscribeLocalEvent<ADTStoveComponent, ExaminedEvent>(OnStoveExamined);
        SubscribeLocalEvent<ADTPlateComponent, ComponentInit>(OnPlateInit);
        SubscribeLocalEvent<ADTPlateComponent, UseInHandEvent>(OnPlateUseInHand, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, AfterInteractEvent>(OnPlateAfterInteract, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, IngestibleEvent>(OnPlateIngestible, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, IsDigestibleEvent>(OnPlateIsDigestible, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, EdibleEvent>(OnPlateEdible, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, GetUtensilsEvent>(OnPlateGetUtensils, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, BeforeIngestedEvent>(OnPlateBeforeIngested, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<ADTPlateComponent, IngestedEvent>(OnPlateIngested, before: new[] { typeof(IngestionSystem) });
    }

    public int GetPlateWeight(Entity<ADTPlateComponent> plate, EntityUid item)
    {
        if (!TryComp<ItemComponent>(item, out var itemComp))
            return 1;

        var weight = _cookingItem.GetItemSizeWeight(itemComp.Size);
        if (weight >= _cookingItem.GetItemSizeWeight(plate.Comp.FullSize))
            return plate.Comp.MaxWeight;

        return weight;
    }

    public bool CanFitOnPlate(Entity<ADTPlateComponent> plate, EntityUid item)
    {
        var contents = plate.Comp.Contents.ContainedEntities;
        if (contents.Count >= plate.Comp.MaxItems)
            return false;

        var weight = GetPlateWeight(plate, item);
        foreach (var contained in contents)
        {
            weight += GetPlateWeight(plate, contained);
        }

        return weight <= plate.Comp.MaxWeight;
    }

    private void OnPlateUseInHand(Entity<ADTPlateComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || GetTopItem(ent) is not { } item)
            return;

        args.Handled = true;
        _ingestion.TryIngest(args.User, args.User, ent);
    }

    private void OnPlateAfterInteract(Entity<ADTPlateComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<MobStateComponent>(target) || GetTopItem(ent) == null)
            return;

        args.Handled = true;
        _ingestion.TryIngest(args.User, target, ent);
    }

    private void OnPlateIngestible(Entity<ADTPlateComponent> ent, ref IngestibleEvent args)
    {
        if (GetTopItem(ent) is { } item)
            RaiseLocalEvent(item, ref args);
    }

    private void OnPlateIsDigestible(Entity<ADTPlateComponent> ent, ref IsDigestibleEvent args)
    {
        if (GetTopItem(ent) is { } item)
            RaiseLocalEvent(item, ref args);
    }

    private void OnPlateEdible(Entity<ADTPlateComponent> ent, ref EdibleEvent args)
    {
        if (GetTopItem(ent) is { } item)
            RaiseLocalEvent(item, ref args);
    }

    private void OnPlateGetUtensils(Entity<ADTPlateComponent> ent, ref GetUtensilsEvent args)
    {
        if (GetTopItem(ent) is { } item)
            RaiseLocalEvent(item, ref args);
    }

    private void OnPlateBeforeIngested(Entity<ADTPlateComponent> ent, ref BeforeIngestedEvent args)
    {
        if (GetTopItem(ent) is { } item)
            RaiseLocalEvent(item, ref args);
    }

    private void OnPlateIngested(Entity<ADTPlateComponent> ent, ref IngestedEvent args)
    {
        if (GetTopItem(ent) is not { } item)
            return;

        RaiseLocalEvent(item, ref args);
        args.Handled = true;

        if (!args.Destroy)
            return;

        args.Destroy = false;
        args.Repeat = false;

        var eaten = new FullyEatenEvent(args.User);
        RaiseLocalEvent(item, ref eaten);
        PredictedDel(item);
    }

    private static EntityUid? GetTopItem(Entity<ADTPlateComponent> ent)
    {
        var items = ent.Comp.Contents.ContainedEntities;
        if (items.Count == 0)
            return null;

        return items[items.Count - 1];
    }

    private void OnPlateInit(Entity<ADTPlateComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Contents = Container.EnsureContainer<Container>(ent, ADTPlateComponent.ContainerId);
    }

    private void OnCookwareInit(Entity<ADTCookwareComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Contents = Container.EnsureContainer<Container>(ent, ADTCookwareComponent.ContainerId);
    }

    private void OnStoveInit(Entity<ADTStoveComponent> ent, ref ComponentInit args)
    {
        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            Container.EnsureContainer<ContainerSlot>(ent, ADTStoveComponent.SlotContainerId(i));
        }
    }

    private void OnCookwareExamined(Entity<ADTCookwareComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("adt-cookware-examine-contents",
            ("count", ent.Comp.Contents.ContainedEntities.Count),
            ("max", ent.Comp.MaxItems)));

        if (ent.Comp.Stage is ADTCookingStage.Burnt or ADTCookingStage.OnFire)
            args.PushMarkup(Loc.GetString($"adt-cookware-stage-{ent.Comp.Stage.ToString().ToLowerInvariant()}"));
    }

    private void OnStoveExamined(Entity<ADTStoveComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.Broken)
            args.PushMarkup(Loc.GetString("adt-stove-examine-broken"));

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            var slot = ent.Comp.Slots[i];
            var cookware = GetSlotCookware(ent, i);
            var content = cookware == null
                ? Loc.GetString("adt-stove-slot-empty")
                : Name(cookware.Value);

            args.PushMarkup(Loc.GetString("adt-stove-examine-slot",
                ("slot", Loc.GetString(slot.Name, ("number", i + 1))),
                ("heat", Loc.GetString($"adt-stove-heat-{slot.Heat.ToString().ToLowerInvariant()}")),
                ("content", content)));
        }

        if (ent.Comp.OvenTimerEnd is { } end && end > _cookingTiming.CurTime)
        {
            args.PushMarkup(Loc.GetString("adt-stove-examine-timer",
                ("seconds", (int) Math.Ceiling((end - _cookingTiming.CurTime).TotalSeconds))));
        }
    }

    public EntityUid? GetSlotCookware(Entity<ADTStoveComponent> stove, int index)
    {
        if (!Container.TryGetContainer(stove, ADTStoveComponent.SlotContainerId(index), out var container))
            return null;

        if (container.ContainedEntities.Count == 0)
            return null;

        return container.ContainedEntities[0];
    }

    public bool TryGetStoveSlot(EntityUid cookware, out Entity<ADTStoveComponent> stove, out int index)
    {
        stove = default;
        index = -1;

        if (!Container.TryGetContainingContainer(cookware, out var container))
            return false;

        if (!TryComp<ADTStoveComponent>(container.Owner, out var stoveComp))
            return false;

        string containerId = container.ID;
        if (!containerId.StartsWith(ADTStoveComponent.SlotContainerPrefix))
            return false;

        if (!int.TryParse(containerId.Substring(ADTStoveComponent.SlotContainerPrefix.Length), out index))
            return false;

        stove = (container.Owner, stoveComp);
        return index >= 0 && index < stoveComp.Slots.Count;
    }
}
