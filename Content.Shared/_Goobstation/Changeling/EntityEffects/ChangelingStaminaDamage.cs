// ADT-Tweak
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Changeling.EntityEffects;

public sealed partial class ChangelingStaminaDamage : EntityEffectBase<ChangelingStaminaDamage>
{
    [DataField]
    public float Amount = 5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-changeling-stamina-damage", ("chance", Probability), ("amount", Amount));
}

public sealed partial class ChangelingStaminaDamageSystem : EntityEffectSystem<StaminaComponent, ChangelingStaminaDamage>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<ChangelingStaminaDamage> args)
    {
        _stamina.TakeStaminaDamage(entity.Owner, args.Effect.Amount * args.Scale, entity.Comp, visual: false);
    }
}
