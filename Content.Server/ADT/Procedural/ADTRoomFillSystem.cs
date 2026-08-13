using Content.Server.Procedural;
using Content.Shared.Procedural;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Procedural;

public sealed class ADTRoomFillSystem : EntitySystem
{
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRoomFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTRoomFillComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid is { } gridUid)
        {
            var room = ResolveRoom(ent);

            if (room == null)
            {
                Log.Error($"Unable to find matching room prototype for {ToPrettyString(ent.Owner)}.");
            }
            else
            {
                var grid = Comp<MapGridComponent>(gridUid);
                var origin = _maps.LocalToTile(gridUid, grid, xform.Coordinates)
                             - new Vector2i(room.Size.X / 2, room.Size.Y / 2);

                _dungeon.SpawnRoom(
                    gridUid,
                    grid,
                    origin,
                    room,
                    _random,
                    null,
                    clearExisting: ent.Comp.ClearExisting,
                    rotation: ent.Comp.Rotation);
            }
        }

        QueueDel(ent);
    }

    private DungeonRoomPrototype? ResolveRoom(Entity<ADTRoomFillComponent> ent)
    {
        if (ent.Comp.Room is { } pinned && _prototype.TryIndex(pinned, out var room))
            return room;

        return _dungeon.GetRoomPrototype(_random, ent.Comp.RoomWhitelist, ent.Comp.MinSize, ent.Comp.MaxSize);
    }
}
