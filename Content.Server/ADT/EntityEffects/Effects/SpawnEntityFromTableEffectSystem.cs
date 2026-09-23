using System.Numerics;
using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.EntityTable;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class SpawnEntityFromTableEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntityFromTable>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntityFromTable> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);
        var random = _robustRandom.GetRandom();

        for (var i = 0; i < quantity; i++)
        {
            var spawns = _entityTable.GetSpawns(args.Effect.EntityTable, random);
            foreach (var proto in spawns)
            {
                var randomOffset = new Vector2(
                    random.NextFloat(-args.Effect.Offset, args.Effect.Offset),
                    random.NextFloat(-args.Effect.Offset, args.Effect.Offset));
                var coords = _transform.GetMapCoordinates(entity.Owner, entity.Comp);
                var spawned = Spawn(proto, coords.Offset(randomOffset));
                _transform.AttachToGridOrMap(spawned);
            }
        }
    }
}