using Content.Shared.ADT.VendingMachines;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.ADT.VendingMachines;

public sealed class ADTVendingFoodNutrimentSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    private const string FoodSolution = "food";

    public void ReduceDispensedFood(EntityUid vendingMachine, EntityUid dispensed)
    {
        if (!TryComp<ADTVendingFoodNutrimentReductionComponent>(vendingMachine, out var reduction)
            || !_solution.TryGetSolution(dispensed, FoodSolution, out var soln, out var food)
            || soln is not { } solEnt || food is not { } solution)
            return;

        var nutriment = solution.GetTotalPrototypeQuantity(reduction.NutrimentReagent);
        if (nutriment <= 0)
            return;

        _solution.RemoveReagent(solEnt, reduction.NutrimentReagent, nutriment * reduction.NutrimentMultiplier);
        _solution.UpdateChemicals(solEnt);
    }
}