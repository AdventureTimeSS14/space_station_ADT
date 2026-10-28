using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private const int MaxOpenStrokes = 64;
    private const double CursorRate = 30;
    private const double StrokeMessageCost = 4;
    private const long MaxDroppedSamples = 5000;

    private double _strokeRate;

    private void InitializeLive()
    {
        _net.RegisterNetMessage<MsgCanvasStroke>(OnStroke);
        _net.RegisterNetMessage<MsgCanvasCursor>(OnCursor);

        Subs.CVar(_cfg, ADTCCVars.CanvasStrokeSamplesPerSec, v => _strokeRate = Math.Max(1, v), true);
    }

    private void RefillLive(CanvasMember member)
    {
        var now = _timing.RealTime;
        var dt = (now - member.LastRefill).TotalSeconds;
        member.LastRefill = now;
        if (dt <= 0)
            return;

        member.SampleTokens = Math.Min(_strokeRate, member.SampleTokens + _strokeRate * dt);
        member.CursorTokens = Math.Min(CursorRate, member.CursorTokens + CursorRate * dt);
    }

    private void OnStroke(MsgCanvasStroke msg)
    {
        if (msg.Malformed ||
            !_players.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
            !_sessions.TryGetValue(msg.Painting, out var session) ||
            session.State == CanvasSessionState.Loading ||
            !session.Members.TryGetValue(player.UserId, out var member) ||
            !member.Present ||
            !member.CanDraw)
        {
            return;
        }

        RefillLive(member);

        var cost = StrokeMessageCost + msg.Samples.Count;
        if (member.SampleTokens < cost)
        {
            member.DroppedSamples += msg.Samples.Count + 1;
            if (member.DroppedSamples > MaxDroppedSamples)
            {
                Log.Warning($"{player.Name} ({player.UserId}) flooded painting {session.PaintingUid} with strokes, removed from editors");
                PopupMember(session, member, "adt-canvas-kicked-flood");
                Demote(session, member, CanvasCloseReason.Kicked);
            }

            return;
        }

        member.SampleTokens -= cost;

        switch (msg.Phase)
        {
            case CanvasStrokePhase.Begin:
                if (member.OpenStrokes.Count >= MaxOpenStrokes && !member.OpenStrokes.Contains(msg.StrokeId))
                    return;

                member.OpenStrokes.Add(msg.StrokeId);
                break;

            case CanvasStrokePhase.Points:
                if (!member.OpenStrokes.Contains(msg.StrokeId))
                    return;
                break;

            case CanvasStrokePhase.End:
                if (!member.OpenStrokes.Remove(msg.StrokeId))
                    return;
                break;
        }

        msg.Author = member.Index;
        foreach (var subscriber in session.Subscribers)
        {
            if (subscriber != player)
                _net.ServerSendMessage(msg, subscriber.Channel);
        }
    }

    private void OnCursor(MsgCanvasCursor msg)
    {
        if (msg.Malformed ||
            !_players.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
            !_sessions.TryGetValue(msg.Painting, out var session) ||
            session.State == CanvasSessionState.Loading ||
            !session.Members.TryGetValue(player.UserId, out var member) ||
            !member.Present ||
            player.AttachedEntity is not { } attached ||
            !CanPaint(attached))
        {
            return;
        }

        RefillLive(member);
        if (member.CursorTokens < 1)
            return;

        member.CursorTokens -= 1;

        if (!member.CanDraw)
            msg.Flags &= ~(CanvasCursorFlags.Drawing | CanvasCursorFlags.Eraser);

        foreach (var sample in msg.Samples)
        {
            if (sample.X > session.Width * CanvasConstants.CoordScale || sample.Y > session.Height * CanvasConstants.CoordScale)
                return;
        }

        msg.Author = member.Index;
        foreach (var subscriber in session.Subscribers)
        {
            if (subscriber != player)
                _net.ServerSendMessage(msg, subscriber.Channel);
        }
    }
}
