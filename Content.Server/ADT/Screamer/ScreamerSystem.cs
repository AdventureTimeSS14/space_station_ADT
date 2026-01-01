using System.Numerics;
using Content.Shared.ADT.Screamer;
using Robust.Server.Player;

namespace Content.Server.ADT.Screamer;

public sealed class ScreamerSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public void DoScreamer(EntityUid uid, string protoId, string? sound, Vector2 offset, float alpha, float duration, bool fadeIn, bool fadeOut)
    {
        if (!_player.TryGetSessionByEntity(uid, out var session))
            return;

        var msg = new DoScreamerMessage(protoId, sound, offset, alpha, duration, fadeIn, fadeOut);
        RaiseNetworkEvent(msg, session.Channel);
    }
}
