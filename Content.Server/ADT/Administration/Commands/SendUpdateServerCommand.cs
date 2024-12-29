using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Server.ServerUpdates;

namespace Content.Server.ADT.Administration.Commands;


[AdminCommand(AdminFlags.Permissions)]
public sealed class SendUpdateServerCommand : LocalizedCommands
{
    [Dependency] private readonly ServerUpdateManager _serverManager = default!;
    public override string Command => "send_updateserver_devtest";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
            return;
        }

        _serverManager.SendDiscordWebHookUpdateMessage();
    }
}
