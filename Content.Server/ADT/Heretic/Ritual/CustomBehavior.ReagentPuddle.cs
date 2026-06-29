using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids.Components;
using Content.Shared.Heretic.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualReagentPuddleBehavior : RitualCustomBehavior
{

    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private EntityLookupSystem _lookup = default!;

    [DataField] public ProtoId<ReagentPrototype>? Reagent;

    private List<EntityUid> _uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        outstr = null;

        if (Reagent == null)
            return true;

        _lookup = args.EntityManager.System<EntityLookupSystem>();

        var lookup = _lookup.GetEntitiesInRange(args.Platform, 1.5f);

        foreach (var ent in lookup)
        {
            if (!args.EntityManager.TryGetComponent(ent, out PuddleComponent? puddle))
                continue;

            if (puddle.Solution == null)
                continue;

            if (!args.EntityManager.TryGetComponent(puddle.Solution.Value, out SolutionComponent? solutionComp))
                continue;

            var hasReagent = false;
            foreach (var reagent in solutionComp.Solution.Contents)
            {
                if (reagent.Reagent.Prototype == Reagent.Value)
                {
                    hasReagent = true;
                    break;
                }
            }

            if (!hasReagent)
                continue;

            _uids.Add(ent);
        }

        if (_uids.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-reagentpuddle", ("reagentname", Reagent!));
            return false;
        }

        return true;
    }

    public override void Finalize(RitualData args)
    {
        foreach (var uid in _uids)
            args.EntityManager.QueueDeleteEntity(uid);

        _uids.Clear();
    }
}
