using Content.Shared.ADT.EntityEffects;
using Content.Shared.EntityEffects;
using Content.Server.ADT.NPC;
using Content.Shared.Ghost;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ChangeFactionNearbyEffectSystem : EntityEffectSystem<TransformComponent, ChangeFactionNearbyEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ChangeFactionStatusEffectSystem _changeFaction = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ChangeFactionNearbyEffect> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Radius))
        {
            if (HasComp<GhostComponent>(target))
                continue;

            _changeFaction.TryChangeFaction(target, effect.NewFaction, out _, effect.Duration);
        }
    }
}