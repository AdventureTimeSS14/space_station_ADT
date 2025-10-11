using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects;

[UsedImplicitly]
public sealed partial class SpeciesChange : EntityEffect
{
    /// <summary>
    ///     What sex is the consumer changed to? If not set then swap between male/female.
    /// </summary>
    [DataField("sex")]
    public ProtoId<SpeciesPrototype>? NewSpecies;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var appearance))
            return;

        var uid = args.TargetEntity;
        var newSpecies = NewSpecies;
        var humanoidAppearanceSystem = args.EntityManager.System<SharedHumanoidAppearanceSystem>();


        // Eventually this should also add the slime sub-species.
        if (newSpecies.HasValue)
            humanoidAppearanceSystem.SetSpecies(uid, newSpecies.Value);
    }
}
