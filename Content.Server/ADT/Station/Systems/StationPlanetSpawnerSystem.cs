using Content.Server.ADT.Planet;
using Content.Server.ADT.Station.Components;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;

namespace Content.Server.ADT.Station.Systems;

public sealed class StationPlanetSpawnerSystem : EntitySystem
{
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!; // ADT-tweak

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPlanetSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationPlanetSpawnerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<StationPlanetSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!_cfg.GetCVar(ADTCCVars.PlanetSpawnerEnabled))
            return;

        if (ent.Comp.GridPath is not {} path)
            return;

        ent.Comp.Map = _planet.LoadPlanet(ent.Comp.Planet, path.ToString());
    }

    private void OnShutdown(Entity<StationPlanetSpawnerComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Map);
    }
}
