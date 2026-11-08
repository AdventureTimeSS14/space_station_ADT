using System.Linq;
using System.Text;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private readonly record struct CookingTarget(
        ADTCookingTargetKind Kind,
        ADTCookingRecipePrototype? Recipe,
        float Time,
        float Overcook,
        float Burn);

    private void UpdateTarget(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        if (comp.Stage != ADTCookingStage.Idle && comp.Stage != ADTCookingStage.Cooking)
            return;

        var key = BuildKey(ent, medium);
        if (key == comp.TargetKey)
            return;

        comp.TargetKey = key;
        var target = SelectTarget(ent, medium, settings);

        if (target.Kind == ADTCookingTargetKind.None)
        {
            comp.TargetKind = ADTCookingTargetKind.None;
            comp.Recipe = null;
            comp.CookTime = 0;
            ResetMinigames(comp);
            SetStage(ent, ADTCookingStage.Idle);
            comp.CookResults.Clear();
            comp.CookCenterResult = null;
            comp.CookToppings.Clear();
            comp.CookToppingsStay = false;
            RefreshVisualTiming(ent);
            return;
        }

        var sameTarget = comp.TargetKind == target.Kind && comp.Recipe?.Id == target.Recipe?.ID;
        if (!sameTarget)
        {
            comp.CookTime = 0;
            ResetMinigames(comp);
        }

        comp.TargetKind = target.Kind;
        if (target.Recipe == null)
            comp.Recipe = null;
        else
            comp.Recipe = new ProtoId<ADTCookingRecipePrototype>(target.Recipe.ID);
        comp.TargetTime = target.Time;
        comp.OvercookTime = target.Overcook;
        comp.BurnTime = target.Burn;
        SetStage(ent, ADTCookingStage.Cooking);
        FillCookResults(ent, target, medium);
        RefreshVisualTiming(ent);
    }

    private void FillCookResults(Entity<ADTCookwareComponent> ent, CookingTarget target, ADTCookingMediumPrototype? medium)
    {
        var comp = ent.Comp;
        comp.CookResults.Clear();
        comp.CookCenterResult = null;
        comp.CookCenterLarge = false;
        comp.CookToppings.Clear();
        comp.CookToppingsStay = false;

        if (target.Kind == ADTCookingTargetKind.Recipe && target.Recipe?.Result != null && comp.Type != ADTCookwareType.Pot)
        {
            var center = ResolveResult(ent, target.Recipe, out var toppings);
            comp.CookCenterResult = center;
            comp.CookCenterLarge = IsLargeResult(target.Recipe);
            comp.CookToppingsStay = toppings.Count > 0 && center == target.Recipe.Result.Value;
            foreach (var topping in toppings)
            {
                comp.CookToppings.Add(GetNetEntity(topping));
            }

            return;
        }

        if (target.Kind != ADTCookingTargetKind.Individual)
            return;

        _solution.TryGetSolution(ent.Owner, comp.Solution, out _, out var solution);
        var secret = GetAllowedSecret(ent);
        foreach (var item in comp.Contents.ContainedEntities)
        {
            var single = FindSingleRecipe(item, comp.Type, medium, solution, secret);
            if (single?.Result != null)
                comp.CookResults[GetNetEntity(item)] = single.Result.Value;
        }
    }

    private List<EntityUid> GetMainItems(Entity<ADTCookwareComponent> ent)
    {
        var settings = Settings;
        var items = ent.Comp.Contents.ContainedEntities;
        var main = new List<EntityUid>();
        foreach (var item in items)
        {
            if (!IsAdditive(item, settings))
                main.Add(item);
        }

        if (main.Count == 0)
            main.AddRange(items);

        return main;
    }

    private bool IsTopping(EntityUid item, ADTCookingRecipePrototype recipe)
    {
        if (MetaData(item).EntityPrototype is { } proto && recipe.ToppingIds.Contains(proto.ID))
            return true;

        foreach (var tag in recipe.ToppingTags)
        {
            if (_tag.HasTag(item, tag))
                return true;
        }

        return false;
    }

    private List<EntityUid>? GetToppings(IReadOnlyList<EntityUid> items, ADTCookingRecipePrototype recipe)
    {
        if (recipe.MaxToppings <= 0)
            return null;

        var toppings = new List<EntityUid>();
        var rest = new List<EntityUid>();
        foreach (var item in items)
        {
            if (IsTopping(item, recipe))
                toppings.Add(item);
            else
                rest.Add(item);
        }

        if (toppings.Count == 0 || toppings.Count > recipe.MaxToppings)
            return null;

        return MatchIngredients(rest, recipe.Ingredients) ? toppings : null;
    }

    private EntProtoId ResolveResult(Entity<ADTCookwareComponent> ent, ADTCookingRecipePrototype recipe, out List<EntityUid> toppings)
    {
        toppings = GetToppings(GetMainItems(ent), recipe) ?? new List<EntityUid>();
        if (toppings.Count == 0)
            return recipe.Result!.Value;

        foreach (var variant in recipe.ToppingResults)
        {
            var fits = true;
            foreach (var topping in toppings)
            {
                if (MetaData(topping).EntityPrototype is not { } proto || !variant.Toppings.Contains(proto.ID))
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
                return variant.Result;
        }

        return recipe.Result!.Value;
    }

    private void AddToppings(EntityUid result, Solution solution, HashSet<string> flavors, List<EntProtoId> protos)
    {
        if (TryComp<EdibleComponent>(result, out var edible) && _solution.TryGetSolution(result, edible.Solution, out var food, out var foodSolution))
        {
            if (foodSolution.AvailableVolume < solution.Volume)
                _solution.SetCapacity(food.Value, foodSolution.Volume + solution.Volume);

            _solution.TryAddSolution(food.Value, solution);
        }

        if (flavors.Count > 0)
        {
            var profile = EnsureComp<FlavorProfileComponent>(result);
            foreach (var flavor in flavors)
            {
                profile.Flavors.Add(flavor);
            }
        }

        if (protos.Count == 0)
            return;

        var dish = EnsureComp<ADTDishComponent>(result);
        dish.Toppings.AddRange(protos);
        Dirty(result, dish);
    }

    private bool IsLargeResult(ADTCookingRecipePrototype recipe)
    {
        if (recipe.ResultCount != 1)
            return false;

        if (recipe.Ingredients.Count == 0)
            return true;

        var bases = Settings.BaseIngredients;
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.Id is { } id && bases.Contains(id))
                return true;
        }

        return false;
    }

    private string BuildKey(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium)
    {
        var builder = new StringBuilder();
        var ids = new List<string>();
        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            ids.Add(MetaData(item).EntityPrototype?.ID ?? item.ToString());
        }

        ids.Sort(StringComparer.Ordinal);
        builder.AppendJoin(',', ids);
        builder.Append('|');
        builder.Append(medium?.ID);
        builder.Append('|');

        if (_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
        {
            foreach (var reagent in solution.Contents.OrderBy(r => r.Reagent.Prototype.Id, StringComparer.Ordinal))
            {
                builder.Append(reagent.Reagent.Prototype.Id);
                builder.Append(':');
                builder.Append(reagent.Quantity.Value);
                builder.Append(';');
            }
        }

        return builder.ToString();
    }

    private CookingTarget SelectTarget(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        var items = comp.Contents.ContainedEntities;
        if (comp.Type == ADTCookwareType.Pot && medium == null)
            return default;

        _solution.TryGetSolution(ent.Owner, comp.Solution, out _, out var solution);
        var secret = GetAllowedSecret(ent);

        var main = new List<EntityUid>();
        foreach (var item in items)
        {
            if (!IsAdditive(item, settings))
                main.Add(item);
        }

        if (main.Count == 0)
            main.AddRange(items);

        var hasAdditives = main.Count != items.Count;

        ADTCookingRecipePrototype? best = null;
        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (!RecipeFits(recipe, comp.Type, medium, solution, secret))
                continue;

            if (!MatchIngredients(items, recipe.Ingredients)
                && !(hasAdditives && MatchIngredients(main, recipe.Ingredients))
                && GetToppings(main, recipe) == null)
            {
                continue;
            }

            if (best == null || Complexity(recipe) > Complexity(best))
                best = recipe;
        }

        if (best != null)
            return new CookingTarget(ADTCookingTargetKind.Recipe, best, best.Time, best.OvercookTime ?? settings.DefaultOvercookTime, best.BurnTime ?? settings.DefaultBurnTime);

        if (items.Count == 0)
            return default;

        var individualTime = 0f;
        var individual = true;
        foreach (var item in main)
        {
            var single = FindSingleRecipe(item, comp.Type, medium, solution, secret);
            if (single == null)
            {
                if (IsAlreadyCooked(item, comp.Type))
                    continue;

                individual = false;
                break;
            }

            individualTime = Math.Max(individualTime, single.Time);
        }

        if (individual)
        {
            individualTime = Math.Max(individualTime, settings.ReheatTime);
            return new CookingTarget(ADTCookingTargetKind.Individual, null, individualTime, settings.DefaultOvercookTime, settings.DefaultBurnTime);
        }

        if (!settings.FreeformResults.ContainsKey(comp.Type))
            return default;

        foreach (var item in items)
        {
            if (!HasComp<EdibleComponent>(item))
                return default;
        }

        if (comp.Type == ADTCookwareType.Pot && (solution == null || GetWater(solution, settings) < settings.FreeformMinWater))
            return default;

        var time = settings.FreeformTimes.GetValueOrDefault(comp.Type, 30f);
        return new CookingTarget(ADTCookingTargetKind.Freeform, null, time, settings.DefaultOvercookTime, settings.DefaultBurnTime);
    }

    private HashSet<ProtoId<ADTCookingRecipePrototype>>? GetAllowedSecret(EntityUid cookware)
    {
        return TryGetStoveSlot(cookware, out var stove, out _) ? stove.Comp.SecretRecipes : null;
    }

    private bool RecipeFits(ADTCookingRecipePrototype recipe, ADTCookwareType type, ADTCookingMediumPrototype? medium, Solution? solution, HashSet<ProtoId<ADTCookingRecipePrototype>>? secret)
    {
        if (recipe.Cookware != type || (recipe.Ingredients.Count == 0 && recipe.Reagents.Count == 0))
            return false;

        if (recipe.Secret && (secret == null || !secret.Contains(recipe.ID)))
            return false;

        if (recipe.RequireMedium && medium == null)
            return false;

        foreach (var (reagent, amount) in recipe.Reagents)
        {
            if (solution == null || solution.GetTotalPrototypeQuantity(reagent) < amount)
                return false;
        }

        return true;
    }

    private ADTCookingRecipePrototype? FindSingleRecipe(EntityUid item, ADTCookwareType type, ADTCookingMediumPrototype? medium, Solution? solution, HashSet<ProtoId<ADTCookingRecipePrototype>>? secret)
    {
        var single = new List<EntityUid> { item };
        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Reagents.Count > 0 || recipe.IngredientCount() != 1)
                continue;

            if (!RecipeFits(recipe, type, medium, solution, secret))
                continue;

            if (MatchIngredients(single, recipe.Ingredients))
                return recipe;
        }

        return null;
    }

    private bool IsAlreadyCooked(EntityUid item, ADTCookwareType type)
    {
        if (HasComp<ADTDishComponent>(item))
            return true;

        var proto = MetaData(item).EntityPrototype?.ID;
        if (proto == null)
            return false;

        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Cookware == type && recipe.Result?.Id == proto)
                return true;
        }

        return false;
    }

    private static float Complexity(ADTCookingRecipePrototype recipe)
    {
        var reagents = 0f;
        foreach (var amount in recipe.Reagents.Values)
        {
            reagents += amount.Float();
        }

        return recipe.IngredientCount() * 10 + recipe.Reagents.Count + reagents / 10000f;
    }

    private bool IsRecipeIngredient(EntityUid item, ADTCookwareType type)
    {
        var proto = MetaData(item).EntityPrototype?.ID;
        if (proto == null)
            return false;

        foreach (var recipe in _proto.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Cookware != type)
                continue;

            foreach (var ingredient in recipe.Ingredients)
            {
                if (ingredient.Id is { } id && id.Id == proto)
                    return true;
            }
        }

        return false;
    }

    private bool MatchIngredients(IReadOnlyList<EntityUid> items, List<ADTCookingIngredient> ingredients)
    {
        var free = new List<EntityUid>(items);

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Id == null)
                continue;

            for (var i = 0; i < ingredient.Count; i++)
            {
                var index = free.FindIndex(e => MetaData(e).EntityPrototype?.ID == ingredient.Id.Value.Id);
                if (index < 0)
                    return false;

                free.RemoveAt(index);
            }
        }

        foreach (var ingredient in ingredients)
        {
            if (ingredient.Id != null || ingredient.Tag == null)
                continue;

            for (var i = 0; i < ingredient.Count; i++)
            {
                var tag = ingredient.Tag.Value;
                var index = free.FindIndex(e => _tag.HasTag(e, tag));
                if (index < 0)
                    return false;

                free.RemoveAt(index);
            }
        }

        return free.Count == 0;
    }

    private FixedPoint2 GetWater(Solution solution, ADTCookingSettingsPrototype settings)
    {
        var water = FixedPoint2.Zero;
        foreach (var reagent in settings.WaterReagents)
        {
            water += solution.GetTotalPrototypeQuantity(reagent);
        }

        return water;
    }

    private void Finish(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        var method = MethodFor(comp.Type);
        ADTCookingRecipePrototype? recipe = null;
        if (comp.Recipe != null)
            _proto.TryIndex(comp.Recipe.Value, out recipe);

        var level = CalculateLevel(ent, medium, recipe, comp.TargetKind, settings);

        if (comp.Type == ADTCookwareType.Pot)
        {
            FinishSoup(ent, recipe, level, settings);
            FinishCommon(ent, settings);
            return;
        }

        _solution.TryGetSolution(ent.Owner, comp.Solution, out var solutionEnt, out var solution);
        var sauce = _dish.HasSauce(solution, settings) ? solution!.Clone() : null;

        var items = new List<EntityUid>(comp.Contents.ContainedEntities);

        switch (comp.TargetKind)
        {
            case ADTCookingTargetKind.Recipe when recipe?.Result != null:
                var resultProto = ResolveResult(ent, recipe, out var toppings);
                var toppingSolution = new Solution();
                var toppingFlavors = new HashSet<string>();
                var toppingProtos = new List<EntProtoId>();
                foreach (var topping in toppings)
                {
                    if (TryComp<EdibleComponent>(topping, out var toppingEdible) && _solution.TryGetSolution(topping, toppingEdible.Solution, out _, out var toppingFood))
                        toppingSolution.AddSolution(toppingFood, _proto);

                    if (TryComp<FlavorProfileComponent>(topping, out var toppingFlavor))
                    {
                        foreach (var flavor in toppingFlavor.Flavors)
                        {
                            toppingFlavors.Add(flavor.Id);
                        }
                    }

                    if (MetaData(topping).EntityPrototype is { } toppingProto)
                        toppingProtos.Add(toppingProto.ID);
                }

                if (resultProto != recipe.Result.Value)
                    toppingProtos.Clear();

                var addedSolution = new Solution();
                foreach (var item in items)
                {
                    if (!toppings.Contains(item))
                        addedSolution.AddSolution(GetAddedReagents(item, settings), _proto);
                }

                if (recipe.ResultCount > 1)
                {
                    toppingSolution.ScaleSolution(1f / recipe.ResultCount);
                    addedSolution.ScaleSolution(1f / recipe.ResultCount);
                }

                foreach (var item in items)
                {
                    Del(item);
                }

                var large = IsLargeResult(recipe);
                for (var i = 0; i < recipe.ResultCount; i++)
                {
                    var result = EntityManager.SpawnInContainerOrDrop(resultProto, ent, ADTCookwareComponent.ContainerId);
                    ApplyDish(result, method, level, settings);
                    if (addedSolution.Volume > 0)
                        AddToFood(result, addedSolution.Clone());

                    if (toppings.Count > 0)
                        AddToppings(result, toppingSolution.Clone(), toppingFlavors, toppingProtos);

                    if (large)
                        comp.LargeItems.Add(GetNetEntity(result));
                }
                break;
            case ADTCookingTargetKind.Individual:
                foreach (var item in items)
                {
                    var single = FindSingleRecipe(item, comp.Type, medium, solution, GetAllowedSecret(ent));
                    if (single?.Result == null)
                    {
                        if (IsAdditive(item, settings))
                            Del(item);

                        continue;
                    }

                    var itemAdded = GetAddedReagents(item, settings);
                    if (single.ResultCount > 1)
                        itemAdded.ScaleSolution(1f / single.ResultCount);

                    Del(item);
                    for (var i = 0; i < single.ResultCount; i++)
                    {
                        var result = EntityManager.SpawnInContainerOrDrop(single.Result.Value, ent, ADTCookwareComponent.ContainerId);
                        ApplyDish(result, method, level, settings);
                        if (itemAdded.Volume > 0)
                            AddToFood(result, itemAdded.Clone());
                    }
                }
                break;
            case ADTCookingTargetKind.Freeform:
                CookFreeform(ent, items, solution, method, level, settings);
                break;
        }

        if (sauce != null && comp.TargetKind != ADTCookingTargetKind.Freeform)
        {
            foreach (var result in comp.Contents.ContainedEntities)
            {
                _dish.AddSauce(result, sauce, settings);
            }
        }

        if (solutionEnt != null && solution != null)
        {
            {
                if (medium != null)
                    _solution.RemoveReagent(solutionEnt.Value, medium.Reagent.Id, medium.Consumption);

                if (recipe != null)
                {
                    foreach (var (reagent, amount) in recipe.Reagents)
                    {
                        _solution.RemoveReagent(solutionEnt.Value, reagent.Id, amount);
                    }

                    foreach (var (reagent, amount) in recipe.BonusReagents)
                    {
                        _solution.RemoveReagent(solutionEnt.Value, reagent.Id, amount);
                    }
                }

                foreach (var group in settings.FlavorGroups)
                {
                    foreach (var reagent in group.Reagents)
                    {
                        _solution.RemoveReagent(solutionEnt.Value, reagent.Id, group.Amount);
                    }
                }
            }
        }

        FinishCommon(ent, settings);
    }

    private void FinishCommon(Entity<ADTCookwareComponent> ent, ADTCookingSettingsPrototype settings)
    {
        ent.Comp.CookResults.Clear();
        ent.Comp.CookCenterResult = null;
        ent.Comp.CookToppings.Clear();
        ent.Comp.CookToppingsStay = false;
        SetStage(ent, ADTCookingStage.Done);
        ent.Comp.TargetKey = null;
        RefreshVisualTiming(ent);
    }

    private void FinishSoup(Entity<ADTCookwareComponent> ent, ADTCookingRecipePrototype? recipe, int level, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        if (!_solution.TryGetSolution(ent.Owner, comp.Solution, out var potSoln, out var pot))
            return;

        var water = GetWater(pot, settings);
        var portions = Math.Clamp((int) (water / settings.FreeformWaterPerPortion).Float(), 1, settings.FreeformMaxPortions);
        var soup = new Solution();
        var items = new List<EntityUid>(comp.Contents.ContainedEntities);
        var names = new List<string>();
        comp.SoupFlavors.Clear();

        EntProtoId? result = null;
        if (comp.TargetKind == ADTCookingTargetKind.Recipe && recipe?.Result != null)
        {
            result = recipe.Result.Value;
            var solutionName = "food";
            if (_proto.TryIndex(recipe.Result.Value, out var resultProto)
                && resultProto.TryComp<EdibleComponent>(out var resultEdible, EntityManager.ComponentFactory))
            {
                solutionName = resultEdible.Solution;
            }

            if (_solution.TryGetSolution(recipe.Result.Value, solutionName, out var protoSolution))
            {
                var perPortion = protoSolution.Clone();
                perPortion.ScaleSolution(portions);
                soup.AddSolution(perPortion, _proto);
            }

            if (resultProto != null && resultProto.TryComp<FlavorProfileComponent>(out var resultFlavor, EntityManager.ComponentFactory))
            {
                foreach (var f in resultFlavor.Flavors)
                {
                    comp.SoupFlavors.Add(f);
                }
            }

            comp.SoupName = null;
        }
        else if (settings.FreeformResults.TryGetValue(ADTCookwareType.Pot, out var freeform))
        {
            result = freeform;
            foreach (var item in items)
            {
                if (TryComp<EdibleComponent>(item, out var edible) && _solution.TryGetSolution(item, edible.Solution, out _, out var itemSolution))
                    soup.AddSolution(NutrientsOnly(itemSolution, settings), _proto);

                var name = Name(item);
                if (!names.Contains(name))
                    names.Add(name);
            }

            if (!MathHelper.CloseTo(settings.FreeformNutritionMultiplier, 1f))
                soup.ScaleSolution(settings.FreeformNutritionMultiplier);

            if (water > FixedPoint2.Zero)
                soup.AddReagent("Water", water);

            comp.SoupFlavors.Add(settings.FreeformFlavor);

            comp.SoupName = Loc.GetString("adt-cooking-freeform-pot", ("ingredients", JoinNames(names)));
        }

        foreach (var item in items)
        {
            soup.AddSolution(GetAddedReagents(item, settings), _proto);
            Del(item);
        }

        soup.AddSolution(NutrientsOnly(pot, settings), _proto);

        ProtoId<ADTDishQualityPrototype>? qualityId = null;
        if (settings.Qualities.Count > 0)
        {
            qualityId = settings.Qualities[Math.Clamp(level, 0, settings.Qualities.Count - 1)];
            if (_proto.TryIndex(qualityId.Value, out var quality))
                ScaleSolutionNutrients(soup, quality.NutritionMultiplier, settings);
        }

        _solution.RemoveAllSolution(potSoln.Value);
        if (pot.MaxVolume < soup.Volume)
            _solution.SetCapacity(potSoln.Value, soup.Volume);

        _solution.TryAddSolution(potSoln.Value, soup);

        comp.SoupResult = result;
        comp.SoupQuality = qualityId;
        comp.SoupPortions = portions;
    }

    private void ScaleSolutionNutrients(Solution solution, float multiplier, ADTCookingSettingsPrototype settings)
    {
        if (MathHelper.CloseTo(multiplier, 1f))
            return;

        foreach (var reagent in settings.NutrientReagents)
        {
            var amount = solution.GetTotalPrototypeQuantity(reagent);
            if (amount <= FixedPoint2.Zero)
                continue;

            var delta = amount * (multiplier - 1f);
            if (delta > FixedPoint2.Zero)
                solution.AddReagent(reagent.Id, delta);
            else
                solution.RemoveReagent(reagent.Id, -delta);
        }
    }

    private int CalculateLevel(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium, ADTCookingRecipePrototype? recipe, ADTCookingTargetKind kind, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        var cap = settings.Qualities.Count - 1;

        if (comp.Type == ADTCookwareType.Pan && medium == null)
            cap = Math.Min(cap, settings.NoMediumMaxLevel);

        if (kind == ADTCookingTargetKind.Freeform)
            cap = Math.Min(cap, settings.FreeformMaxLevel);

        _solution.TryGetSolution(ent.Owner, comp.Solution, out _, out var solution);
        var points = 0;

        if (medium != null && MediumBonus(ent, medium, recipe))
            points++;

        foreach (var group in settings.FlavorGroups)
        {
            if (GroupPresent(group, solution, comp.Contents.ContainedEntities))
                points++;
        }

        if (recipe != null && solution != null && RecipeBonusPresent(solution, recipe))
            points++;

        if (comp.Type == ADTCookwareType.Pan && comp.FlipDone)
            points++;

        if (comp.Type == ADTCookwareType.Pot && comp.StirsNeeded > 0 && comp.StirsDone >= comp.StirsNeeded)
            points++;

        var level = 1;
        foreach (var threshold in settings.QualityThresholds)
        {
            if (points >= threshold)
                level++;
        }

        return Math.Clamp(level, 0, Math.Max(0, cap));
    }

    private bool MediumBonus(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype medium, ADTCookingRecipePrototype? recipe)
    {
        if (recipe != null && recipe.PreferredMedia.Contains(medium.ID))
            return true;

        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            var proto = MetaData(item).EntityPrototype?.ID;
            if (proto != null && medium.BonusIngredients.Contains(proto))
                return true;

            foreach (var tag in medium.BonusTags)
            {
                if (_tag.HasTag(item, tag))
                    return true;
            }
        }

        return false;
    }

    private bool GroupPresent(ADTFlavorGroup group, Solution? solution, IReadOnlyList<EntityUid> items)
    {
        if (solution != null)
        {
            foreach (var reagent in group.Reagents)
            {
                if (solution.GetTotalPrototypeQuantity(reagent) >= group.Amount)
                    return true;
            }
        }

        foreach (var item in items)
        {
            if (InGroup(group, item))
                return true;
        }

        return false;
    }

    private bool InGroup(ADTFlavorGroup group, EntityUid item)
    {
        var proto = MetaData(item).EntityPrototype?.ID;
        if (proto != null && group.Ingredients.Contains(proto))
            return true;

        foreach (var tag in group.Tags)
        {
            if (_tag.HasTag(item, tag))
                return true;
        }

        return false;
    }

    private bool IsAdditive(EntityUid item, ADTCookingSettingsPrototype settings)
    {
        foreach (var group in settings.FlavorGroups)
        {
            if (InGroup(group, item))
                return true;
        }

        return false;
    }

    private static bool RecipeBonusPresent(Solution solution, ADTCookingRecipePrototype recipe)
    {
        foreach (var (reagent, amount) in recipe.BonusReagents)
        {
            if (solution.GetTotalPrototypeQuantity(reagent) >= amount)
                return true;
        }

        return false;
    }

    private void ApplyDish(EntityUid result, ADTCookingMethod method, int level, ADTCookingSettingsPrototype settings)
    {
        if (settings.Qualities.Count == 0)
            return;

        var qualityId = settings.Qualities[Math.Clamp(level, 0, settings.Qualities.Count - 1)];
        _dish.SetDish(result, method, qualityId);

        if (!_proto.TryIndex(qualityId, out var quality))
            return;

        ScaleNutrients(result, quality.NutritionMultiplier, settings);
    }

    private void ScaleNutrients(EntityUid food, float multiplier, ADTCookingSettingsPrototype settings)
    {
        if (MathHelper.CloseTo(multiplier, 1f))
            return;

        if (!TryComp<EdibleComponent>(food, out var edible))
            return;

        if (!_solution.TryGetSolution(food, edible.Solution, out var solnEnt, out var solution))
            return;

        foreach (var reagent in settings.NutrientReagents)
        {
            var amount = solution.GetTotalPrototypeQuantity(reagent);
            if (amount <= FixedPoint2.Zero)
                continue;

            var delta = amount * (multiplier - 1f);
            if (delta > FixedPoint2.Zero)
            {
                if (solution.AvailableVolume < delta)
                    _solution.SetCapacity(solnEnt.Value, solution.Volume + delta);

                _solution.TryAddReagent(solnEnt.Value, reagent.Id, delta);
            }
            else
            {
                _solution.RemoveReagent(solnEnt.Value, reagent.Id, -delta);
            }
        }
    }

    private void AddToFood(EntityUid food, Solution added)
    {
        if (!TryComp<EdibleComponent>(food, out var edible))
            return;

        if (!_solution.TryGetSolution(food, edible.Solution, out var solnEnt, out var solution))
            return;

        if (solution.AvailableVolume < added.Volume)
            _solution.SetCapacity(solnEnt.Value, solution.Volume + added.Volume);

        _solution.TryAddSolution(solnEnt.Value, added);
    }

    private bool CanAddToFood(EntityUid food)
    {
        return TryComp<EdibleComponent>(food, out var edible) && _solution.TryGetSolution(food, edible.Solution, out _, out _);
    }

    private bool CanSendSolution(EntityUid source)
    {
        return TryComp<SolutionTransferComponent>(source, out var transfer) && transfer.CanSend;
    }

    private Solution GetAddedReagents(EntityUid item, ADTCookingSettingsPrototype settings)
    {
        var added = new Solution();
        if (!TryComp<EdibleComponent>(item, out var edible) || !_solution.TryGetSolution(item, edible.Solution, out _, out var current))
            return added;

        Solution? baseline = null;
        if (MetaData(item).EntityPrototype is { } proto)
            _solution.TryGetSolution(proto, edible.Solution, out baseline);

        foreach (var reagent in current.Contents)
        {
            var id = reagent.Reagent.Prototype;
            if (settings.NutrientReagents.Contains(id))
                continue;

            var extra = reagent.Quantity - (baseline?.GetTotalPrototypeQuantity(id) ?? FixedPoint2.Zero);
            if (extra > FixedPoint2.Zero)
                added.AddReagent(reagent.Reagent, extra);
        }

        return added;
    }

    private static Solution NutrientsOnly(Solution source, ADTCookingSettingsPrototype settings)
    {
        var result = new Solution();
        foreach (var reagent in settings.NutrientReagents)
        {
            var amount = source.GetTotalPrototypeQuantity(reagent);
            if (amount > FixedPoint2.Zero)
                result.AddReagent(reagent.Id, amount);
        }

        return result;
    }

    private void SetFood(EntityUid food, Solution content)
    {
        if (!TryComp<EdibleComponent>(food, out var edible))
            return;

        if (!_solution.TryGetSolution(food, edible.Solution, out var solnEnt, out _))
            return;

        _solution.RemoveAllSolution(solnEnt.Value);
        _solution.SetCapacity(solnEnt.Value, content.Volume);
        _solution.TryAddSolution(solnEnt.Value, content);
    }

    private void CookFreeform(Entity<ADTCookwareComponent> ent, List<EntityUid> items, Solution? cookwareSolution, ADTCookingMethod method, int level, ADTCookingSettingsPrototype settings)
    {
        if (!settings.FreeformResults.TryGetValue(ent.Comp.Type, out var resultProto))
            return;

        var total = new Solution();
        var names = new List<string>();

        foreach (var item in items)
        {
            if (TryComp<EdibleComponent>(item, out var edible) && _solution.TryGetSolution(item, edible.Solution, out _, out var itemSolution))
                total.AddSolution(NutrientsOnly(itemSolution, settings), _proto);

            total.AddSolution(GetAddedReagents(item, settings), _proto);

            var name = Name(item);
            if (!names.Contains(name))
                names.Add(name);

            Del(item);
        }

        if (!MathHelper.CloseTo(settings.FreeformNutritionMultiplier, 1f))
            total.ScaleSolution(settings.FreeformNutritionMultiplier);

        var portions = 1;
        if (ent.Comp.Type == ADTCookwareType.Pot && cookwareSolution != null)
        {
            var water = GetWater(cookwareSolution, settings);
            portions = Math.Clamp((int) (water / settings.FreeformWaterPerPortion).Float(), 1, settings.FreeformMaxPortions);
            total.AddSolution(NutrientsOnly(cookwareSolution, settings), _proto);
        }

        var displayName = Loc.GetString($"adt-cooking-freeform-{ent.Comp.Type.ToString().ToLowerInvariant()}",
            ("ingredients", JoinNames(names)));

        for (var i = 0; i < portions; i++)
        {
            var portion = total.Clone();
            portion.ScaleSolution(1f / portions);

            var result = EntityManager.SpawnInContainerOrDrop(resultProto, ent, ADTCookwareComponent.ContainerId);
            SetFood(result, portion);
            _metaData.SetEntityName(result, displayName);
            ApplyDish(result, method, level, settings);
        }
    }

    private string JoinNames(List<string> names)
    {
        const int max = 3;
        if (names.Count <= max)
            return string.Join(", ", names);

        return Loc.GetString("adt-cooking-freeform-many", ("list", string.Join(", ", names.Take(max))));
    }

    private void Overcook(Entity<ADTCookwareComponent> ent, ADTCookingSettingsPrototype settings)
    {
        if (ent.Comp.SoupQuality is { } soupQuality && _proto.TryIndex(soupQuality, out var soupLevel) && settings.Qualities.Count > 0)
        {
            var lowerSoup = settings.Qualities[Math.Max(0, Math.Min(soupLevel.Level - 1, settings.Qualities.Count - 1))];
            ent.Comp.SoupQuality = lowerSoup;

            if (_proto.TryIndex(lowerSoup, out var lowerSoupQuality)
                && soupLevel.NutritionMultiplier > 0
                && _solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var potSoln, out var pot))
            {
                ScaleSolutionNutrients(pot, lowerSoupQuality.NutritionMultiplier / soupLevel.NutritionMultiplier, settings);
                _solution.UpdateChemicals(potSoln.Value, false);
            }
        }

        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            if (!TryComp<ADTDishComponent>(item, out var dish) || !_proto.TryIndex(dish.Quality, out var quality))
                continue;

            var lower = Math.Max(0, quality.Level - 1);
            if (lower >= settings.Qualities.Count || lower == quality.Level)
                continue;

            var lowerId = settings.Qualities[lower];
            _dish.SetDish(item, dish.Method, lowerId);

            if (_proto.TryIndex(lowerId, out var lowerQuality) && quality.NutritionMultiplier > 0)
                ScaleNutrients(item, lowerQuality.NutritionMultiplier / quality.NutritionMultiplier, settings);
        }

        SetStage(ent, ADTCookingStage.Overcooked);
        RefreshVisualTiming(ent);
    }

    private void Burn(Entity<ADTCookwareComponent> ent, ADTCookingSettingsPrototype settings)
    {
        BurnContents(ent, settings);
        if (ent.Comp.SoupResult != null && settings.Qualities.Count > 0)
        {
            var previousQuality = ent.Comp.SoupQuality;
            var burntId = settings.Qualities[0];
            ent.Comp.SoupQuality = burntId;

            if (previousQuality is { } previousId
                && previousId != burntId
                && _proto.TryIndex(previousId, out var previous)
                && _proto.TryIndex(burntId, out var burnt)
                && previous.NutritionMultiplier > 0
                && _solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var potSoln, out var pot))
            {
                ScaleSolutionNutrients(pot, burnt.NutritionMultiplier / previous.NutritionMultiplier, settings);
                _solution.UpdateChemicals(potSoln.Value, false);
            }
        }

        SetStage(ent, ADTCookingStage.Burnt);
        RefreshVisualTiming(ent);
        _audio.PlayPvs(settings.BurnSound, ent);
        if (TryGetStoveSlot(ent, out var stove, out _))
            _popup.PopupEntity(Loc.GetString("adt-cookware-burnt", ("cookware", ent.Owner)), stove);
    }

    private void BurnContents(Entity<ADTCookwareComponent> ent, ADTCookingSettingsPrototype settings)
    {
        var items = new List<EntityUid>(ent.Comp.Contents.ContainedEntities);
        foreach (var item in items)
        {
            Del(item);
        }

        if (items.Count > 0)
            EntityManager.SpawnInContainerOrDrop(settings.BurntResult, ent, ADTCookwareComponent.ContainerId);
    }
}
