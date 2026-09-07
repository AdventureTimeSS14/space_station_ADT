using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.ADT.Sponsors;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.Sponsors.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class SponsorPanelCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _euis = default!;

    public string Command => "sponsorpanel";
    public string Description => "Открывает панель управления спонсорскими тирами и выдачами.";
    public string Help => "sponsorpanel [ник|guid]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
            return;

        var eui = new SponsorPanelEui();
        _euis.OpenEui(eui, player);

        if (args.Length > 0)
            eui.HandleMessage(new SponsorPanelEuiMsg.LookupPlayer { Query = args[0] });
    }
}
