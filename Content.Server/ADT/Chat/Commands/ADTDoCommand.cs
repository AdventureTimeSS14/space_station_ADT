using Content.Server.ADT.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.ADT.Chat.Commands;

[AnyCommand]
public sealed class ADTDoCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ADTDoEmoteSystem _doEmote = default!;

    public override string Command => "do";

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

        _doEmote.TrySendDoEmote(playerEntity, message, player);
    }
}
