// ADT File
using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;


namespace Content.Server.Administration.Commands;

// Команда для массового бана игроков
// Автор: Discord: schrodinger71
// Данная команда предназначена для администрации проекта "Время Приключений" и позволяет эффективно банить сразу несколько игроков.
[AdminCommand(AdminFlags.MassBan)]
public sealed class BanMassCommand : LocalizedCommands
{

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    public override string Command => "banmass";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string[] targets;
        string reason;
        uint minutes;

        if (!Enum.TryParse(_cfg.GetCVar(CCVars.ServerBanDefaultSeverity), out NoteSeverity severity))
        {
            _logManager.GetSawmill("admin.server_ban")
                .Warning("Server ban severity could not be parsed from config! Defaulting to high.");
            severity = NoteSeverity.High;
        }

        if (args.Length < 2 || args.Length > 3)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        targets = args[0].Split(' ');
        reason = args[1];

        if (args.Length == 3)
        {
            if (!uint.TryParse(args[2], out minutes))
            {
                shell.WriteLine(Loc.GetString("cmd-ban-invalid-minutes", ("minutes", args[2])));
                shell.WriteLine(Help);
                return;
            }
        }
        else
        {
            minutes = 0;
        }

        var player = shell.Player;

        foreach (var target in targets)
        {
            var located = await _locator.LookupIdByNameOrIdAsync(target);

            if (located == null)
            {
                shell.WriteError(Loc.GetString("cmd-ban-player", ("target", target)));
                continue;
            }

            var targetUid = located.UserId;
            var targetHWid = located.LastHWId;

            _bans.CreateServerBan(targetUid, target, player?.UserId, null, targetHWid, minutes, severity, reason);
        }
    }
}
