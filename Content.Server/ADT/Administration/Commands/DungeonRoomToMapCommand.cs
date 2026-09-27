using Content.Server.ADT.Procedural;
using Content.Server.Administration;
using Content.Shared.ADT.Procedural;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class DungeonRoomToMapCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IResourceManager _resource = default!;

    public override string Command => "dungeonRoomToMap";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<ADTDungeonRoomPrototype>(proto: _proto),
                Loc.GetString("cmd-adt-dungeonroomtomap-hint-room"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.UserFilePath(args[1], _resource.UserData),
                Loc.GetString("cmd-hint-savemap-path"));
        }

        return CompletionResult.Empty;
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-adt-dungeonroomtomap-usage"));
            return;
        }

        if (!_proto.TryIndex<ADTDungeonRoomPrototype>(args[0], out var room))
        {
            shell.WriteError(Loc.GetString("cmd-adt-dungeonroomtomap-no-room", ("room", args[0])));
            return;
        }

        var path = new ResPath(args.Length > 1 ? args[1] : $"/{room.ID}.yml").ToRootedPath();

        if (path.Extension != "yml")
        {
            shell.WriteError(Loc.GetString("cmd-export-only-yml"));
            return;
        }

        if (!_entMan.System<ADTDungeonRoomMapSystem>().TryBuild(room, out var data, out var error))
        {
            shell.WriteError(error);
            return;
        }

        try
        {
            _resource.UserData.CreateDir(path.Directory);

            using var writer = _resource.UserData.OpenWriteText(path);
            var stream = new YamlStream { new YamlDocument(data.ToYaml()) };
            stream.Save(new YamlMappingFix(new Emitter(writer)), false);
        }
        catch (Exception exception)
        {
            shell.WriteError(Loc.GetString(
                "cmd-adt-dungeonroomtomap-save-failed",
                ("path", path.ToString()),
                ("reason", exception.Message)));
            return;
        }

        shell.WriteLine(Loc.GetString(
            "cmd-adt-dungeonroomtomap-done",
            ("room", room.ID),
            ("path", path.ToString()),
            ("size", $"{room.Size.X}x{room.Size.Y}")));
    }
}
