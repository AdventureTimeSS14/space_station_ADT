using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class CreateRQuantityEntityReactionEffectSystem : EntityEffectSystem<TransformComponent, CreateRQuantityEntityEvent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<CreateRQuantityEntityEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;

        var quantity = _random.Next(1, ev.MaxEntities + 1);
        var mapCoords = _transform.GetMapCoordinates(uid, entity.Comp);

        for (var i = 0; i < quantity; i++)
        {
            var spawned = Spawn(ev.Entity, mapCoords);
            _transform.AttachToGridOrMap(spawned);
        }
    }
}