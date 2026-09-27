using Content.Server.ADT.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.ADT.Chat.Commands;

[AnyCommand]
public sealed class ADTD20MeCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ADTD20EmoteSystem _d20Emote = default!;

    public override string Command => "d20me";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _d20Emote.TrySendD20Emote(playerEntity, message, shell, player);
    }
}
