using System.Linq;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.GhostKick;
using Content.Server.Administration;
using Content.Server.Administration.Managers;

namespace Content.Server.ADT.Administration.Commands;

// Команда для тихого кика
// Автор: Discord: schrodinger71
// Данная команда предназначена для глав проекта "Время Приключений" и позволяет тихо какать разрывая соединение пользователя.
[AdminCommand(AdminFlags.Permissions)]
public sealed class KickHideCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override string Command => "kick_hide";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("cmd-kick_hide-error-arg");
            return;
        }

        var target = args[0];
        var located = await _locator.LookupIdByNameOrIdAsync(target);

        if (located == null)
        {
            shell.WriteError(Loc.GetString("cmd-kick_hide-player"));
            return;
        }

        var players = IoCManager.Resolve<IPlayerManager>();
        if (!players.TryGetSessionByUsername(target, out var player))
        {
            shell.WriteError($"Unable to find player: '{target}'.");
            return;
        }
        if (_adminManager.HasAdminFlag(player, AdminFlags.Permissions))
        {
            shell.WriteError($"You not permission for kick '{target}'.");
            return;
        }
        _ghostKickManager.DoDisconnect(player.Channel, "Smitten.");
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(
                options,
                LocalizationManager.GetString("cmd-ban-hint"));
        }

        return CompletionResult.Empty;
    }
}
