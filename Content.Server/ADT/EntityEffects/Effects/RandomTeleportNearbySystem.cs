using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.ADT.EntityEffects.Effects;

public sealed partial class RandomTeleportNearbySystem : EntityEffectSystem<TransformComponent, RandomTeleportEvent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<RandomTeleportEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;

        var mapCoords = _transform.GetMapCoordinates(uid, entity.Comp);

        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, ev.Range);

        if (entities.Count == 0)
            return;

        var canTarget = entities
            .Where(e => e.Owner != uid && _examine.InRangeUnOccluded(uid, e.Owner, ev.Range))
            .Select(e => e.Owner)
            .ToList();

        if (canTarget.Count == 0)
            return;

        foreach (var target in canTarget)
        {
            var targetXform = Transform(target);

            var angle = _random.NextDouble() * 2 * Math.PI;
            var distance = _random.NextDouble() * (ev.MaxRadius - ev.MinRadius) + ev.MinRadius;
            var offset = new Vector2((float)(Math.Cos(angle) * distance), (float)(Math.Sin(angle) * distance));

            var targetMapCoords = _transform.GetMapCoordinates(target, targetXform);
            var newPos = targetMapCoords.Position + offset;
            var mapId = targetMapCoords.MapId;

            if (_mapManager.TryFindGridAt(mapId, newPos, out var gridUid, out _))
            {
                var invMatrix = _transform.GetInvWorldMatrix(gridUid);
                var localPos = Vector2.Transform(newPos, invMatrix);
                _transform.SetCoordinates(target, targetXform, new EntityCoordinates(gridUid, localPos));
            }
            else
            {
                var mapEntity = _mapManager.GetMapEntityId(mapId);
                _transform.SetCoordinates(target, targetXform, new EntityCoordinates(mapEntity, newPos));
            }
        }
    }
}