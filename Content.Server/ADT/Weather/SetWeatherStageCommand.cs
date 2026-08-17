using System.Linq;
using Content.Server.Administration;
using Content.Server.Weather;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Weather;

[AdminCommand(AdminFlags.Fun)]
public sealed class SetWeatherStageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override string Command => "setweatherstage";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError("Usage: setweatherstage <stageIndex> [mapId]");
            return;
        }

        if (!int.TryParse(args[0], out var stageIndex) || stageIndex < 0)
        {
            shell.WriteError($"'{args[0]}' is not a valid non-negative stage index.");
            return;
        }

        EntityUid? mapUid = null;

        if (args.Length == 2)
        {
            if (!int.TryParse(args[1], out var rawMapId))
            {
                shell.WriteError($"'{args[1]}' is not a valid map id.");
                return;
            }

            var mapId = new MapId(rawMapId);
            if (!_mapManager.MapExists(mapId))
            {
                shell.WriteError($"Map {rawMapId} does not exist.");
                return;
            }

            var mapQuery = _entManager.AllEntityQueryEnumerator<MapComponent>();
            while (mapQuery.MoveNext(out var candidate, out var mapComp))
            {
                if (mapComp.MapId != mapId)
                    continue;

                mapUid = candidate;
                break;
            }
        }
        else if (shell.Player?.AttachedEntity is { } attached &&
                 _entManager.TryGetComponent<TransformComponent>(attached, out var xform))
        {
            mapUid = xform.MapUid;
        }

        if (mapUid is not { } map)
        {
            shell.WriteError("Could not determine target map. Specify a map id explicitly.");
            return;
        }

        if (!_entManager.TryGetComponent<WeatherSchedulerComponent>(map, out var scheduler))
        {
            shell.WriteError("Target map has no WeatherSchedulerComponent (no weather schedule on this planet).");
            return;
        }

        if (scheduler.Stages.Count == 0)
        {
            shell.WriteError("This map's weather schedule has no stages defined.");
            return;
        }

        if (stageIndex >= scheduler.Stages.Count)
        {
            shell.WriteError($"Stage index out of range. Map has {scheduler.Stages.Count} stages (0-{scheduler.Stages.Count - 1}).");
            return;
        }

        scheduler.Stage = stageIndex;
        scheduler.NextUpdate = _timing.CurTime;
        _entManager.Dirty(map, scheduler);

        shell.WriteLine($"Weather scheduler on map {map} will advance to stage {stageIndex} on the next tick.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<stage index>");

        if (args.Length == 2)
            return CompletionResult.FromHintOptions(
                _mapManager.GetAllMapIds().Select(m => m.ToString()),
                "[map id]");

        return CompletionResult.Empty;
    }
}
