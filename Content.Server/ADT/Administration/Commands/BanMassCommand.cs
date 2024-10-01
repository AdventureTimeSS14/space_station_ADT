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
        string reason;
        uint minutes;

        if (!Enum.TryParse(_cfg.GetCVar(CCVars.ServerBanDefaultSeverity), out NoteSeverity severity))
        {
            _logManager.GetSawmill("admin.server_ban")
                .Warning("Server ban severity could not be parsed from config! Defaulting to high.");
            severity = NoteSeverity.High;
        }

        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        reason = args[0];
        if (!uint.TryParse(args[1], out minutes))
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-minutes", ("minutes", args[1])));
            shell.WriteLine(Help);
            return;
        }

        var player = shell.Player;

        for (int i = 2; i < args.Length; i++)
        {
            var target = args[i];
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 3)
        {
            var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(
                options,
                LocalizationManager.GetString("cmd-ban-hint"));
        }

        if (args.Length == 1)
            return CompletionResult.FromHint(LocalizationManager.GetString("cmd-ban-hint-reason"));

        if (args.Length == 2)
        {
            var durations = new CompletionOption[]
            {
                new("0", LocalizationManager.GetString("cmd-ban-hint-duration-1")),
                new("1440", LocalizationManager.GetString("cmd-ban-hint-duration-2")),
                new("4320", LocalizationManager.GetString("cmd-ban-hint-duration-3")),
                new("10080", LocalizationManager.GetString("cmd-ban-hint-duration-4")),
                new("20160", LocalizationManager.GetString("cmd-ban-hint-duration-5")),
                new("43800", LocalizationManager.GetString("cmd-ban-hint-duration-6")),
            };

            return CompletionResult.FromHintOptions(durations, LocalizationManager.GetString("cmd-ban-hint-duration"));
        }

        return CompletionResult.Empty;
    }
}
