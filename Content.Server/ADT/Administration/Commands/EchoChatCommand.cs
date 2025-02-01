using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Chat.Systems;
using Content.Server.Administration.Managers;
using System.Linq;
using Content.Shared.Database;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]

// Команда для постинга за персонажа игрока
// Автор: Discord: schrodinger71
// Просто фановая команда, что по указанию никнейма, будет постить ваши введённые строки за игрового персонажа указанного никнеймом.
public sealed class EchoChatCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override string Command => "echo_chat";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Проверяем количество аргументов
        if (args.Length != 3 && !(args.Length == 4 && args[3] == "false"))
        {
            shell.WriteLine(Loc.GetString("echo_chat-whisper-error-args"));
            return;
        }

        // Парсим UID
        if (!TryParseUid(args[0], shell, _entManager, out var entityUid))
            return;

        var message = args[1];

        // Обрабатываем тип сообщения
        if (args[2] == "emote")
        {
            _chatSystem.TrySendInGameICMessage(entityUid.Value, message, InGameICChatType.Emote, ChatTransmitRange.Normal);
        }
        else if (args[2] == "speak")
        {
            _chatSystem.TrySendInGameICMessage(entityUid.Value, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }
        else if (args[2] == "whisper")
        {
            _chatSystem.TrySendInGameICMessage(entityUid.Value, message, InGameICChatType.Whisper, ChatTransmitRange.Normal);
        }

        if (args.Length == 4 && args[3] == "false")
        {
            return;
        }

        // Логируем сообщение
        _adminLogger.Add(
            LogType.AdminMessage,
            LogImpact.Low,
            $"[АДМИНАБУЗ] {shell.Player?.Name} used the command echo_chat to make ({_entManager.ToPrettyString(entityUid.Value)}) to say: {message} as {args[2]}."
        );
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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _playerManager.Sessions.Select(c => c.Name).OrderBy(c => c).ToArray();
            return CompletionResult.FromHintOptions(
                options,
                LocalizationManager.GetString("echo_chat-hint"));
        }

        if (args.Length == 2)
            return CompletionResult.FromHint(Loc.GetString("echo_chat-message-help"));

        if (args.Length == 3)
        {
            var isAnnounce = new CompletionOption[]
            {
                new("speak", Loc.GetString("echo_chat-speak-help")),
                new("emote", Loc.GetString("echo_chat-emote-help")),
                new("whisper", Loc.GetString("echo_chat-whisper-help"))
            };
            return CompletionResult.FromHintOptions(isAnnounce, Loc.GetString("echo_chat-why-help"));
        }

        return CompletionResult.Empty;
    }
}
