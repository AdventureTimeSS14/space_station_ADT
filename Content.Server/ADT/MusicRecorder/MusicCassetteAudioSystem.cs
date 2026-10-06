using Content.Shared.ADT.MusicRecorder;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.MusicRecorder;

public sealed class MusicCassetteAudioSystem : EntitySystem
{
    private const float MaxRequestRange = 32f;

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly Dictionary<(NetUserId User, string TrackId), TimeSpan> _sentAt = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MusicCassetteRequestAudioEvent>(OnRequestAudio);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _sentAt.Clear();
    }

    private void OnRequestAudio(MusicCassetteRequestAudioEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryGetEntity(ev.Cassette, out var cassette) ||
            !TryComp<MusicCassetteAudioComponent>(cassette, out var audio) ||
            !audio.Audio.TryGetValue(ev.TrackId, out var bytes))
        {
            return;
        }

        if (!_transform.InRange(Transform(player).Coordinates, Transform(cassette.Value).Coordinates, MaxRequestRange))
            return;

        var key = (args.SenderSession.UserId, ev.TrackId);
        if (_sentAt.TryGetValue(key, out var sent) && _timing.CurTime - sent < TimeSpan.FromSeconds(2))
            return;

        _sentAt[key] = _timing.CurTime;

        var filter = Filter.SinglePlayer(args.SenderSession);
        for (var offset = 0; offset < bytes.Length; offset += MusicCassetteComponent.ChunkBytes)
        {
            var count = Math.Min(MusicCassetteComponent.ChunkBytes, bytes.Length - offset);
            var chunk = new byte[count];
            Array.Copy(bytes, offset, chunk, 0, count);
            RaiseNetworkEvent(new MusicCassetteAudioChunkEvent(ev.TrackId, bytes.Length, offset, chunk), filter);
        }
    }
}
