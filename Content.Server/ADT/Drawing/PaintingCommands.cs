using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.ADT.Drawing;

[AdminCommand(AdminFlags.Ban)]
public sealed class PaintingQueueCommand : IConsoleCommand
{
    [Dependency] private readonly EuiManager _euis = default!;

    public string Command => "paintingqueue";
    public string Description => "Открывает очередь модерации картин.";
    public string Help => "paintingqueue";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("Только для игроков.");
            return;
        }

        _euis.OpenEui(new PaintingQueueEui(), player);
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class PaintingBanCommand : IConsoleCommand
{
    private static readonly string[] Durations = { "0", "60", "1440", "10080", "43200" };

    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public string Command => "paintingban";
    public string Description => "Запрещает игроку рисовать и отправлять картины.";
    public string Help => "paintingban <ник|guid> <минуты, 0 - навсегда> <причина>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError("Только для игроков.");
            return;
        }

        if (args.Length < 3 || !int.TryParse(args[1], out var minutes) || minutes < 0)
        {
            shell.WriteError(Help);
            return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (located == null)
        {
            shell.WriteError($"Игрок {args[0]} не найден.");
            return;
        }

        var reason = string.Join(' ', args, 2, args.Length - 2);
        var moderation = _systems.GetEntitySystem<PaintingModerationSystem>();
        shell.WriteLine(await moderation.Ban(admin, located.UserId, minutes, reason));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(players: _players), "<ник|guid>"),
            2 => CompletionResult.FromHintOptions(Durations, "<минуты, 0 - навсегда>"),
            _ => CompletionResult.FromHint("<причина>"),
        };
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class PaintingUnbanCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public string Command => "paintingunban";
    public string Description => "Снимает запрет на рисование.";
    public string Help => "paintingunban <ник|guid>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } admin)
        {
            shell.WriteError("Только для игроков.");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var located = await _locator.LookupIdByNameOrIdAsync(args[0]);
        if (located == null)
        {
            shell.WriteError($"Игрок {args[0]} не найден.");
            return;
        }

        var moderation = _systems.GetEntitySystem<PaintingModerationSystem>();
        shell.WriteLine(await moderation.Unban(admin, located.UserId));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.SessionNames(players: _players), "<ник|guid>")
            : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PaintingWhitelistAddCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IPlayerManager _players = default!;

    public string Command => "paintingwhitelistadd";
    public string Description => "Добавляет игрока в вайтлист рисования (adt.canvas.whitelist).";
    public string Help => "paintingwhitelistadd <сикей>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var located = await _locator.LookupIdByNameAsync(args[0]);
        if (located == null)
        {
            shell.WriteError($"Игрок {args[0]} не найден.");
            return;
        }

        var moderation = _systems.GetEntitySystem<PaintingModerationSystem>();
        shell.WriteLine(await moderation.WhitelistAdd(shell.Player, located.UserId, located.Username));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1
            ? CompletionResult.FromHintOptions(CompletionHelper.SessionNames(players: _players), "<сикей>")
            : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PaintingWhitelistRemoveCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "paintingwhitelistremove";
    public string Description => "Убирает игрока из вайтлиста рисования.";
    public string Help => "paintingwhitelistremove <сикей>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        var located = await _locator.LookupIdByNameAsync(args[0]);
        if (located == null)
        {
            shell.WriteError($"Игрок {args[0]} не найден.");
            return;
        }

        var moderation = _systems.GetEntitySystem<PaintingModerationSystem>();
        shell.WriteLine(await moderation.WhitelistRemove(shell.Player, located.UserId, located.Username));
    }

    public async ValueTask<CompletionResult> GetCompletionAsync(IConsoleShell shell, string[] args, string argStr, CancellationToken cancel)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var names = await _db.GetPaintingWhitelistNamesAsync();
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return CompletionResult.FromHintOptions(names, "<сикей>");
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PaintingBlobsMigrateCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => "paintingblobs_migrate";
    public string Description => "Переносит блобы картин из базы в файлы каталога adt.canvas.blob_dir.";
    public string Help => "paintingblobs_migrate";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var store = _systems.GetEntitySystem<PaintingBlobStoreSystem>();
        if (!store.External)
        {
            shell.WriteError("Сначала задайте adt.canvas.blob_store file и adt.canvas.blob_dir.");
            return;
        }

        shell.WriteLine("Перенос блобов начат.");
        try
        {
            var moved = await store.MigrateToFilesAsync();
            shell.WriteLine($"Перенесено блобов: {moved}.");
        }
        catch (Exception e)
        {
            shell.WriteError($"Перенос прерван: {e.Message}");
        }
    }
}

[AdminCommand(AdminFlags.Host)]
public sealed class PaintingBlobsImportCommand : IConsoleCommand
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => "paintingblobs_import";
    public string Description => "Возвращает блобы картин из файлов каталога adt.canvas.blob_dir обратно в базу.";
    public string Help => "paintingblobs_import";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var store = _systems.GetEntitySystem<PaintingBlobStoreSystem>();
        shell.WriteLine("Перенос блобов в базу начат.");
        try
        {
            var moved = await store.ImportFromFilesAsync();
            if (moved < 0)
            {
                shell.WriteError("Не задан adt.canvas.blob_dir.");
                return;
            }

            shell.WriteLine($"Перенесено в базу: {moved}. Не забудьте вернуть adt.canvas.blob_store db.");
        }
        catch (Exception e)
        {
            shell.WriteError($"Перенос прерван: {e.Message}");
        }
    }
}
