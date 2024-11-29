using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ListLawsSetGetCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override string Command => "lslawset_get";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError(LocalizationManager.GetString("shell-target-player-does-not-exist"));
            return;
        }
        // Парсим UID
        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;
        if (!_entManager.TryGetComponent<SiliconLawProviderComponent>(entityUid.Value, out var componentUid))
        {
            shell.WriteLine(Loc.GetString("cmd-lslawset_get-error-component"));
            return;
        }

        var listLaws = componentUid?.Lawset?.Laws;
        // Проверяем, есть ли законы
        if (listLaws != null)
        {
            // Перебираем все законы и выводим их ID в консоль
            foreach (var law in listLaws)
            {
                shell.WriteLine($"Law {law.LawString}: {Loc.GetString(law.LawString)}");
            }
        }
        else
        {
            shell.WriteLine("Нет законов для данной сущности.");
        }
    }

    private bool TryParseUid(string str, IConsoleShell shell,
    IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        if (NetEntity.TryParse(str, out var entityUidNet) && _entManager.TryGetEntity(entityUidNet, out entityUid) && entMan.EntityExists(entityUid))
            return true;

        if (_players.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
        {
            entityUid = session.AttachedEntity.Value;
            return true;
        }

        if (session == null)
            shell.WriteError(Loc.GetString("cmd-rename-not-found", ("target", str)));
        else
            shell.WriteError(Loc.GetString("cmd-rename-no-entity", ("target", str)));

        entityUid = EntityUid.Invalid;
        return false;
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), LocalizationManager.GetString("shell-argument-username-hint"));
        }

        return CompletionResult.Empty;
    }
}

