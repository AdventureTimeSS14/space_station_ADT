using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Shared.ADT.MartialArts;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Guidebook;

public sealed class ADTGuideCombo : ADTGuideEntry, IDocumentTag
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceCache _resource = default!;

    private const string IntentsRsi = "/Textures/ADT/Interface/Misc/intents_big.rsi";

    public ADTGuideCombo()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("Combo", out var comboId))
            return false;

        if (!_proto.TryIndex<ComboPrototype>(comboId, out var combo))
            return false;

        foreach (var step in combo.AttackTypes)
        {
            var stateId = step.ToString().ToLowerInvariant();

            if (!_resource.TryGetResource<RSIResource>(new ResPath(IntentsRsi), out var rsi)
                || !rsi.RSI.TryGetState(stateId, out var state))
                continue;

            AddIcon(state.Frame0, Loc.GetString($"combo-intent-{stateId}"));
        }

        args.TryGetValue("Note", out var note);
        AddTitle(Loc.GetString(combo.Name), note);

        control = this;
        return true;
    }
}
