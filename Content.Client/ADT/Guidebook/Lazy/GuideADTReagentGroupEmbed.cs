using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Guidebook.Lazy;

[UsedImplicitly]
public sealed class GuideADTReagentGroupEmbed : ADTLazyList, IDocumentTag
{
    private const float EstimatedHeight = 150f;

    [Dependency] private IPrototypeManager _prototype = default!;

    public GuideADTReagentGroupEmbed()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Group", out var group))
            return false;

        var reagents = _prototype.EnumeratePrototypes<ReagentPrototype>()
            .Where(p => p.Group.Equals(group))
            .OrderBy(p => p.LocalizedName);

        foreach (var reagent in reagents)
        {
            var target = reagent;
            AddLazy(() => new GuideReagentEmbed(target),
                $"{reagent.LocalizedName} {reagent.LocalizedDescription}",
                reagent,
                EstimatedHeight);
        }

        control = this;
        return true;
    }
}
