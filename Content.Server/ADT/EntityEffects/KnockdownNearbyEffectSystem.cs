using Content.Shared.ADT.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class KnockdownNearbyEffectSystem : EntityEffectSystem<TransformComponent, KnockdownNearbyEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<KnockdownNearbyEffect> args)
    {
        foreach (var target in _lookup.GetEntitiesInRange(entity.Owner, args.Effect.Radius))
        {
            if (!HasComp<StandingStateComponent>(target))
                continue;

            _stun.TryKnockdown(target, args.Effect.Time, drop: true);
        }
    }
}