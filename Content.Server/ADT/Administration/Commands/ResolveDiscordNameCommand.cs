using Content.Server.ADT.Administration;
using Content.Shared.Administration;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

// TEST DEBUG COMMAND
[AdminCommand(AdminFlags.Moderator)]
public sealed class ResolveDiscordNameCommand : IConsoleCommand
{
    public string Command => "resolve_discord_name";
    public string Description => "Gets the username of a Discord account by ID.";
    public string Help => $"Usage: {Command} <discord_id>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var cfg = IoCManager.Resolve<IConfigurationManager>();
        var botToken = cfg.GetCVar(ADTCCVars.DiscordTokenBot);

        if (!ulong.TryParse(args[0], out var discordId))
        {
            shell.WriteLine("Invalid Discord ID format. Must be a number.");
            return;
        }

        string? username;
        try
        {
            username = await AuthApiHelper.GetAccountDiscord(discordId, botToken);
        }
        catch (Exception e)
        {
            shell.WriteLine($"Error during Discord lookup: {e.Message}");
            return;
        }

        if (username != null)
        {
            shell.WriteLine($"Discord username: {username}");
        }
        else
        {
            shell.WriteLine("Failed to retrieve Discord username.");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}
