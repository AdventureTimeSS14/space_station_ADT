using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Content.Server.Chat.Systems;
using Content.Server.Administration.Managers;
using Content.Shared.Players;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Corvax.Sponsors;
using Content.Server.MoMMI;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
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
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

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

        var message = args[1];
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

        var player = shell.Player;
        if (player != null)
        {
            var sessionPlayer = _playerManager.GetSessionById(player.UserId);
            var adminData = _adminManager.GetAdminData(sessionPlayer);
            var senderPermission = adminData?.HasFlag(AdminFlags.Permissions) ?? false; // Default to false if null

            // Log only if the user doesn't have the Permissions flag
            if (!senderPermission)
            {
                _adminLogger.Add(LogType.Chat, LogImpact.High, $"Server announcement: {player.Name} ({player.UserId}) used the command to make {entityUid.Value} say: \"{message}\" as {args[2]}.");
            }
        }
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
