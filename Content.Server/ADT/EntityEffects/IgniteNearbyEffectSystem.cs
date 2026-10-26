using Content.Shared.ADT.EntityEffects;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class IgniteNearbyEffectSystem : EntityEffectSystem<TransformComponent, IgniteNearbyEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<IgniteNearbyEffect> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Radius))
        {
            if (TryComp<FlammableComponent>(target, out var flammable))
                _flammable.AdjustFireStacks(target, effect.FireStacks, flammable, true);
        }
    }
}