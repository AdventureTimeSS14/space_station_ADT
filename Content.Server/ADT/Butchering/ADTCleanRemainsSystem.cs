using Content.Server.Body.Systems;
using Content.Shared.ADT.Butchering;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Gibbing;

namespace Content.Server.ADT.Butchering;

public sealed class ADTCleanRemainsSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCleanRemainsComponent, BeingGibbedEvent>(OnBeingGibbed, after: [typeof(BodySystem)]);
        SubscribeLocalEvent<ADTCleanRemainsComponent, GibbedBeforeDeletionEvent>(OnBeforeDeletion,
            before: [typeof(BloodstreamSystem)]);
    }

    private void OnBeingGibbed(Entity<ADTCleanRemainsComponent> ent, ref BeingGibbedEvent args)
    {
        if (!ent.Comp.KeepOrgans)
            return;

        args.Giblets.RemoveWhere(giblet => HasComp<OrganComponent>(giblet));
    }

    private void OnBeforeDeletion(Entity<ADTCleanRemainsComponent> ent, ref GibbedBeforeDeletionEvent args)
    {
        if (!ent.Comp.NoBloodSpill || !TryComp<BloodstreamComponent>(ent, out var bloodstream))
            return;

        Drain(ent, bloodstream.BloodSolutionName);
        Drain(ent, bloodstream.BloodTemporarySolutionName);
    }

    private void Drain(EntityUid uid, string solutionName)
    {
        if (_solution.TryGetSolution(uid, solutionName, out var solution))
            _solution.RemoveAllSolution(solution.Value);
    }
}
