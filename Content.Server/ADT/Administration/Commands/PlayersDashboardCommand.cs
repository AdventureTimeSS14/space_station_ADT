using Content.Server.ADT.Administration.PlayersDashboard;
using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class PlayersDashboardCommand : LocalizedCommands
{
    [Dependency] private EuiManager _euiManager = default!;

    public override string Command => "playersdashboard";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _euiManager.OpenEui(new PlayersDashboardEui(), player);
    }
}
