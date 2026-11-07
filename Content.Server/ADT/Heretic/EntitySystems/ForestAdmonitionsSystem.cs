using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class ForestAdmonitionsSystem : SharedForestAdmonitionsSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private readonly HashSet<Entity<PhysicsComponent>> _lookupPhysics = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ShadowCloakedComponent, ForestAdmonitionsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var cloaked, out var forest, out var xform))
        {
            if (cloaked.ShadowCloakEntity != forest.CloakEntity)
                continue;

            if (now < forest.NextUpdate)
                continue;

            forest.NextUpdate = now + forest.UpdateDelay;
            SpreadFog((uid, forest, xform));
        }
    }

    private void SpreadFog(Entity<ForestAdmonitionsComponent, TransformComponent> ent)
    {
        var (_, forest, xform) = ent;

        var range = forest.Range;
        var limit = MathF.Sqrt(range * range + range * range);
        var inv = 1f / limit;
        var coords = xform.Coordinates;
        for (var y = -range; y <= range; y++)
        {
            for (var x = -range; x <= range; x++)
            {
                var offsetX = (float) x;
                var offsetY = (float) y;
                var length = MathF.Sqrt(offsetX * offsetX + offsetY * offsetY) * inv;
                var chance = MathF.Pow(1f - length, forest.FogSlope);

                if (!_random.Prob(Math.Clamp(chance, 0f, 1f)))
                    continue;

                var pos = coords.Offset(new Vector2(offsetX, offsetY)).SnapToGrid(EntityManager);
                var mapPos = XForm.ToMapCoordinates(pos);

                const int mask = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable);

                _lookupPhysics.Clear();
                _lookup.GetEntitiesInRange(mapPos, 0.1f, _lookupPhysics);
                if (_lookupPhysics.Any(e => (e.Comp.CollisionLayer & mask) != 0))
                    continue;

                Spawn(forest.FogProto, mapPos);
            }
        }
    }
}
