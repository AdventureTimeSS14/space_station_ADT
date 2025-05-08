using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Moderator)]
    public sealed class GetDiscordIdCommand : IConsoleCommand
    {
        public string Command => "get_discord_id";
        public string Description => "Retrieves the Discord ID of a user.";
        public string Help => $"Usage: {Command} <user id or name>";

        [Dependency] private readonly IPlayerLocator _playerLocator = default!;

        public async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            var dbMan = IoCManager.Resolve<IServerDbManager>();

            if (args.Length != 1)
            {
                shell.WriteLine(Help);
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
                    return;
                }
                userId = dbGuid.UserId;
            }

            var discordId = await dbMan.GetDiscordIdAsync(userId);

            if (discordId != null)
            {
                shell.WriteLine($"Discord ID for user {args[0]}: {discordId}");
                // Log.Debug($"Discord ID for user {args[0]}: {discordId.Value}");
            }
            else
            {
                shell.WriteLine($"Discord ID for user {args[0]} not found.");
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(
                    CompletionHelper.SessionNames(),
                    Loc.GetString("cmd-mind-command-hint")
                );
            }

            return CompletionResult.Empty;
        }
    }
}

/*
        ╔════════════════════════════════════╗
        ║   Schrödinger's Cat Code   🐾      ║
        ║   /\_/\\                           ║
        ║  ( o.o )  Meow!                    ║
        ║   > ^ <                            ║
        ╚════════════════════════════════════╝

*/
