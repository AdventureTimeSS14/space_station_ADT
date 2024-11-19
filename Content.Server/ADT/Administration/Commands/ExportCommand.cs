using System.Linq;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Serilog;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.IO;
using Content.Shared.ADT.Export;

namespace Content.Server.ADT.Administration.Commands;

/// <summary>
/// Команда для экспорта yml файлов с сервера на клиент
/// Автор: _kote / FaDeOkno
/// Команда создана для облегчения работы мапперов и возможности получить карту без необходимости лезть в файлы сервера
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class ExportCommand : LocalizedCommands
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IResourceManager _resource = default!;

    public override string Command => "export";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var opts = CompletionHelper.UserFilePath(args[0], _resource.UserData)
                    .Concat(CompletionHelper.ContentFilePath(args[0], _resource));
                return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-hint-savemap-path"));
        }
        return CompletionResult.Empty;
    }

    public async override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            Log.Error("This command can not be executed by server.");
            return;
        }

        var resPath = new ResPath(args[0]).ToRootedPath();
        TextReader? reader;
        if (resPath.Extension != "yml")
        {
            shell.WriteLine(Loc.GetString("cmd-export-only-yml"));
            return;
        }
        if (!_resource.UserData.Exists(resPath))
        {
            Log.Information($"No user map found: {resPath}");

            // fallback to content
            if (_resource.TryContentFileRead(resPath, out var contentReader))
            {
                reader = new StreamReader(contentReader);
            }
            else
            {
                Log.Error($"No map found: {resPath}");
                return;
            }
        }
        else
        {
            reader = _resource.UserData.OpenText(resPath);
        }
        var data = await reader.ReadToEndAsync();
        var msg = new ExportYmlMessage() { Data = data };
        _netMan.ServerSendMessage(msg, shell.Player.Channel);
    }
}

