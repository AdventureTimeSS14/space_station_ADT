using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Atmos.Miasma;


namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class Embalm : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
      => Loc.GetString("reagent-effect-guidebook-embalm", ("chance", Probability));

    // Gives the entity a component that prevents rotting and also execution by defibrillator
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        entityManager.EnsureComponent<EmbalmedComponent>(args.TargetEntity);
    }
}