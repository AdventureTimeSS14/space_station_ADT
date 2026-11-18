using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Content.Client.ADT.Guidebook.Lazy;
using Content.Client.Guidebook.Richtext;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Cooking.Guidebook;

[UsedImplicitly]
public sealed partial class GuideADTCookingGroupEmbed : ADTLazyList, IDocumentTag
{
    private const float EstimatedHeight = 140f;

    [Dependency] private IPrototypeManager _prototype = default!;

    public GuideADTCookingGroupEmbed()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Group", out var group))
            return false;

        var recipes = new List<(ADTCookingRecipePrototype Recipe, EntityPrototype Result)>();
        foreach (var recipe in _prototype.EnumeratePrototypes<ADTCookingRecipePrototype>())
        {
            if (recipe.Group != group
                || recipe.HideInGuidebook
                || recipe.Result is not { } resultId
                || !_prototype.TryIndex(resultId, out var result))
            {
                continue;
            }

            recipes.Add((recipe, result));
        }

        foreach (var (recipe, result) in recipes.OrderBy(pair => pair.Result.Name))
        {
            var target = recipe;
            AddLazy(() => new GuideADTCookingEmbed(target), GetSearchText(recipe, result), result, EstimatedHeight);
        }

        control = this;
        return true;
    }

    private string GetSearchText(ADTCookingRecipePrototype recipe, EntityPrototype result)
    {
        var text = new StringBuilder(result.Name);
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.Id is { } id && _prototype.TryIndex(id, out var entity))
                text.Append(' ').Append(entity.Name);
        }

        foreach (var reagent in recipe.Reagents.Keys)
        {
            if (_prototype.TryIndex<ReagentPrototype>(reagent, out var prototype))
                text.Append(' ').Append(prototype.LocalizedName);
        }

        return text.ToString();
    }
}
