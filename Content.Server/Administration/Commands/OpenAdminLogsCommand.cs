﻿using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class OpenAdminLogsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "adminlogs";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminLogsEui();
        _euiManager.OpenEui(ui, player);

        // ADT-Tweak-Start
        if (args.Length == 1)
        {
            var pm = IoCManager.Resolve<IPlayerManager>();
            if (pm.TryGetPlayerDataByUsername(args[0], out var playerData))
                ui.SetLogFilter(selectedPlayers: [playerData.UserId.UserId]);
        }
        // ADT-Tweak-End
    }
}
