using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class ExplosionReactionEffectSystem : EntityEffectSystem<TransformComponent, ExplosionEvent>
{
    [Dependency] private readonly SharedExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ExplosionEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;

        _explosion.QueueExplosion(
            uid,
            ev.ExplosionType,
            ev.MaxIntensity * args.Scale,
            ev.IntensitySlope,
            ev.MaxTotalIntensity,
            ev.TileBreakScale,
            canCreateVacuum: false,
            user: null,
            addLog: true
        );
    }
}