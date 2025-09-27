// Утилита для проверки тайлов. Ссылается на: ../Content.Server/ADT/Systems/AIDownloadSystem.cs
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Utils;

public static class AITileHelper
{
    public static bool IsTileFree(IEntityManager entMan, EntityCoordinates coords)
    {
        var mapSys = entMan.System<MapSystem>();
        var tileRef = mapSys.GetTileRef(coords);

        if (tileRef == null || tileRef.Value.Tile.IsEmpty)
            return false;

        // проверяем объекты на тайле
        foreach (var ent in entMan.GetEntitiesIntersecting(coords))
        {
            if (entMan.TryGetComponent(ent, out PhysicsComponent? phys))
            {
                if (phys.BodyType == BodyType.Static && phys.Hard)
                    return false; // есть стена или терминал
            }
        }

        return true;
    }
}
