using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Chat.Systems;
using Serilog;
using FastAccessors;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]

// Команда для постинга за персонажа игрока
// Автор: Discord: schrodinger71
// Просто фановая команда, что по указанию никнейма, будет постить ваши введённые строки за игрового персонажа указанного никнеймом.
public sealed class EchoChatCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override string Command => "echo_chat";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;

        var massaged = args[1];

        if (args[2] == "emote")
        {
            _chatSystem.TrySendInGameICMessage(entityUid.Value, massaged, InGameICChatType.Emote, ChatTransmitRange.Normal);
            shell.WriteLine($"зашли сюда (args[2] == emote)");
        }
        else if (args[2] == "speak")
        {
            _chatSystem.TrySendInGameICMessage(entityUid.Value, massaged, InGameICChatType.Speak, ChatTransmitRange.Normal);
            shell.WriteLine($"зашли сюда (args[2] == speak)");
        }

        shell.WriteLine($"Инфа о переменных: \nargs[0]: {args[0]} \nargs[1]: {massaged} \nargs[2]: {args[2]}");
        Log.Information($"Инфа о переменных: args[0]: {args[0]} args[1]: {massaged} args[2]: {args[2]}");
        Log.Debug($"Инфа о переменных: args[0]: {args[0]} args[1]: {massaged} args[2]: {args[2]}");

        //_adminLogger.Add(LogType.Chat, LogImpact.Low, $"Server announcement: {message}");
    }

    private bool TryParseUid(string str, IConsoleShell shell,
        IEntityManager entMan, [NotNullWhen(true)] out EntityUid? entityUid)
    {
        if (NetEntity.TryParse(str, out var entityUidNet) && _entManager.TryGetEntity(entityUidNet, out entityUid) && entMan.EntityExists(entityUid))
            return true;

        if (_playerManager.TryGetSessionByUsername(str, out var session) && session.AttachedEntity.HasValue)
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
}
