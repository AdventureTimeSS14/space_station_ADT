using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Maps;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.MapRotation;

[AdminCommand(AdminFlags.Round)]
public sealed class MapRotationCommand : LocalizedCommands
{
    [Dependency] private MapRotationManager _rotation = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _players = default!;

    public override string Command => "maprotation";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            PrintState(shell);
            return;
        }

        if (!_rotation.Loaded)
        {
            shell.WriteError(Loc.GetString("cmd-maprotation-not-loaded"));
            return;
        }

        switch (args[0])
        {
            case "reset":
                _rotation.Reset();
                shell.WriteLine(Loc.GetString("cmd-maprotation-reset"));
                return;

            case "set":
                if (args.Length != 3 || !int.TryParse(args[2], out var bonus) || bonus < 0)
                {
                    shell.WriteError(Help);
                    return;
                }

                if (bonus > MapRotationManager.MaxBonusVotes)
                {
                    shell.WriteError(Loc.GetString("cmd-maprotation-bonus-too-large", ("max", MapRotationManager.MaxBonusVotes)));
                    return;
                }

                if (!_prototype.HasIndex<GameMapPrototype>(args[1]))
                {
                    shell.WriteError(Loc.GetString("cmd-maprotation-unknown-map", ("map", args[1])));
                    return;
                }

                _rotation.SetBonus(args[1], bonus);
                shell.WriteLine(Loc.GetString("cmd-maprotation-set", ("map", args[1]), ("bonus", bonus)));
                return;

            default:
                shell.WriteError(Help);
                return;
        }
    }

    private void PrintState(IConsoleShell shell)
    {
        if (!_rotation.Loaded)
        {
            shell.WriteLine(Loc.GetString("cmd-maprotation-not-loaded"));
            return;
        }

        var recent = _rotation.GetRecentMaps();
        shell.WriteLine(Loc.GetString("cmd-maprotation-recent", ("maps", recent.Count == 0 ? "-" : string.Join(", ", recent))));

        var excluded = recent.Where(_rotation.IsExcluded).ToList();
        shell.WriteLine(Loc.GetString("cmd-maprotation-excluded", ("maps", excluded.Count == 0 ? "-" : string.Join(", ", excluded))));

        if (_rotation.Bonuses.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-maprotation-no-bonus"));
            return;
        }

        var players = Math.Max(_players.PlayerCount, _rotation.MinPlayers);

        foreach (var (map, perPlayer) in _rotation.Bonuses.OrderByDescending(p => p.Value))
        {
            shell.WriteLine(Loc.GetString("cmd-maprotation-bonus",
                ("map", map),
                ("perPlayer", perPlayer.ToString("F3")),
                ("votes", _rotation.ToVotes(perPlayer, players)),
                ("players", players)));
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHintOptions(new[] { "reset", "set" }, Loc.GetString("cmd-maprotation-hint-action"));

        if (args.Length == 2 && args[0] == "set")
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<GameMapPrototype>(proto: _prototype),
                Loc.GetString("cmd-maprotation-hint-map"));
        }

        if (args.Length == 3 && args[0] == "set")
            return CompletionResult.FromHint(Loc.GetString("cmd-maprotation-hint-bonus"));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Round)]
public sealed class EndLobbyWarmupCommand : LocalizedEntityCommands
{
    [Dependency] private GameTicker _ticker = default!;

    public override string Command => "endlobbywarmup";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_ticker.EndLobbyWarmup())
        {
            shell.WriteError(Loc.GetString("cmd-endlobbywarmup-not-active"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-endlobbywarmup-done"));
    }
}
