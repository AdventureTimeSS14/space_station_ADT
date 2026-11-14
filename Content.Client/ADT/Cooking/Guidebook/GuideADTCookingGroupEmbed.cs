using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Richtext;
using Content.Shared.ADT.Cooking.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Cooking.Guidebook;

[UsedImplicitly]
public sealed partial class GuideADTCookingGroupEmbed : BoxContainer, IDocumentTag
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public GuideADTCookingGroupEmbed()
    {
        Orientation = LayoutOrientation.Vertical;
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Group", out var group))
            return false;

        var recipes = _prototype.EnumeratePrototypes<ADTCookingRecipePrototype>()
            .Where(p => p.Group == group && !p.HideInGuidebook && p.Result != null && _prototype.HasIndex<EntityPrototype>(p.Result.Value))
            .OrderBy(p => _prototype.Index<EntityPrototype>(p.Result!.Value).Name);

        foreach (var recipe in recipes)
        {
            AddChild(new GuideADTCookingEmbed(recipe));
        }

        control = this;
        return true;
    }
}
