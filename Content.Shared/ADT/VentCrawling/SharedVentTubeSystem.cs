using System.Linq;
using Content.Shared.ADT.VentCrawling.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Shared.ADT.VentCrawling;

public sealed class SharedVentTubeSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public EntityUid? NextTubeFor(EntityUid target, Direction nextDirection, VentCrawlerTubeComponent? targetTube = null)
    {
        if (!Resolve(target, ref targetTube))
            return null;
        var oppositeDirection = nextDirection.GetOpposite();

        var xform = Transform(target);
        if (xform.GridUid == null)
            return null;

        if (!TryComp<MapGridComponent>(xform.GridUid.Value, out var grid))
            return null;

        var position = xform.Coordinates;
        foreach (EntityUid entity in _mapSystem.GetInDir(xform.GridUid.Value, grid ,position, nextDirection))
        {

            if (!TryComp(entity, out VentCrawlerTubeComponent? tube)
                || !CanConnect(target, targetTube, nextDirection)
                || !CanConnect(entity, tube, oppositeDirection))
                continue;

            return entity;
        }

        return null;
    }

    private bool CanConnect(EntityUid tubeId, VentCrawlerTubeComponent tube, Direction direction)
    {
        if (!tube.Connected)
            return false;

        var ev = new GetVentCrawlingsConnectableDirectionsEvent();
        RaiseLocalEvent(tubeId, ref ev);
        return ev.Connectable.Contains(direction);
    }
}
