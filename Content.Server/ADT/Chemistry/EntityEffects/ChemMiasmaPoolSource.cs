using Content.Server.Atmos.Rotting;
using Content.Server.ADT.Disease;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Chemistry.ReagentEffects;

/// <summary>
/// The miasma system rotates between 1 disease at a time.
/// This gives all entities the disease the miasme system is currently on.
/// For things ingested by one person, you probably want ChemCauseRandomDisease instead.
/// </summary>
[UsedImplicitly]
public sealed partial class ChemMiasmaPoolSource : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-chem-miasma-pool", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs ev)
    {
        if (ev is not EntityEffectReagentArgs args)
            return;

        if (args.Scale < 0.1f)
            return;

        var disease = args.EntityManager.System<BkRottingSystem>().RequestPoolDisease();

        args.EntityManager.System<DiseaseSystem>().TryAddDisease(args.TargetEntity, disease);
    }
}
