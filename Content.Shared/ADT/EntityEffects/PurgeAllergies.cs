using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Эффект для удаления аллергий, аллергены которых присутствуют в крови.
/// </summary>
public sealed partial class PurgeAllergiesEntityEffectSystem : EntityEffectSystem<AllergicComponent, PurgeAllergies>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    protected override void Effect(Entity<AllergicComponent> entity, ref EntityEffectEvent<PurgeAllergies> args)
    {
        if (!TryComp<BloodstreamComponent>(entity, out var bloodstreamComp))
            return;

        (EntityUid uid, _) = entity;
        ref List<ProtoId<ReagentPrototype>> allergicTriggers = ref entity.Comp.Triggers;

        if (_solutionContainerSystem.TryGetSolution(uid, BloodstreamComponent.DefaultBloodSolutionName, out _, out var chemicalsSolution))
        {
            foreach (var reagent in chemicalsSolution.Contents)
            {
                if (allergicTriggers.Contains(reagent.Reagent.Prototype))
                    allergicTriggers.Remove(reagent.Reagent.Prototype);
            }
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class PurgeAllergies : EntityEffectBase<PurgeAllergies>
{
    public override string? EntityEffectGuidebookText(
        IPrototypeManager prototype,
        IEntitySystemManager entSys)
    {
        return Loc.GetString(
            "reagent-effect-guidebook-purge-allergies",
            ("chance", Probability));
    }
}
