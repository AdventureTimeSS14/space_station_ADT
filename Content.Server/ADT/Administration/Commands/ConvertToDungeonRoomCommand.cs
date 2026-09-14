using System.Linq;
using Content.Server.ADT.Procedural;
using Content.Server.Administration;
using Content.Shared.ADT.Export;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Administration.Commands;

/// <summary>
/// Превращает грид в комнату данжа формата adtDungeonRoom
/// <summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class ConvertToDungeonRoomCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IResourceManager _resource = default!;

    public override string Command => "convertToDungeonRoom";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = CompletionHelper.UserFilePath(args[0], _resource.UserData)
            .Concat(CompletionHelper.ContentFilePath(args[0], _resource));

        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-hint-savemap-path"));
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteError(Loc.GetString("cmd-adt-converttodungeonroom-server"));
            return;
        }

        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-adt-converttodungeonroom-usage"));
            return;
        }

        var path = new ResPath(args[0]).ToRootedPath();

        if (path.Extension != "yml")
        {
            shell.WriteError(Loc.GetString("cmd-export-only-yml"));
            return;
        }

        var roomId = args.Length > 1 ? args[1] : path.FilenameWithoutExtension;
        var tags = args.Skip(2).ToList();

        var maps = _entMan.System<SharedMapSystem>();
        var loader = _entMan.System<MapLoaderSystem>();
        var export = _entMan.System<ADTDungeonRoomExportSystem>();

        var mapUid = maps.CreateMap(out var mapId, runMapInit: false);

        try
        {
            if (!loader.TryLoadGrid(mapId, path, out var grid))
            {
                shell.WriteError(Loc.GetString("cmd-adt-converttodungeonroom-no-grid", ("path", path.ToString())));
                return;
            }

            if (!export.TrySerialize(grid.Value, roomId, tags, out var yaml, out var error))
            {
                shell.WriteError(error);
                return;
            }

            _netMan.ServerSendMessage(new ExportYmlMessage { Data = yaml }, shell.Player.Channel);
            shell.WriteLine(Loc.GetString("cmd-adt-converttodungeonroom-done", ("room", roomId)));
        }
        finally
        {
            _entMan.DeleteEntity(mapUid);
        }
    }
}
