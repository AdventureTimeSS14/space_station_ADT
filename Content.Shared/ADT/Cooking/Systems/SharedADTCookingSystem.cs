using Content.Shared.ADT.Cooking.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Cooking.Systems;

public abstract partial class SharedADTCookingSystem : EntitySystem
{
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] private IngestionSystem _ingestion = default!;
    [Dependency] private IGameTiming _cookingTiming = default!;

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
    }

    private void OnPlateUseInHand(Entity<ADTPlateComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || GetTopItem(ent) is not { } item)
            return;

        args.Handled = true;
        _ingestion.TryIngest(args.User, args.User, item);
    }

    private void OnPlateAfterInteract(Entity<ADTPlateComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!HasComp<MobStateComponent>(target) || GetTopItem(ent) is not { } item)
            return;

        args.Handled = true;
        _ingestion.TryIngest(args.User, target, item);
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

        if (ent.Comp.Stage != ADTCookingStage.Idle)
            args.PushMarkup(Loc.GetString($"adt-cookware-stage-{ent.Comp.Stage.ToString().ToLowerInvariant()}"));
    }

    private void OnStoveExamined(Entity<ADTStoveComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

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
