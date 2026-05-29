using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.ADT.Trail;
internal static class OcclusionHelper
{
    private const float MaxRaycastRange = 100f;
    public static bool IsOccluded(
        this OccluderSystem occluderSystem,
        Vector2 cameraPos,
        Vector2 targetPos,
        MapId mapId)
    {
        if (mapId == MapId.Nullspace)
            return false;

        var distance = Vector2.Distance(cameraPos, targetPos);

        if (distance > MaxRaycastRange)
            return true;

        var origin = new MapCoordinates(cameraPos, mapId);
        var target = new MapCoordinates(targetPos, mapId);

        return !occluderSystem.InRangeUnoccluded(origin, target, distance, ignoreTouching: true);
    }
}
