using Content.Server.Fluids.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Effects;

// ADT: spills target's blood on the floor

public sealed partial class SpillBlood : EntityEffectBase<SpillBlood>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => "Spills target blood.";
}

public sealed partial class SpillBloodEffectSystem : EntityEffectSystem<BloodstreamComponent, SpillBlood>
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

    protected override void Effect(Entity<BloodstreamComponent> entity, ref EntityEffectEvent<SpillBlood> args)
    {
        var bloodStream = entity.Comp;

        if (!_solution.ResolveSolution(entity.Owner,
                bloodStream.BloodSolutionName,
                ref bloodStream.BloodSolution,
                out var bloodSolution))
            return;

        _puddle.TrySpillAt(entity.Owner, bloodSolution.SplitSolution(args.Effect.Amount), out _);
    }
}
