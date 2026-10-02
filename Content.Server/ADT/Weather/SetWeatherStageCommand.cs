using System.Linq;
using Content.Server.Administration;
using Content.Server.Weather;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.ADT.Weather;

[AdminCommand(AdminFlags.Fun)]
public sealed class SetWeatherStageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override string Command => "setweatherstage";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            shell.WriteError("Usage: setweatherstage <stageIndex> [instant] [mapId]");
            return;
        }

        if (!int.TryParse(args[0], out var stageIndex) || stageIndex < 0)
        {
            shell.WriteError($"'{args[0]}' is not a valid non-negative stage index.");
            return;
        }

        EntityUid? mapUid = null;
        var instant = false;
        int? rawMapId = null;

        foreach (var arg in args.Skip(1))
        {
            if (bool.TryParse(arg, out var parsedInstant))
            {
                instant = parsedInstant;
                continue;
            }

            if (int.TryParse(arg, out var parsedMapId))
            {
                rawMapId = parsedMapId;
                continue;
            }

            shell.WriteError($"'{arg}' is neither true/false nor a valid map id.");
            return;
        }

        if (rawMapId is { } mapIdValue)
        {

            var mapId = new MapId(mapIdValue);
            if (!_mapManager.MapExists(mapId))
            {
                shell.WriteError($"Map {mapIdValue} does not exist.");
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

        _entManager.System<WeatherSchedulerSystem>().SetStage(map, scheduler, stageIndex, instant);

        if (instant)
            shell.WriteLine($"Weather on map {map} was instantly switched to stage {stageIndex}.");
        else
            shell.WriteLine($"Weather scheduler on map {map} will advance to stage {stageIndex} on the next tick.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<stage index>");

        if (args.Length == 2)
            return CompletionResult.FromHintOptions(new[] { "true", "false" }, "[instant]");

        if (args.Length == 3)
            return CompletionResult.FromHintOptions(
                _mapManager.GetAllMapIds().Select(m => m.ToString()),
                "[map id]");

        return CompletionResult.Empty;
    }
}
