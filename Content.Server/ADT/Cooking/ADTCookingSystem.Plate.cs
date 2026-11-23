using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{

    private readonly HashSet<EntityUid> _pendingServing = new();

    private void InitializePlate()
    {
        SubscribeLocalEvent<ADTPlateComponent, InteractUsingEvent>(OnPlateInteractUsing, before: new[] { typeof(SolutionSpikerSystem) });
        SubscribeLocalEvent<ADTPlateComponent, GetVerbsEvent<AlternativeVerb>>(OnPlateGetVerbs);
        SubscribeLocalEvent<ADTPlateComponent, EntInsertedIntoContainerMessage>(OnPlateInserted);
        SubscribeLocalEvent<ADTPlateComponent, EntRemovedFromContainerMessage>(OnPlateRemoved);
        SubscribeLocalEvent<ADTPlateComponent, DestructionEventArgs>(OnPlateDestroyed);
    }

    private void OnPlateDestroyed(Entity<ADTPlateComponent> ent, ref DestructionEventArgs args)
    {
        Container.EmptyContainer(ent.Comp.Contents, true, Transform(ent).Coordinates);
    }

    private void OnPlateInteractUsing(Entity<ADTPlateComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ADTCookwareComponent>(args.Used, out var cookware))
        {
            args.Handled = true;
            MoveToPlate(ent, (args.Used, cookware), args.User);
            return;
        }

        if (HasComp<SolutionTransferComponent>(args.Used) && ent.Comp.Contents.ContainedEntities.Count > 0)
        {
            var top = ent.Comp.Contents.ContainedEntities[^1];
            if (TryPourRecipeReagents(ent, args.Used, args.User) || TryPourSauce(top, args.Used, args.User))
            {
                args.Handled = true;
                return;
            }
        }

        if (HasComp<ADTPlateComponent>(args.Used) || HasComp<SolutionTransferComponent>(args.Used))
            return;

        if (!HasComp<EdibleComponent>(args.Used) && !IsPlateRecipeIngredient(args.Used))
            return;

        args.Handled = true;

        if (!CanPlaceOnPlate(ent, args.Used))
        {
            _popup.PopupEntity(Loc.GetString("adt-plate-full"), ent, args.User);
            return;
        }

        Container.Insert(args.Used, ent.Comp.Contents);
    }

    private bool CanPlaceOnPlate(Entity<ADTPlateComponent> plate, EntityUid item)
    {
        if (CanFitOnPlate(plate, item))
            return true;

        if (plate.Comp.Contents.ContainedEntities.Count >= plate.Comp.MaxItems)
            return false;

        var items = new List<EntityUid>(plate.Comp.Contents.ContainedEntities) { item };
        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Cookware == ADTCookwareType.Plate && FitsIngredients(items, recipe.Ingredients))
                return true;
        }

        return false;
    }

    private bool FitsIngredients(List<EntityUid> items, List<ADTCookingIngredient> ingredients)
    {
        var left = new int[ingredients.Count];
        for (var i = 0; i < ingredients.Count; i++)
        {
            left[i] = ingredients[i].Count;
        }

        foreach (var item in items)
        {
            var proto = MetaData(item).EntityPrototype?.ID;
            var slot = -1;

            for (var i = 0; i < ingredients.Count && slot < 0; i++)
            {
                if (left[i] > 0 && ingredients[i].Id is { } id && id.Id == proto)
                    slot = i;
            }

            for (var i = 0; i < ingredients.Count && slot < 0; i++)
            {
                if (left[i] > 0 && ingredients[i].Id == null && ingredients[i].Tag is { } tag && _tag.HasTag(item, tag))
                    slot = i;
            }

            if (slot < 0)
                return false;

            left[slot]--;
        }

        return true;
    }

    private bool IsPlateRecipeIngredient(EntityUid item)
    {
        var proto = MetaData(item).EntityPrototype?.ID;
        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Cookware != ADTCookwareType.Plate || recipe.Secret)
                continue;

            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Id is { } id)
                {
                    if (proto == id.Id)
                        return true;
                }
                else if (ingredient.Tag is { } tag && _tag.HasTag(item, tag))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void MoveToPlate(Entity<ADTPlateComponent> plate, Entity<ADTCookwareComponent> cookware, EntityUid user)
    {
        if (cookware.Comp.Stage == ADTCookingStage.OnFire || cookware.Comp.Stage == ADTCookingStage.Cooking)
        {
            _popup.PopupEntity(Loc.GetString("adt-plate-cookware-busy"), plate, user);
            return;
        }

        if (cookware.Comp.Type == ADTCookwareType.Pot)
        {
            if (IsBowl(plate))
                TryLadle(cookware, plate, user);
            else
                _popup.PopupEntity(Loc.GetString("adt-plate-use-bowl"), plate, user);

            return;
        }

        var moved = 0;
        foreach (var item in new List<EntityUid>(cookware.Comp.Contents.ContainedEntities))
        {
            if (!CanPlaceOnPlate(plate, item))
                continue;

            if (Container.Insert(item, plate.Comp.Contents))
                moved++;
        }

        if (moved == 0)
            _popup.PopupEntity(Loc.GetString("adt-plate-full"), plate, user);
    }

    private void OnPlateGetVerbs(Entity<ADTPlateComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var priority = 0;
        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            var target = item;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-plate-verb-take", ("item", Name(target))),
                Category = ADTCookingVerbCategories.PlateTake,
                Act = () => TakeFromPlate(ent, target, user),
                Priority = priority,
            });

            priority--;
        }
    }

    private void TakeFromPlate(Entity<ADTPlateComponent> ent, EntityUid item, EntityUid user)
    {
        if (!ent.Comp.Contents.Contains(item))
            return;

        Container.Remove(item, ent.Comp.Contents);
        _hands.PickupOrDrop(user, item);
    }

    private void OnPlateInserted(Entity<ADTPlateComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ADTPlateComponent.ContainerId)
            return;

        _pendingServing.Add(ent);
    }

    private void OnPlateRemoved(Entity<ADTPlateComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ADTPlateComponent.ContainerId)
            return;

        _pendingServing.Add(ent);
    }

    private void ProcessPendingServing()
    {
        if (_pendingServing.Count == 0)
            return;

        var pending = new List<EntityUid>(_pendingServing);
        _pendingServing.Clear();

        foreach (var uid in pending)
        {
            if (TerminatingOrDeleted(uid) || !TryComp<ADTPlateComponent>(uid, out var plate))
                continue;

            UpdateServing((uid, plate));
        }
    }

    private void UpdateServing(Entity<ADTPlateComponent> ent)
    {
        var settings = Settings;
        var items = ent.Comp.Contents.ContainedEntities;

        ADTCookingRecipePrototype? best = null;
        if (items.Count > 0)
        {
            foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
            {
                if (recipe.Cookware != ADTCookwareType.Plate || recipe.Ingredients.Count == 0 || recipe.Secret)
                    continue;

                if (!MatchIngredients(items, recipe.Ingredients) || !PlateReagentsPresent(items, recipe, settings))
                    continue;

                if (best == null || Complexity(recipe) > Complexity(best))
                    best = recipe;
            }
        }

        if (best?.Result != null)
        {
            AssembleOnPlate(ent, best, settings);
            return;
        }

        if (best?.ServingName != null)
        {
            ent.Comp.OriginalName ??= Name(ent);
            ent.Comp.Serving = best.ID;
            _metaData.SetEntityName(ent, Loc.GetString(best.ServingName.Value));
            ApplyServingBonus(ent, settings);
            return;
        }

        ent.Comp.Serving = null;
        if (ent.Comp.OriginalName != null)
        {
            _metaData.SetEntityName(ent, ent.Comp.OriginalName);
            ent.Comp.OriginalName = null;
        }
    }

    private bool PlateReagentsPresent(IReadOnlyList<EntityUid> items, ADTCookingRecipePrototype recipe, ADTCookingSettingsPrototype settings)
    {
        if (recipe.Reagents.Count == 0)
            return true;

        var present = GetPlateAddedReagents(items, settings);
        foreach (var (reagent, amount) in recipe.Reagents)
        {
            if (present.GetTotalPrototypeQuantity(reagent) < amount)
                return false;
        }

        return true;
    }

    private Solution GetPlateAddedReagents(IReadOnlyList<EntityUid> items, ADTCookingSettingsPrototype settings)
    {
        var total = new Solution();
        foreach (var item in items)
        {
            total.AddSolution(GetAddedReagents(item, settings), _proto);
        }

        return total;
    }

    private bool TryPourRecipeReagents(Entity<ADTPlateComponent> ent, EntityUid source, EntityUid user)
    {
        var items = ent.Comp.Contents.ContainedEntities;
        if (items.Count == 0 || !CanAddToFood(items[^1]))
            return false;

        if (!CanSendSolution(source) || !_solution.TryGetDrainableSolution(source, out var sourceSoln, out var sourceSolution))
            return false;

        var settings = Settings;
        var present = GetPlateAddedReagents(items, settings);

        ADTCookingRecipePrototype? best = null;
        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Cookware != ADTCookwareType.Plate || recipe.Reagents.Count == 0 || recipe.Secret)
                continue;

            if (!MatchIngredients(items, recipe.Ingredients))
                continue;

            var fits = true;
            var missingAny = false;
            foreach (var (reagent, amount) in recipe.Reagents)
            {
                var missing = amount - present.GetTotalPrototypeQuantity(reagent);
                if (missing <= FixedPoint2.Zero)
                    continue;

                missingAny = true;
                if (sourceSolution.GetTotalPrototypeQuantity(reagent) < missing)
                {
                    fits = false;
                    break;
                }
            }

            if (!fits || !missingAny)
                continue;

            if (best == null || Complexity(recipe) > Complexity(best))
                best = recipe;
        }

        if (best == null)
            return false;

        var poured = new Solution();
        foreach (var (reagent, amount) in best.Reagents)
        {
            var missing = amount - present.GetTotalPrototypeQuantity(reagent);
            if (missing <= FixedPoint2.Zero)
                continue;

            foreach (var (id, quantity) in sourceSolution.Contents.ToArray())
            {
                if (missing <= FixedPoint2.Zero)
                    break;

                if (id.Prototype != reagent)
                    continue;

                var removed = _solution.RemoveReagent(sourceSoln.Value, id, FixedPoint2.Min(quantity, missing));
                if (removed <= FixedPoint2.Zero)
                    continue;

                poured.AddReagent(id, removed);
                missing -= removed;
            }
        }

        var top = items[^1];
        AddToFood(top, poured);
        _popup.PopupEntity(Loc.GetString("adt-dish-sauce-poured", ("dish", top), ("source", source)), ent, user);
        _pendingServing.Add(ent);
        return true;
    }

    private void AssembleOnPlate(Entity<ADTPlateComponent> ent, ADTCookingRecipePrototype recipe, ADTCookingSettingsPrototype settings)
    {
        if (recipe.Result == null)
            return;

        var level = 1;
        var levels = 0;
        var sum = 0;
        foreach (var item in new List<EntityUid>(ent.Comp.Contents.ContainedEntities))
        {
            if (TryComp<ADTDishComponent>(item, out var dish) && _proto.TryIndex(dish.Quality, out var quality))
            {
                sum += quality.Level;
                levels++;
            }

            Del(item);
        }

        if (levels > 0)
            level = (int) MathF.Round(sum / (float) levels);

        level = Math.Max(level, 1);

        for (var i = 0; i < recipe.ResultCount; i++)
        {
            var result = recipe.ConsumePlate
                ? EntityManager.SpawnNextToOrDrop(recipe.Result.Value, ent)
                : EntityManager.SpawnInContainerOrDrop(recipe.Result.Value, ent, ADTPlateComponent.ContainerId);

            ApplyDish(result, recipe.Method, level, settings);
            EnsureComp<ADTServedComponent>(result);
        }

        if (recipe.ConsumePlate)
            QueueDel(ent);
    }

    private void ApplyServingBonus(Entity<ADTPlateComponent> ent, ADTCookingSettingsPrototype settings)
    {
        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            if (HasComp<ADTServedComponent>(item))
                continue;

            EnsureComp<ADTServedComponent>(item);

            if (!TryComp<ADTDishComponent>(item, out var dish) || !_proto.TryIndex(dish.Quality, out var quality))
                continue;

            var level = Math.Min(quality.Level + 1, settings.Qualities.Count - 1);
            if (level == quality.Level)
                continue;

            var next = settings.Qualities[level];
            _dish.SetDish(item, dish.Method, next);

            if (_proto.TryIndex(next, out var nextQuality) && quality.NutritionMultiplier > 0)
                ScaleNutrients(item, nextQuality.NutritionMultiplier / quality.NutritionMultiplier, settings);
        }
    }
}
