using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.InconnuOS.NanoNet.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class NanoNetListCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "nanonet_list";
    public string Description => "Список всех опубликованных сайтов NanoNet.";
    public string Help => "nanonet_list";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sites = _entities.System<NanoNetSystem>().GetSites().ToList();

        if (sites.Count == 0)
        {
            shell.WriteLine("Опубликованных сайтов нет.");
            return;
        }

        foreach (var site in sites)
        {
            var persistent = site.Persistent ? " [БД]" : string.Empty;
            shell.WriteLine($"{site.Label}.nt — {site.OwnerName} ({site.Owner}){persistent}");
        }
    }
}

[AdminCommand(AdminFlags.Moderator)]
public sealed class NanoNetRemoveCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public string Command => "nanonet_remove";
    public string Description => "Снимает сайт NanoNet с публикации, в том числе из БД.";
    public string Help => "nanonet_remove <домен>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var admin = shell.Player?.Name ?? "server console";

        if (!_entities.System<NanoNetSystem>().TryForceUnpublish(args[0], admin))
        {
            shell.WriteError($"Сайт {args[0]} не найден.");
            return;
        }

        shell.WriteLine($"Сайт {args[0]} снят с публикации.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = _entities.System<NanoNetSystem>().GetSites().Select(s => $"{s.Label}.nt");
        return CompletionResult.FromHintOptions(options, "<домен>");
    }
}
