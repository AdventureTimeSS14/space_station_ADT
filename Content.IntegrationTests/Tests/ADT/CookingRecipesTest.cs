using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.ADT;

[TestFixture]
public sealed class CookingRecipesTest : GameTest
{
    private const string Stove = "ADTKitchenElectricRange";
    private const string SecretStove = "ADTSyndicateKitchenElectricRange";
    private const string Plate = "FoodPlate";
    private const string Beaker = "Beaker";
    private const string Bowl = "FoodBowlBig";
    private const string BeakerSolution = "beaker";
    private const string User = "MobHuman";
    private const string PanOil = "ADTOilSunflower";
    private const string PotWater = "Water";
    private const float OilAmount = 10f;
    private const float WaterAmount = 20f;
    private const int MaxCookSeconds = 300;

    private static readonly Dictionary<ADTCookwareType, string> Cookware = new()
    {
        [ADTCookwareType.Pan] = "ADTCookwarePan",
        [ADTCookwareType.Pot] = "ADTCookwarePot",
        [ADTCookwareType.Tray] = "ADTCookwareTray",
    };

    private sealed class Run
    {
        public ADTCookingRecipePrototype Recipe = default!;
        public EntityUid Cookware;
        public bool Done;
        public string? Error;
    }

    [Test]
    public async Task RecipesFitIntoCookware()
    {
        var server = Pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();
        var solutions = server.ResolveDependency<IEntityManager>().System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var plateItems = MaxItems(protoMan, factory, solutions, Plate, out _);

            Assert.Multiple(() =>
            {
                foreach (var recipe in protoMan.EnumeratePrototypes<ADTCookingRecipePrototype>())
                {
                    var count = recipe.Ingredients.Sum(i => i.Count);

                    if (recipe.Cookware == ADTCookwareType.Plate)
                    {
                        Assert.That(count, Is.LessThanOrEqualTo(plateItems),
                            $"{recipe.ID}: нужно {count} предметов, а на тарелку влезает {plateItems}.");
                        continue;
                    }

                    var maxItems = MaxItems(protoMan, factory, solutions, Cookware[recipe.Cookware], out var maxVolume);
                    Assert.That(count, Is.LessThanOrEqualTo(maxItems),
                        $"{recipe.ID}: нужно {count} предметов, а в {Cookware[recipe.Cookware]} влезает {maxItems}.");

                    var volume = recipe.Reagents.Values.Aggregate(FixedPoint2.Zero, (a, b) => a + b) + MediumAmount(recipe);
                    Assert.That(volume, Is.LessThanOrEqualTo(maxVolume),
                        $"{recipe.ID}: нужно {volume}u жидкости вместе со средой, а в {Cookware[recipe.Cookware]} влезает {maxVolume}u.");
                }
            });
        });
    }

    [Test]
    public async Task RecipesCookOnStove()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var containers = entMan.System<SharedContainerSystem>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();
        var interaction = entMan.System<SharedInteractionSystem>();
        var power = entMan.System<SharedPowerReceiverSystem>();

        var map = await Pair.CreateTestMap();
        var runs = new List<Run>();
        EntityUid user = default;

        await server.WaitPost(() =>
        {
            var coords = map.GridCoords;
            user = entMan.SpawnEntity(User, coords);
            var settings = protoMan.Index<ADTCookingSettingsPrototype>(ADTCookingSettingsPrototype.DefaultId);

            foreach (var recipe in protoMan.EnumeratePrototypes<ADTCookingRecipePrototype>().OrderBy(r => r.ID))
            {
                if (recipe.Cookware == ADTCookwareType.Plate)
                    continue;

                var run = new Run { Recipe = recipe };
                runs.Add(run);

                var stove = entMan.SpawnEntity(recipe.Secret ? SecretStove : Stove, coords);
                power.SetNeedsPower(stove, false);
                var stoveComp = entMan.GetComponent<ADTStoveComponent>(stove);

                var wanted = recipe.Cookware == ADTCookwareType.Tray ? ADTStoveSlotType.Oven : ADTStoveSlotType.Burner;
                var slot = stoveComp.Slots.FindIndex(s => s.Type == wanted);
                if (slot < 0)
                {
                    run.Error = $"на плите нет слота {wanted}";
                    continue;
                }

                stoveComp.Slots[slot].Heat = wanted == ADTStoveSlotType.Oven ? settings.OvenHeat : ADTStoveHeat.Low;

                var cookware = entMan.SpawnEntity(Cookware[recipe.Cookware], coords);
                run.Cookware = cookware;
                var cookwareComp = entMan.GetComponent<ADTCookwareComponent>(cookware);

                if (!containers.TryGetContainer(stove, ADTStoveComponent.SlotContainerId(slot), out var slotContainer)
                    || !containers.Insert(cookware, slotContainer))
                {
                    run.Error = "посуда не ставится на плиту";
                    continue;
                }

                if (!solutions.TryGetSolution(cookware, cookwareComp.Solution, out var solution, out _))
                {
                    run.Error = "у посуды нет раствора";
                    continue;
                }

                var reagents = new Dictionary<string, FixedPoint2>();
                foreach (var (reagent, amount) in recipe.Reagents)
                {
                    reagents[reagent.Id] = reagents.GetValueOrDefault(reagent.Id) + amount;
                }

                if (recipe.Cookware == ADTCookwareType.Pot)
                    reagents[PotWater] = reagents.GetValueOrDefault(PotWater) + WaterAmount;
                else if (recipe.Cookware == ADTCookwareType.Pan && recipe.RequireMedium)
                    reagents[PanOil] = reagents.GetValueOrDefault(PanOil) + OilAmount;

                foreach (var (reagent, amount) in reagents)
                {
                    if (!solutions.TryAddReagent(solution.Value, new ReagentQuantity(reagent, amount, null), out var accepted) || accepted < amount)
                    {
                        run.Error = $"не влезает {reagent} {amount}u";
                        break;
                    }
                }

                if (run.Error != null)
                    continue;

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Id == null)
                        continue;

                    for (var i = 0; i < ingredient.Count; i++)
                    {
                        var item = entMan.SpawnEntity(ingredient.Id.Value.Id, coords);
                        interaction.InteractUsing(user, item, cookware, coords, checkCanInteract: false, checkCanUse: false);

                        if (!cookwareComp.Contents.Contains(item))
                        {
                            run.Error = $"ингредиент {ingredient.Id.Value} не кладётся в посуду";
                            break;
                        }
                    }

                    if (run.Error != null)
                        break;
                }
            }
        });

        for (var second = 0; second < MaxCookSeconds; second++)
        {
            await RunSeconds(1);

            var pending = false;
            await server.WaitPost(() =>
            {
                foreach (var run in runs)
                {
                    if (run.Done || run.Error != null)
                        continue;

                    if (!entMan.TryGetComponent<ADTCookwareComponent>(run.Cookware, out var comp))
                    {
                        run.Error = "посуда пропала";
                        continue;
                    }

                    if (comp.Stage is ADTCookingStage.Idle or ADTCookingStage.Cooking)
                    {
                        pending = true;
                        continue;
                    }

                    run.Done = true;
                    run.Error = CheckResult(entMan, run.Recipe, comp);

                    if (run.Error == null && run.Recipe.Cookware == ADTCookwareType.Pot && run.Recipe.Result is { } soup)
                        run.Error = CheckLadle(entMan, interaction, solutions, user, run.Cookware, soup.Id);
                }
            });

            if (!pending)
                break;
        }

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var run in runs)
                {
                    if (run.Error != null)
                    {
                        Assert.Fail($"{run.Recipe.ID}: {run.Error}");
                        continue;
                    }

                    if (!run.Done)
                        Assert.Fail($"{run.Recipe.ID}: не приготовилось за {MaxCookSeconds} с (время рецепта {run.Recipe.Time} с).");
                }
            });
        });
    }

    [Test]
    public async Task RecipesAssembleOnPlate()
    {
        var server = Pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var interaction = entMan.System<SharedInteractionSystem>();
        var solutions = entMan.System<SharedSolutionContainerSystem>();

        var map = await Pair.CreateTestMap();
        var errors = new List<string>();

        var recipes = protoMan.EnumeratePrototypes<ADTCookingRecipePrototype>()
            .Where(r => r.Cookware == ADTCookwareType.Plate && !r.Secret)
            .OrderBy(r => r.ID)
            .ToList();

        foreach (var recipe in recipes)
        {
            EntityUid plate = default;
            EntityCoordinates coords = default;
            HashSet<EntityUid> before = new();

            await server.WaitPost(() =>
            {
                coords = map.GridCoords;
                var user = entMan.SpawnEntity(User, coords);
                plate = entMan.SpawnEntity(Plate, coords);
                var plateComp = entMan.GetComponent<ADTPlateComponent>(plate);

                if (recipe.Result is { } result)
                    before = Existing(entMan, result.Id);

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Id == null)
                        continue;

                    for (var i = 0; i < ingredient.Count; i++)
                    {
                        var item = entMan.SpawnEntity(ingredient.Id.Value.Id, coords);
                        interaction.InteractUsing(user, item, plate, coords, checkCanInteract: false, checkCanUse: false);

                        if (!plateComp.Contents.Contains(item))
                        {
                            errors.Add($"{recipe.ID}: ингредиент {ingredient.Id.Value} не кладётся на тарелку");
                            return;
                        }
                    }
                }

                if (recipe.Reagents.Count > 0)
                {
                    var beaker = entMan.SpawnEntity(Beaker, coords);
                    if (!solutions.TryGetSolution(beaker, BeakerSolution, out var beakerSolution, out _))
                    {
                        errors.Add($"{recipe.ID}: у {Beaker} нет раствора");
                        return;
                    }

                    foreach (var (reagent, amount) in recipe.Reagents)
                    {
                        solutions.TryAddReagent(beakerSolution.Value, new ReagentQuantity(reagent.Id, amount, null), out _);
                    }

                    interaction.InteractUsing(user, beaker, plate, coords, checkCanInteract: false, checkCanUse: false);
                    entMan.DeleteEntity(beaker);
                }

                entMan.DeleteEntity(user);
            });

            await RunTicksSync(2);

            await server.WaitPost(() =>
            {
                if (recipe.Result is { } result)
                {
                    if (recipe.ConsumePlate)
                    {
                        if (!Existing(entMan, result.Id).Except(before).Any())
                            errors.Add($"{recipe.ID}: на тарелке не собралось {result.Id}");

                        return;
                    }

                    var count = entMan.TryGetComponent<ADTPlateComponent>(plate, out var resultPlate)
                        ? resultPlate.Contents.ContainedEntities.Count(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == result.Id)
                        : 0;

                    if (count < recipe.ResultCount)
                        errors.Add($"{recipe.ID}: на тарелке {count} из {recipe.ResultCount} {result.Id}");

                    return;
                }

                if (recipe.ServingName != null
                    && (!entMan.TryGetComponent<ADTPlateComponent>(plate, out var plateComp) || plateComp.Serving != recipe.ID))
                {
                    errors.Add($"{recipe.ID}: тарелка не стала сервировкой рецепта");
                }
            });

            await server.WaitPost(() =>
            {
                if (entMan.EntityExists(plate))
                    entMan.DeleteEntity(plate);
            });
        }

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    private static string? CheckResult(IEntityManager entMan, ADTCookingRecipePrototype recipe, ADTCookwareComponent comp)
    {
        if (comp.Stage != ADTCookingStage.Done)
            return $"готовка закончилась стадией {comp.Stage}";

        if (comp.TargetKind != ADTCookingTargetKind.Recipe || comp.Recipe?.Id != recipe.ID)
            return $"сработал не тот рецепт: {comp.TargetKind} {comp.Recipe?.Id ?? "-"}";

        if (recipe.Result is not { } result)
            return null;

        if (recipe.Cookware == ADTCookwareType.Pot)
        {
            return comp.SoupResult?.Id == result.Id
                ? null
                : $"в кастрюле суп {comp.SoupResult?.Id ?? "-"}, а ожидался {result.Id}";
        }

        var count = comp.Contents.ContainedEntities.Count(e => entMan.GetComponent<MetaDataComponent>(e).EntityPrototype?.ID == result.Id);
        return count >= recipe.ResultCount
            ? null
            : $"в посуде {count} из {recipe.ResultCount} {result.Id}";
    }

    private static string? CheckLadle(IEntityManager entMan, SharedInteractionSystem interaction, SharedSolutionContainerSystem solutions, EntityUid user, EntityUid pot, string soup)
    {
        var coords = entMan.GetComponent<TransformComponent>(user).Coordinates;
        var before = Existing(entMan, soup);
        var bowl = entMan.SpawnEntity(Bowl, coords);
        interaction.InteractUsing(user, bowl, pot, coords, checkCanInteract: false, checkCanUse: false);

        var portion = Existing(entMan, soup).Except(before).FirstOrDefault();
        if (!portion.IsValid())
            return "суп не разливается в миску";

        if (!entMan.TryGetComponent<EdibleComponent>(portion, out var edible)
            || !solutions.TryGetSolution(portion, edible.Solution, out _, out var solution)
            || solution.Volume <= FixedPoint2.Zero)
        {
            return "в разлитой порции супа пусто";
        }

        entMan.DeleteEntity(portion);
        return null;
    }

    private static HashSet<EntityUid> Existing(IEntityManager entMan, string proto)
    {
        var set = new HashSet<EntityUid>();
        var query = entMan.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if (meta.EntityPrototype?.ID == proto)
                set.Add(uid);
        }

        return set;
    }

    private static FixedPoint2 MediumAmount(ADTCookingRecipePrototype recipe)
    {
        return recipe.Cookware switch
        {
            ADTCookwareType.Pot => WaterAmount,
            ADTCookwareType.Pan when recipe.RequireMedium => OilAmount,
            _ => FixedPoint2.Zero,
        };
    }

    private static int MaxItems(IPrototypeManager protoMan, IComponentFactory factory, SharedSolutionContainerSystem solutions, string proto, out FixedPoint2 maxVolume)
    {
        maxVolume = FixedPoint2.Zero;
        var entity = protoMan.Index<EntityPrototype>(proto);

        if (entity.TryGetComponent<ADTPlateComponent>(out var plate, factory))
            return plate.MaxItems;

        var cookware = entity.TryGetComponent<ADTCookwareComponent>(out var comp, factory) ? comp : null;
        if (cookware == null)
            return 0;

        if (solutions.TryGetSolution(entity, cookware.Solution, out var solution))
            maxVolume = solution.MaxVolume;

        return cookware.MaxItems;
    }
}
