using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class WashCreamPieReactionSystem : EntityEffectSystem<CreamPiedComponent, WashCreamPieReaction>
{
    [Dependency] private readonly SharedCreamPieSystem _creamPie = default!;

    protected override void Effect(Entity<CreamPiedComponent> entity, ref EntityEffectEvent<WashCreamPieReaction> args)
    {
        var uid = entity.Owner;
        _creamPie.SetCreamPied((entity, entity.Comp), false);
    }
}