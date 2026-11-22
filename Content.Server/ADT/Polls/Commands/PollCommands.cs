using Content.Server.Administration;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.Polls.Commands;

[AdminCommand(AdminFlags.Host)]
public sealed class PollPanelCommand : IConsoleCommand
{
    [Dependency] private EuiManager _euis = default!;

    public string Command => "pollpanel";
    public string Description => "Открывает панель управления внутриигровыми опросами.";
    public string Help => "pollpanel";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("Команда доступна только игроку.");
            return;
        }

        _euis.OpenEui(new PollPanelEui(), player);
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PollCloseCommand : IConsoleCommand
{
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "poll_close";
    public string Description => "Закрывает опрос. С флагом open открывает обратно.";
    public string Help => "poll_close <id> [open]";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var id))
        {
            shell.WriteError(Help);
            return;
        }

        var closed = !(args.Length > 1 && args[1] == "open");
        var ok = await _systems.GetEntitySystem<ADTPollSystem>().SetPollClosed(id, closed, shell.Player);

        if (ok)
            shell.WriteLine(closed ? $"Опрос {id} закрыт." : $"Опрос {id} открыт.");
        else
            shell.WriteError($"Опрос {id} не найден.");
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PollDeleteCommand : IConsoleCommand
{
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "poll_delete";
    public string Description => "Удаляет опрос вместе со всеми ответами.";
    public string Help => "poll_delete <id>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var id))
        {
            shell.WriteError(Help);
            return;
        }

        if (await _systems.GetEntitySystem<ADTPollSystem>().DeletePoll(id, shell.Player))
            shell.WriteLine($"Опрос {id} удалён.");
        else
            shell.WriteError($"Опрос {id} не найден.");
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PollWipeCommand : IConsoleCommand
{
    [Dependency] private IEntitySystemManager _systems = default!;

    public string Command => "poll_wipe";
    public string Description => "Стирает все ответы опроса, сам опрос остаётся.";
    public string Help => "poll_wipe <id>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var id))
        {
            shell.WriteError(Help);
            return;
        }

        var removed = await _systems.GetEntitySystem<ADTPollSystem>().WipeResponses(id, shell.Player);
        shell.WriteLine($"Стёрто ответов: {removed}.");
    }
}
