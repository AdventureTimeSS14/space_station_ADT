using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Server.Maps;
using Robust.Shared.Random;
using Content.Shared.Ghost;
using Content.Server.ADT.Ghostbar.Components;
using Content.Server.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Antag.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Random.Helpers;
using Content.Shared.Weather;
using Content.Shared.Stealth.Components;
using Content.Server.Stealth;

namespace Content.Server.ADT.Ghostbar;

public sealed class GhostBarSystemManager : EntitySystem
{

    [Dependency] private readonly SharedWeatherSystem _weathersystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private static IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationSpawningSystem _spawningSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private static IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }
    public GhostBarMapPrototype? _GhostBarMap = null;

    public GhostBarMapPrototype? GetCurrentGhostMapProto()
    {
        List<GhostBarMapPrototype> maplist = new List<GhostBarMapPrototype>();
        if (maplist == null)
            return null;

        foreach (var proto in _prototypeManager.EnumeratePrototypes<GhostBarMapPrototype>())
        {
            if (proto == null)
                return null;
            if (_GhostBarMap == proto)
                continue;
            maplist.Add(proto);
        }
        var mapprotostr = _random.Pick(maplist);
        return mapprotostr;
    }



    private void OnRoundStart(RoundStartingEvent ev)
    {
        _GhostBarMap = GetCurrentGhostMapProto();
    }

}

