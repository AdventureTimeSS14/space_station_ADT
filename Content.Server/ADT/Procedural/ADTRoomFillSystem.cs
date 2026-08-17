using Content.Shared.ADT.Procedural;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Procedural;

public sealed class ADTRoomFillSystem : EntitySystem
{
    [Dependency] private readonly ADTDungeonRoomSystem _rooms = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRoomFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTRoomFillComponent> marker, ref MapInitEvent args)
    {
        var xform = Transform(marker);

        if (xform.GridUid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var room = ResolveRoom(marker);

            if (room == null)
            {
                Log.Error($"Не нашлось подходящей комнаты для {ToPrettyString(marker)}");
            }
            else
            {
                var origin = _maps.LocalToTile(gridUid, grid, xform.Coordinates)
                             - new Vector2i(room.Size.X / 2, room.Size.Y / 2);

                _rooms.SpawnRoom(
                    gridUid,
                    grid,
                    origin,
                    room,
                    _random,
                    clearExisting: marker.Comp.ClearExisting,
                    rotation: marker.Comp.Rotation);
            }
        }

        QueueDel(marker);
    }

    private ADTDungeonRoomPrototype? ResolveRoom(Entity<ADTRoomFillComponent> marker)
    {
        if (marker.Comp.Room is { } pinned && _prototype.TryIndex(pinned, out var room))
            return room;

        return _rooms.GetRoom(_random, marker.Comp.RoomWhitelist, marker.Comp.MinSize, marker.Comp.MaxSize);
    }
}
