using Content.Shared.ADT.Supermatter.Components;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Objectives.Systems;

public sealed class CheckSupermatterSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;

    /// <summary>
    /// Вынюхиваем кристалл Суперматерии на станции.
    /// </summary>
    public bool SupermatterCheck()
    {
        var query = AllEntityQuery<SupermatterComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var xform))
        {
            var station = _stationSystem.GetOwningStation(uid);

            if (station == EntityUid.Invalid)
                continue;

            return true;
        }

        return false;
    }
}