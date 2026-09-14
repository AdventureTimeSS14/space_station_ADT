using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Antag;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Server | AdminFlags.Permissions)]
public sealed class CheckAntagBonusCommand : IConsoleCommand
{
    [Dependency] private readonly AntagRollBonusManager _rollBonus = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public string Command => "checkantagbonus";
    public string Description => "Shows the stored antag roll bonus of a player.";
    public string Help => $"Usage: {Command} <username or user id>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (await AntagRollBonusCommandHelper.ResolveUser(shell, _playerLocator, args[0]) is not { } userId)
            return;

        var missed = await _rollBonus.GetMissedRounds(userId);

        if (!_rollBonus.Enabled)
            shell.WriteLine("The antag roll bonus is currently disabled, these values are not applied.");

        if (missed.Count == 0)
        {
            shell.WriteLine($"{args[0]} has no antag roll bonus.");
            return;
        }

        shell.WriteLine($"Antag roll bonus of {args[0]} ({userId}):");

        foreach (var (role, rounds) in missed.OrderByDescending(p => p.Value))
        {
            var weight = _rollBonus.GetWeight(rounds);
            shell.WriteLine($"  {role.Id}: {rounds}/{AntagRollBonusManager.MaxMissedRounds} missed round(s), weight x{weight:F2}");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "<username or user id>");
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Server | AdminFlags.Permissions)]
public sealed class ChangeAntagBonusCommand : IConsoleCommand
{
    [Dependency] private readonly AntagRollBonusManager _rollBonus = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public const string AllRolesKeyword = "all";

    public string Command => "changeantagbonus";
    public string Description => "Sets how many rounds a player counts as having missed an antag role.";
    public string Help => $"Usage: {Command} <username or user id> <antag id or {AllRolesKeyword}> <missed rounds 0-{AntagRollBonusManager.MaxMissedRounds}>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError(Help);
            return;
        }

        if (!int.TryParse(args[2], out var rounds))
        {
            shell.WriteError($"'{args[2]}' is not a whole number of rounds.");
            return;
        }

        if (rounds < 0 || rounds > AntagRollBonusManager.MaxMissedRounds)
        {
            shell.WriteError($"Missed rounds have to be between 0 and {AntagRollBonusManager.MaxMissedRounds}.");
            return;
        }

        List<ProtoId<AntagPrototype>> roles;
        if (args[1].Equals(AllRolesKeyword, StringComparison.OrdinalIgnoreCase))
        {
            roles = AntagRollBonusCommandHelper.BonusRoles(_proto).ToList();
        }
        else if (_proto.TryIndex<AntagPrototype>(args[1], out var antag))
        {
            roles = new List<ProtoId<AntagPrototype>> { antag.ID };
        }
        else
        {
            shell.WriteError($"There is no antag prototype called '{args[1]}'.");
            return;
        }

        if (await AntagRollBonusCommandHelper.ResolveUser(shell, _playerLocator, args[0]) is not { } userId)
            return;

        foreach (var role in roles)
        {
            if (await _rollBonus.SetMissedRounds(userId, role, rounds))
                continue;

            shell.WriteError($"Failed to write the roll bonus for {role.Id} to the database.");
            return;
        }

        var target = roles.Count == 1 ? roles[0].Id : $"{roles.Count} antag role(s)";
        var weight = _rollBonus.GetWeight(rounds);

        shell.WriteLine($"Set the roll bonus of {args[0]} for {target} to {rounds} missed round(s), weight x{weight:F2}.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "<username or user id>");
        }

        if (args.Length == 2)
        {
            var options = AntagRollBonusCommandHelper.BonusRoles(_proto)
                .Select(r => r.Id)
                .Append(AllRolesKeyword);

            return CompletionResult.FromHintOptions(options, $"<antag id or {AllRolesKeyword}>");
        }

        if (args.Length == 3)
            return CompletionResult.FromHint($"<missed rounds 0-{AntagRollBonusManager.MaxMissedRounds}>");

        return CompletionResult.Empty;
    }
}

internal static class AntagRollBonusCommandHelper
{
    public static async Task<NetUserId?> ResolveUser(IConsoleShell shell, IPlayerLocator locator, string arg)
    {
        if (Guid.TryParse(arg, out var guid))
            return new NetUserId(guid);

        var located = await locator.LookupIdByNameAsync(arg);
        if (located != null)
            return located.UserId;

        shell.WriteError($"Could not find a player called '{arg}'.");
        return null;
    }

    public static IEnumerable<ProtoId<AntagPrototype>> BonusRoles(IPrototypeManager proto)
    {
        return proto.EnumeratePrototypes<AntagPrototype>()
            .Where(a => a.SetPreference)
            .Select(a => new ProtoId<AntagPrototype>(a.ID));
    }
}
