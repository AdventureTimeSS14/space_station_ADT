using Content.Shared.FixedPoint;
using Content.Server.ADT.Heretic.EntitySystems.PathSpecific;
using Content.Server.Body.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Effects;

public sealed partial class SpillBlood : EntityEffect
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Spills target blood.";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out BloodstreamComponent? bloodStream))
            return;

        if (!args.EntityManager.System<SharedSolutionContainerSystem>()
                .ResolveSolution(args.TargetEntity,
                    bloodStream.BloodSolutionName,
                    ref bloodStream.BloodSolution,
                    out var bloodSolution))
            return;

        args.EntityManager.System<PuddleSystem>()
            .TrySpillAt(args.TargetEntity, bloodSolution.SplitSolution(Amount), out _);
    }
}
