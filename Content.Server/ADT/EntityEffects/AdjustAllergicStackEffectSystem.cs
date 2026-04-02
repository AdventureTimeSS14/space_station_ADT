using Content.Server.ADT.Body;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.ADT.EntityEffects;

/// <summary>
/// Эффект для изменения значения стака аллергии.
/// </summary>
public sealed partial class AdjustAllergicStackEntityEffectSystem : EntityEffectSystem<AllergicComponent, AdjustAllergicStack>
{
    [Dependency] private AllergicSystem _allergic = default!;

    protected override void Effect(Entity<AllergicComponent> entity, ref EntityEffectEvent<AdjustAllergicStack> args)
    {
        _allergic.AdjustAllergyStack(entity, args.Effect.Amount * args.Scale);
    }
}
