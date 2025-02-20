using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;

/*
    ADT Content by 🐾 Schrödinger's Code 🐾
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/

namespace Content.Server.Administration.Systems;

/// <summary>
/// This handles the administrative test arena maps, and loading them.
/// </summary>
public sealed class AdminTestArenaVariableSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;


    public Dictionary<(NetUserId, string), EntityUid> ArenaMap { get; private set; } = new();
    public Dictionary<(NetUserId, string), EntityUid?> ArenaGrid { get; private set; } = new();

    public (EntityUid Map, EntityUid? Grid) AssertArenaLoaded(
        ICommonSession admin,
        string pathGridAdminRoom,
        string prefixNameAdminRoom)
    {
        var key = (admin.UserId, prefixNameAdminRoom);

        if (ArenaMap.TryGetValue(key, out var arenaMap) && !Deleted(arenaMap) && !Terminating(arenaMap))
        {
            if (ArenaGrid.TryGetValue(key, out var arenaGrid) && arenaGrid.HasValue && !Deleted(arenaGrid.Value) && !Terminating(arenaGrid.Value))
            {
                return (arenaMap, arenaGrid);
            }
            else
            {
                ArenaGrid[key] = null;
                return (arenaMap, null);
            }
        }

        ArenaMap[key] = _mapManager.GetMapEntityId(_mapManager.CreateMap());
        _metaDataSystem.SetEntityName(ArenaMap[key], $"{prefixNameAdminRoom}M-{admin.Name}");

        if (_map.TryLoadGrid(Comp<MapComponent>(ArenaMap[key]).MapId, new ResPath(pathGridAdminRoom), out var grids))
        {
            var firstGrid = grids.GetValueOrDefault();
            _metaDataSystem.SetEntityName(firstGrid, $"{prefixNameAdminRoom}G-{admin.Name}");
            ArenaGrid[key] = firstGrid;
        }
        else
        {
            ArenaGrid[key] = null;
        }

        return (ArenaMap[key], ArenaGrid[key]);
    }
}
