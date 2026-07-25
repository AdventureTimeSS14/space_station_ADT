using System.Linq;
using System.Numerics;
using Content.Server.Procedural;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed class ADTLavalandGenerationSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTLavalandGenerationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTLavalandGenerationComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        var placed = new List<Vector2>();

        foreach (var group in comp.Groups)
        {
            if (group.Prototypes.Count == 0 || group.Count <= 0)
                continue;

            for (var i = 0; i < group.Count; i++)
            {
                if (!TryFindSpot(ent, group, placed, out var coords))
                    continue;

                var proto = _random.Pick(group.Prototypes);
                Spawn(proto, coords);

                placed.Add(coords.Position);
            }
        }
    }

    private bool TryFindSpot(
        Entity<ADTLavalandGenerationComponent> ent,
        LavalandScatterGroup group,
        List<Vector2> placed,
        out EntityCoordinates coords)
    {
        var comp = ent.Comp;
        var minCenter = MathF.Max(group.MinDistanceFromCenter, comp.MinRadius);

        for (var attempt = 0; attempt < comp.MaxAttempts; attempt++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(minCenter, comp.MaxRadius);
            var offset = new Vector2(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);

            coords = new EntityCoordinates(ent.Owner, comp.BaseCenter + offset);

            if (IsValidSpot(coords, group, placed))
                return true;
        }

        coords = default;
        return false;
    }

    private bool IsValidSpot(EntityCoordinates coords, LavalandScatterGroup group, List<Vector2> placed)
    {
        var position = coords.Position;

        var spacingSq = group.MinSpacing * group.MinSpacing;
        foreach (var other in placed)
        {
            if (Vector2.DistanceSquared(position, other) < spacingSq)
                return false;
        }

        if (group.AvoidRooms &&
            _lookup.GetEntitiesInRange(coords, group.MinSpacing).Any(e => HasComp<RoomFillComponent>(e)))
        {
            return false;
        }

        return true;
    }
}
