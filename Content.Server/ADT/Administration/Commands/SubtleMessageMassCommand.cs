using System.Linq;
using Content.Server.Prayer;
using Content.Shared.Administration;
using Content.Shared.Players;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SubtleMessageMassCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    public override string Command => "massmsg";
    public override string Description => Loc.GetString("massmsg-command-description");
    public override string Help => Loc.GetString("massmsg-command-help-text", ("command", Command));

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteLine(Loc.GetString("cmd-ban-invalid-arguments"));
            shell.WriteLine(Help);
            return;
        }

        var player = shell.Player;
        if (player == null)
        {
            shell.WriteLine("You cannot use this command from the server console.");
            return;
        }

        // Variables
        var message = args[0];
        var popupMessage = args[1];
        var senderEntity = player.AttachedEntity;
        string[]? allTargets;

        // Второй аргумент: radius:<число>
        if (args[2].StartsWith("radius:", StringComparison.OrdinalIgnoreCase))
        {
            if (senderEntity == null)
            {
                shell.WriteError("You must be attached to an entity to use radius mode.");
                return;
            }

            if (!float.TryParse(args[2].Split(':')[1], out var radius))
            {
                shell.WriteError("Invalid radius value.");
                return;
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(senderEntity.Value, out var xform))
                return;

            var entitiesInRange = _entityLookup.GetEntitiesInRange(xform.Coordinates, radius)
                                               .Where(e => _entityManager.HasComponent<ActorComponent>(e))
                                               .ToList();

            foreach (var ent in entitiesInRange)
            {
                if (!_entityManager.TryGetComponent(ent, out ActorComponent? actor))
                    continue;
                _prayerSystem.SendSubtleMessage(actor.PlayerSession, player, message, popupMessage);
                shell.WriteLine($"{_entityManager.ToPrettyString(ent)}");
            }

            shell.WriteLine($"Sent message to {entitiesInRange.Count} players in radius {radius}.");
            return;
        }

        // Второй аргумент: По никам или всех
        if (args[2] == "all")
        {
            allTargets = CompletionHelper.SessionNames()
                                         .Select(option => option.Value)
                                         .ToArray();
        }
        else
        {
            allTargets = args.Skip(2).Where(arg => !string.IsNullOrWhiteSpace(arg)).ToArray();
        }

        foreach (var target in allTargets)
        {
            var trimmedTarget = target.Trim();
            if (string.IsNullOrWhiteSpace(trimmedTarget))
                continue;

            var located = await _locator.LookupIdByNameOrIdAsync(trimmedTarget);

            if (located == null)
            {
                shell.WriteError(Loc.GetString("massmsg-player-unable"));
                continue;
            }

            var targetSession = _playerManager.GetSessionById(located.UserId);
            _prayerSystem.SendSubtleMessage(targetSession, player, message, popupMessage);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length >= 3)
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(),
                Loc.GetString("massmsg-command-hint")
            );

        if (args.Length == 2)
        {
            var option = new CompletionOption[]
            {
                new(Loc.GetString("prayer-popup-subtle-default"), Loc.GetString("default")),
            };

            return CompletionResult.FromHintOptions(
                option,
                Loc.GetString("massmsg-command-hint-second-args")
            );
        }

        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("massmsg-command-hint-one-args"));

        return CompletionResult.Empty;
    }
}
