using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Chat;
using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Chat.Systems;

public sealed class ADTD20EmoteSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier RollSound = new SoundCollectionSpecifier(SharedADTD20Emote.RollSoundCollection);

    private readonly Dictionary<NetUserId, TimeSpan> _nextRoll = new();

    public override void Initialize()
    {
        base.Initialize();

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
            _nextRoll.Remove(args.Session.UserId);
    }

    public void TrySendD20Emote(EntityUid source, string message, IConsoleShell shell, ICommonSession player)
    {
        if (!_actionBlocker.CanEmote(source))
            return;

        message = SharedADTD20Emote.ReplaceMarker(message, string.Empty).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        if (_nextRoll.TryGetValue(player.UserId, out var next) && _timing.CurTime < next)
        {
            var left = (int) Math.Ceiling((next - _timing.CurTime).TotalSeconds);
            shell.WriteError(Loc.GetString("adt-d20-emote-cooldown", ("seconds", Math.Max(left, 1))));
            return;
        }

        _nextRoll[player.UserId] = _timing.CurTime + SharedADTD20Emote.Cooldown;

        var roll = _random.Next(1, SharedADTD20Emote.Faces + 1);
        var text = $"{message} {SharedADTD20Emote.MakeMarker(roll)}";

        _chat.TrySendInGameICMessage(source, text, InGameICChatType.Emote, ChatTransmitRange.Normal, false, shell, player);
        _audio.PlayPvs(RollSound, source);
    }
}
