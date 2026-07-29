using Content.Shared.ADT.EntityEffects;
using Content.Server.Body.Systems;
using Content.Server.Body.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class OxygenateNearbySystem : EntityEffectSystem<TransformComponent, OxygenateNearby>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<OxygenateNearby> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Range))
        {
            if (TryComp<RespiratorComponent>(target, out var respirator))
                _respirator.UpdateSaturation(target, effect.Factor, respirator);
        }
    }
}