using System.Diagnostics.CodeAnalysis;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Actions.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Guidebook;

public sealed class ADTGuideAction : ADTGuideEntry, IDocumentTag
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private readonly SpriteSystem _sprite;

    public ADTGuideAction()
    {
        IoCManager.InjectDependencies(this);
        _sprite = _systems.GetEntitySystem<SpriteSystem>();
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!args.TryGetValue("Action", out var actionId))
            return false;

        if (!_proto.TryIndex<EntityPrototype>(actionId, out var proto))
            return false;

        if (proto.TryGetComponent<ActionComponent>(out var action, _componentFactory)
            && action.Icon is { } icon)
        {
            AddIcon(_sprite.Frame0(icon));
        }

        args.TryGetValue("Note", out var note);
        AddTitle(proto.Name, note);

        control = this;
        return true;
    }
}
