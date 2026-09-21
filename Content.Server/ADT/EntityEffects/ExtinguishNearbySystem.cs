using Content.Shared.ADT.EntityEffects;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ExtinguishNearbySystem : EntityEffectSystem<TransformComponent, ExtinguishNearby>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ExtinguishNearby> args)
    {
        var uid = entity.Owner;
        var range = args.Effect.Range;

        foreach (var target in _lookup.GetEntitiesInRange(uid, range))
        {
            if (TryComp<FlammableComponent>(target, out var flammable))
                _flammable.Extinguish(target, flammable);
        }
    }
}