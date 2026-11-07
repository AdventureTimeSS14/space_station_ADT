//

using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent]
public sealed partial class BladeArenaComponent : Component
{
    [DataField]
    public ProtoId<TagPrototype> WindowTag = "Window";

    [DataField]
    public List<ProtoId<TagPrototype>> NoReplaceTags = new()
    {
        "Diagonal",
        "Directional",
    };

    [DataField]
    public ProtoId<ContentTileDefinition> Tile = "PlatingRoseStone";

    [DataField]
    public EntProtoId WallReplacement = "WallHereticArena";

    [DataField]
    public EntProtoId<BladeArenaOuterWallComponent> OuterWall = "WallHereticArenaOuter";

    [DataField]
    public EntProtoId WindowReplacement = "WindowHereticArena";

    [DataField]
    public int Radius;

    [DataField]
    public EntityUid? Grid;

    [DataField]
    public List<EntityUid> DetachedEntities = new();

    [DataField]
    public List<EntityUid> SpawnedEntities = new();

    [DataField]
    public List<Vector2i> TilesToRestore = new();

    [DataField]
    public HashSet<EntityUid> Participants = new();

    [DataField]
    public int Layer = (int) (CollisionGroup.Impassable | CollisionGroup.HighImpassable |
                              CollisionGroup.MidImpassable | CollisionGroup.LowImpassable);

    [DataField]
    public ComponentRegistry ComponentsToAdd = new();

    [DataField]
    public EntityWhitelist? ParticipantWhitelist;

    [DataField]
    public EntityWhitelist? ParticipantBlacklist;
}
