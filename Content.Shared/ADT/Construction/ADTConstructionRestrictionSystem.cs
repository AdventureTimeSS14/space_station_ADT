using Content.Shared.ADT.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction;

public sealed class ADTConstructionRestrictionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private HashSet<string>? _recipeGraphs;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<ConstructionPrototype>())
            return;

        _recipeGraphs = null;

        var query = EntityQueryEnumerator<ADTConstructionRestrictionComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            comp.AllowedGraphs = null;
        }
    }

    public bool CanConstruct(EntityUid user, ConstructionPrototype recipe)
    {
        if (!TryComp<ADTConstructionRestrictionComponent>(user, out var restriction))
            return true;

        return IsAllowed((user, restriction), recipe);
    }

    public bool CanUseGraph(EntityUid user, string graph)
    {
        if (!TryComp<ADTConstructionRestrictionComponent>(user, out var restriction))
            return true;

        if (_recipeGraphs == null)
        {
            _recipeGraphs = new();
            foreach (var recipe in _proto.EnumeratePrototypes<ConstructionPrototype>())
            {
                _recipeGraphs.Add(recipe.Graph);
            }
        }

        if (!_recipeGraphs.Contains(graph))
            return true;

        if (restriction.AllowedGraphs == null)
        {
            restriction.AllowedGraphs = new();
            foreach (var recipe in _proto.EnumeratePrototypes<ConstructionPrototype>())
            {
                if (IsAllowed((user, restriction), recipe))
                    restriction.AllowedGraphs.Add(recipe.Graph);
            }
        }

        return restriction.AllowedGraphs.Contains(graph);
    }

    private bool IsAllowed(Entity<ADTConstructionRestrictionComponent> user, ConstructionPrototype recipe)
    {
        if (user.Comp.AllowedRecipes.Contains(recipe.ID))
            return true;

        return recipe.EntityWhitelist != null && _whitelist.IsWhitelistPass(recipe.EntityWhitelist, user);
    }
}
