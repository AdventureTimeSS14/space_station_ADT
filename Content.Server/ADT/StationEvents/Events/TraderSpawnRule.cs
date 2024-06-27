using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

public sealed class TraderSpawnRule : StationEventSystem<TraderSpawnRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;

    protected override void Started(EntityUid uid, TraderSpawnRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions
        {
            LoadMap = true,
        };

        _map.TryLoad(shuttleMap, component.TraderShuttlePath, out _, options);
    }
}

