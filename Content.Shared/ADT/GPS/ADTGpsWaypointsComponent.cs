using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared.ADT.GPS;

[RegisterComponent]
public sealed partial class ADTGpsWaypointsComponent : Component
{
    [DataField]
    public int MaxWaypoints = 3;

    [DataField]
    public int MaxNameLength = 12;

    [ViewVariables]
    public List<ADTGpsWaypoint> Waypoints = new();
}

public sealed class ADTGpsWaypoint
{
    public string Name = string.Empty;

    public MapId Map;

    public Vector2 Position;

    public ADTGpsWaypoint(string name, MapId map, Vector2 position)
    {
        Name = name;
        Map = map;
        Position = position;
    }
}
