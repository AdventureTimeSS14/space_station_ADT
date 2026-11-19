using Content.Shared.ADT.Drawing;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(10);

    private void InitializeLayers()
    {
        _net.RegisterNetMessage<MsgCanvasLayers>();
        _net.RegisterNetMessage<MsgCanvasLayerAction>(OnLayerAction);
        _net.RegisterNetMessage<MsgCanvasFlush>(OnFlush);
    }

    private void OnFlush(MsgCanvasFlush msg)
    {
        if (msg.Malformed ||
            !_players.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
            !_sessions.TryGetValue(msg.Painting, out var session) ||
            session.State == CanvasSessionState.Loading)
        {
            return;
        }

        switch (msg.Phase)
        {
            case CanvasFlushPhase.Request:
                if (player.UserId != session.Host || session.Submitting)
                    return;

                StartFlush(session);
                break;

            case CanvasFlushPhase.Done:
                if (msg.Id != session.FlushId || !session.FlushWaiting.Remove(player.UserId))
                    return;

                if (session.FlushWaiting.Count == 0)
                    FinishFlush(session);
                break;
        }
    }

    private void StartFlush(CanvasSession session)
    {
        session.FlushId = session.NextFlushId++;
        session.FlushWaiting.Clear();
        session.FlushDeadline = _timing.RealTime + FlushTimeout;

        var msg = new MsgCanvasFlush
        {
            Painting = session.PaintingUid,
            Phase = CanvasFlushPhase.Flush,
            Id = session.FlushId,
        };

        foreach (var member in session.Members.Values)
        {
            if (member.User == session.Host || !member.CanDraw || member.Player is not { } player)
                continue;

            session.FlushWaiting.Add(member.User);
            _net.ServerSendMessage(msg, player.Channel);
        }

        if (session.FlushWaiting.Count == 0)
            FinishFlush(session);
    }

    private void UpdateFlush(CanvasSession session, TimeSpan now)
    {
        session.FlushWaiting.RemoveWhere(user =>
            !session.Members.TryGetValue(user, out var member) || !member.Present || !member.CanDraw);

        if (session.FlushWaiting.Count == 0 || now >= session.FlushDeadline)
            FinishFlush(session);
    }

    private void FinishFlush(CanvasSession session)
    {
        var msg = new MsgCanvasFlush
        {
            Painting = session.PaintingUid,
            Phase = CanvasFlushPhase.Ready,
            Id = session.FlushId,
        };

        session.FlushId = 0;
        session.FlushWaiting.Clear();

        foreach (var subscriber in session.Subscribers)
        {
            _net.ServerSendMessage(msg, subscriber.Channel);
        }
    }

    private void OnLayerAction(MsgCanvasLayerAction msg)
    {
        if (msg.Malformed ||
            !_players.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
            !_sessions.TryGetValue(msg.Painting, out var session) ||
            session.State == CanvasSessionState.Loading ||
            session.Submitting ||
            !session.Members.TryGetValue(player.UserId, out var member) ||
            !member.Present)
        {
            return;
        }

        if (msg.Action == CanvasLayerActionKind.Create)
        {
            CreateLayer(session, member, player, msg);
            return;
        }

        if (session.FindLayer(msg.Layer) is not { } layer)
            return;

        var owner = layer.Owner == player.UserId && member.CanDraw;
        var host = member.Role == CanvasRole.Host;

        switch (msg.Action)
        {
            case CanvasLayerActionKind.Rename:
                if (!owner)
                    return;

                layer.Name = msg.Name.Length > 0 ? msg.Name : layer.Name;
                break;

            case CanvasLayerActionKind.Opacity:
                if (!owner)
                    return;

                layer.Opacity = Math.Clamp(msg.Opacity, 0f, 1f);
                break;

            case CanvasLayerActionKind.Visible:
                if (!owner && !host)
                    return;

                layer.Visible = msg.Visible;
                break;

            case CanvasLayerActionKind.Move:
                if (!owner || !MoveLayer(session, layer, msg.Delta))
                    return;

                break;

            case CanvasLayerActionKind.Delete:
                if (!owner && !host)
                    return;

                session.Layers.Remove(layer);
                session.DropUnreferencedBlobs();
                break;

            default:
                return;
        }

        BroadcastLayers(session);
    }

    private void CreateLayer(CanvasSession session, CanvasMember member, ICommonSession player, MsgCanvasLayerAction msg)
    {
        if (!member.CanDraw || msg.LocalId <= 0)
            return;

        if (session.FindLayer(player.UserId, msg.LocalId) != null ||
            session.LayerCount(player.UserId) >= CanvasConstants.MaxLayersPerMember ||
            session.Layers.Count >= CanvasConstants.MaxSessionLayers ||
            !session.TryAllocateLayerId(out var id))
        {
            SendLayers(session, player);
            return;
        }

        var name = msg.Name.Length > 0
            ? msg.Name
            : Loc.GetString("adt-drawing-layer-name", ("number", msg.LocalId));

        var layer = new CanvasSessionLayer(id, player.UserId, msg.LocalId, session.Tiles.Length, name,
            Math.Clamp(msg.Opacity, 0f, 1f), msg.Visible);

        session.Layers.Add(layer);
        BroadcastLayers(session);
    }

    private static bool MoveLayer(CanvasSession session, CanvasSessionLayer layer, sbyte delta)
    {
        var index = session.Layers.IndexOf(layer);
        var target = index + Math.Sign(delta);
        if (index < 0 || delta == 0 || target < 0 || target >= session.Layers.Count)
            return false;

        session.Layers.RemoveAt(index);
        session.Layers.Insert(target, layer);
        return true;
    }

    private void SendLayers(CanvasSession session, ICommonSession player)
    {
        var msg = new MsgCanvasLayers
        {
            Painting = session.PaintingUid,
            Layers = session.LayerInfos(),
        };

        _net.ServerSendMessage(msg, player.Channel);
    }

    private void BroadcastLayers(CanvasSession session)
    {
        var msg = new MsgCanvasLayers
        {
            Painting = session.PaintingUid,
            Layers = session.LayerInfos(),
        };

        foreach (var subscriber in session.Subscribers)
        {
            _net.ServerSendMessage(msg, subscriber.Channel);
        }
    }
}
