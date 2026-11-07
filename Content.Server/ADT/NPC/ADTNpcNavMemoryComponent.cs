using Content.Server.NPC.Pathfinding;
using Robust.Shared.Map;

namespace Content.Server.ADT.NPC;

[RegisterComponent, Access(typeof(ADTNpcNavMemorySystem))]
public sealed partial class ADTNpcNavMemoryComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<(EntityUid Grid, Vector2i Tile), TimeSpan> Blocked = new();

    [DataField]
    public TimeSpan DoorBlockTime = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan StuckBlockTime = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan ProgressTimeout = TimeSpan.FromSeconds(5);

    [DataField]
    public float ProgressDistance = 0.5f;

    [DataField]
    public int MaxStuckRepaths = 2;

    [ViewVariables]
    public EntityCoordinates Target;

    [ViewVariables]
    public PathPoly? LastPathEnd;

    [ViewVariables]
    public float BestRemaining = float.MaxValue;

    [ViewVariables]
    public TimeSpan LastProgress;

    [ViewVariables]
    public TimeSpan NextCheck;

    [ViewVariables]
    public int StuckCount;
}
