using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Examine;

namespace Content.Shared.ADT.Xenobiology.Systems;

// This handles slime extracts.
public sealed partial class SlimeExtractSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeExtractComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SlimeExtractComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<SlimeExtractComponent> ent, ref ComponentInit args)
    {
        if (_solution.TryGetRefillableSolution(ent.Owner, out var soln, out _))
            _solution.RemoveAllSolution(soln.Value);
    }

    private void OnExamined(Entity<SlimeExtractComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ReactiveComponent>(ent))
            args.PushMarkup(Loc.GetString("xeno-extract-reaction-unreactive"));
    }
}