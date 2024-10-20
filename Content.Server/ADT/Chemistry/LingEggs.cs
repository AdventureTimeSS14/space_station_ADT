using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Atmos.Miasma;
using Content.Shared.Changeling.Components;

namespace Content.Server.Chemistry;

public sealed partial class LingEggs : ReagentEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
  => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));

    // Gives the entity a component that turns it into a f#cking changeling
    public override void Effect(ReagentEffectArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.EnsureComponent<SyntLingEggsHolderComponent>(args.SolutionEntity);
    }
}
