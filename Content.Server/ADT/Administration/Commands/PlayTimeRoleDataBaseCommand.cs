using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddOverallAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_addoverall_as";
    public string Description => Loc.GetString("cmd-playtime_addoverall-desc");
    public string Help => Loc.GetString("cmd-playtime_addoverall-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_addoverall-error-args"));
            return;
        }

        if (!int.TryParse(args[1], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[1])));
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        if (_playerManager.TryGetSessionById(userId, out var player))
        {
            _playTimeTracking.AddTimeToOverallPlaytime(player, TimeSpan.FromMinutes(minutes));
            var overall = _playTimeTracking.GetOverallPlaytime(player);

            shell.WriteLine(Loc.GetString(
                "cmd-playtime_addoverall-succeed",
                ("username", args[0]),
                ("time", overall)));
        }
        else
        {
            await _playTimeTracking.AddTimeToOverallPlaytimeById(userId, TimeSpan.FromMinutes(minutes));
            var overall = await _playTimeTracking.GetOverallPlaytimeById(userId);

            shell.WriteLine(Loc.GetString(
                "cmd-playtime_addoverall-succeed",
                ("username", args[0]),
                ("time", overall)));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-playtime_addoverall-arg-user"));

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("cmd-playtime_addoverall-arg-minutes"));

        return CompletionResult.Empty;
    }
}


[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddRoleAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_addrole_as";
    public string Description => Loc.GetString("cmd-playtime_addrole-desc");
    public string Help => Loc.GetString("cmd-playtime_addrole-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_addrole-error-args"));
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        if (!int.TryParse(args[2], out var minutes))
        {
            shell.WriteError(Loc.GetString("parse-minutes-fail", ("minutes", args[2])));
            return;
        }

        await _playTimeTracking.AddTimeToTrackerById(userId, args[1], TimeSpan.FromMinutes(minutes));
        var overall = await _playTimeTracking.GetOverallPlaytimeById(userId);

        shell.WriteLine(Loc.GetString(
            "cmd-playtime_addrole-succeed",
            ("username", args[0]),
            ("role", args[1]),
            ("time", overall)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_addrole-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString("cmd-playtime_addrole-arg-role"));
        }

        if (args.Length == 3)
            return CompletionResult.FromHint(Loc.GetString("cmd-playtime_addrole-arg-minutes"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeGetOverallAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_getoverall_as";
    public string Description => Loc.GetString("cmd-playtime_getoverall-desc");
    public string Help => Loc.GetString("cmd-playtime_getoverall-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_getoverall-error-args"));
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        var overallPlaytime = await _playTimeTracking.GetOverallPlaytimeById(userId);

        shell.WriteLine(Loc.GetString(
            "cmd-playtime_getoverall-success",
            ("username", args[0]),
            ("time", overallPlaytime)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_getoverall-arg-user"));
        }

        return CompletionResult.Empty;
    }
}


[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeGetRoleAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_getrole_as";
    public string Description => Loc.GetString("cmd-playtime_getrole-desc");
    public string Help => Loc.GetString("cmd-playtime_getrole-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (1 or 2))
        {
            shell.WriteLine(Loc.GetString("cmd-playtime_getrole-error-args"));
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        if (args.Length == 1)
        {
            var playTimes = await _playTimeTracking.GetTrackerTimesById(userId);

            if (playTimes.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-playtime_getrole-no"));
                return;
            }

            foreach (var (role, time) in playTimes)
            {
                shell.WriteLine(Loc.GetString("cmd-playtime_getrole-role", ("role", role), ("time", time)));
            }
        }
        else
        {
            if (args[1] == "Overall")
            {
                var overallTime = await _playTimeTracking.GetOverallPlaytimeById(userId);
                shell.WriteLine(Loc.GetString("cmd-playtime_getrole-overall", ("time", overallTime)));
            }
            else
            {
                var roleTime = await _playTimeTracking.GetPlayTimeForTrackerById(userId, args[1]);
                shell.WriteLine(Loc.GetString("cmd-playtime_getrole-succeed", ("username", args[0]), ("time", roleTime)));
            }
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_getrole-arg-user"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<PlayTimeTrackerPrototype>(),
                Loc.GetString("cmd-playtime_getrole-arg-role"));
        }

        return CompletionResult.Empty;
    }
}

/// <summary>
/// Saves the timers for a particular player immediately
/// </summary>
[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeSaveAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_save_as";
    public string Description => Loc.GetString("cmd-playtime_save-desc");
    public string Help => Loc.GetString("cmd-playtime_save-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("cmd-playtime_save-error-args"));
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        await _playTimeTracking.SaveSessionById(userId);
        shell.WriteLine(Loc.GetString("cmd-playtime_save-succeed", ("username", args[0])));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_save-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class PlayTimeFlushAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_flush_as";
    public string Description => Loc.GetString("cmd-playtime_flush-desc");
    public string Help => Loc.GetString("cmd-playtime_flush-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is not (0 or 1))
        {
            shell.WriteError(Loc.GetString("cmd-playtime_flush-error-args"));
            return;
        }

        if (args.Length == 0)
        {
            _playTimeTracking.FlushAllTrackers();
            return;
        }

        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        await _playTimeTracking.FlushTrackerById(userId);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_flush-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
