using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Kitchen.Components;

namespace Content.Server.GuideGenerator;

public sealed class GrindRecipeEntry
{
    /// <summary>
    ///     Id of grindable item
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; }

    /// <summary>
    ///     Human-readable name of recipe.
    ///     Should automatically be localized by default
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    ///     Type of recipe
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; }

    /// <summary>
    ///     Item that will be grinded into something
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; }

    /// <summary>
    ///     Dictionary of reagents that entity contains; aka "Recipe Result"
    /// </summary>
    [JsonPropertyName("result")]
    public Dictionary<string, int>? Result { get; } = new Dictionary<string, int>();


    public GrindRecipeEntry(EntityPrototype proto)
    {
        Id = proto.ID;
        Name = TextTools.TextTools.CapitalizeString(proto.Name);
        Type = "grindableRecipes";
        Input = proto.ID;
        var foodSolutionName = "food"; // default to food because everything in prototypes defaults to "food"

        // Now, to become a recipe, entity must:
        // A) Have "Extractable" component on it.
        // B) Have a solution with the name declared in "Extractable.GrindableSolutionName".
        // F) Have "Food" in its name (see Content.Server/Corvax/GuideGenerator/MealsRecipesJsonGenerator.cs)
        if (proto.Components.TryGetComponent("Extractable", out var extractableComp))
        {
            var extractable = (ExtractableComponent) extractableComp;
            foodSolutionName = extractable.GrindableSolutionName;
            var solutionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedSolutionContainerSystem>();

            if (foodSolutionName != null && solutionSystem.TryGetSolution(proto, foodSolutionName, out var solution))
            {
                foreach (ReagentQuantity reagent in solution.Contents)
                {
                    Result[reagent.Reagent.Prototype] = reagent.Quantity.Int();
                }
            }
            else
            {
                Result = null;
            }
        }
    }
}
