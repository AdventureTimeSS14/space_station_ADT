using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class CreateEntityReactionEffectSystem : EntityEffectSystem<TransformComponent, CreateEntityEvent>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<CreateEntityEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;
        var xform = entity.Comp;

        if (ev.Delay > 0)
        {
            var coords = _transform.GetMapCoordinates(uid, xform);
            _transform.AttachToGridOrMap(uid);
            for (var i = 0; i < ev.Number; i++)
            {
                var spawned = Spawn(ev.Entity, coords);
                _transform.AttachToGridOrMap(spawned);
            }
        }
        else
        {
            for (var i = 0; i < ev.Number; i++)
            {
                var mapCoords = _transform.GetMapCoordinates(uid, xform);
                var spawned = Spawn(ev.Entity, mapCoords);
                _transform.AttachToGridOrMap(spawned);
            }
        }
    }
}