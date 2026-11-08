using Content.Shared.ADT.EntityEffects;
using Content.Server.ADT.Stealth;
using Content.Shared.EntityEffects;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ForceStealthNearbyEffectSystem : EntityEffectSystem<TransformComponent, ForceStealthNearbyEffect>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ForcedStealthSystem _stealth = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ForceStealthNearbyEffect> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Radius))
        {
            if (HasComp<GhostComponent>(target))
                continue;

            if (_random.Prob(effect.Chance))
                _stealth.TryApplyForceStealth(target, out _, effect.Duration);
        }
    }
}
