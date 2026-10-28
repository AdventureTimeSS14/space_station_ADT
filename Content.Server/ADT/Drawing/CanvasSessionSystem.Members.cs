using System.Linq;
using Content.Shared.ADT.Drawing;
using Content.Shared.Database;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem
{
    private const int MaxRefusals = 3;

    private static readonly TimeSpan RequestLifetime = TimeSpan.FromSeconds(60);

    private readonly Dictionary<(Guid Painting, NetUserId User), CanvasRefusal> _refusals = new();

    private int _maxEditors;
    private TimeSpan _joinCooldown;

    private sealed class CanvasRefusal
    {
        public int Count;
        public TimeSpan CooldownUntil;
    }

    #region Заявки

    private void OnJoinRequest(Entity<CanvasEaselComponent> ent, ref CanvasEaselJoinRequestMessage args)
    {
        if (!TryGetMember(ent, args.Actor, out var session, out var member) ||
            member.Role != CanvasRole.Viewer ||
            session.Requests.ContainsKey(member.User))
        {
            return;
        }

        if (!CanPaint(args.Actor))
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-ghost");
            return;
        }

        if (_refusals.TryGetValue((session.PaintingUid, member.User), out var refusal))
        {
            if (refusal.Count >= MaxRefusals)
            {
                Popup(ent.Owner, args.Actor, "adt-canvas-join-blocked");
                return;
            }

            if (_timing.RealTime < refusal.CooldownUntil)
            {
                Popup(ent.Owner, args.Actor, "adt-canvas-join-cooldown");
                return;
            }
        }

        if (session.EditorCount() >= _maxEditors)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-join-full");
            return;
        }

        var request = new CanvasJoinRequest(false, _timing.RealTime + RequestLifetime)
        {
            Checking = true,
        };

        session.Requests[member.User] = request;
        JoinRequestAsync(session, member, request);
    }

    private async void JoinRequestAsync(CanvasSession session, CanvasMember member, CanvasJoinRequest request)
    {
        string? access;
        try
        {
            access = await _moderation.CheckDrawAccess(member.User.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to check painting access for {member.User}: {e}");
            access = "adt-canvas-error-generic";
        }

        if (session.Closed || !session.Requests.TryGetValue(member.User, out var current) || current != request)
            return;

        request.Checking = false;

        if (access != null)
        {
            session.Requests.Remove(member.User);
            PopupMember(session, member, access);
            return;
        }

        if (session.Members.TryGetValue(session.Host, out var host))
            PopupMember(session, host, "adt-canvas-join-requested", ("name", member.Name));

        PopupMember(session, member, "adt-canvas-join-sent");
        UpdateEasel(session);
    }

    private void OnJoinCancel(Entity<CanvasEaselComponent> ent, ref CanvasEaselJoinCancelMessage args)
    {
        if (!TryGetMember(ent, args.Actor, out var session, out var member) ||
            !session.Requests.TryGetValue(member.User, out var request) ||
            request.Invite)
        {
            return;
        }

        session.Requests.Remove(member.User);
        UpdateEasel(session);
    }

    private void OnJoinDecision(Entity<CanvasEaselComponent> ent, ref CanvasEaselJoinDecisionMessage args)
    {
        if (!TryGetHost(ent, args.Actor, out var session) ||
            !session.Requests.TryGetValue(args.User, out var request) ||
            request.Invite ||
            request.Checking ||
            !session.Members.TryGetValue(args.User, out var member))
        {
            return;
        }

        session.Requests.Remove(args.User);

        if (args.Accept)
        {
            Promote(session, member, ent.Owner, args.Actor);
            return;
        }

        Refuse(session, args.User);
        PopupMember(session, member, "adt-canvas-join-rejected");
        UpdateEasel(session);
    }

    private void OnInvite(Entity<CanvasEaselComponent> ent, ref CanvasEaselInviteMessage args)
    {
        if (!TryGetHost(ent, args.Actor, out var session) ||
            !session.Members.TryGetValue(args.User, out var member) ||
            !member.Present ||
            member.Role != CanvasRole.Viewer)
        {
            return;
        }

        if (session.EditorCount() >= _maxEditors)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-join-full");
            return;
        }

        if (session.Requests.TryGetValue(args.User, out var existing) && !existing.Invite && !existing.Checking)
        {
            session.Requests.Remove(args.User);
            Promote(session, member, ent.Owner, args.Actor);
            return;
        }

        session.Requests[args.User] = new CanvasJoinRequest(true, _timing.RealTime + RequestLifetime);
        PopupMember(session, member, "adt-canvas-invite-received", ("name", HostName(session)));
        UpdateEasel(session);
    }

    private void OnInviteResponse(Entity<CanvasEaselComponent> ent, ref CanvasEaselInviteResponseMessage args)
    {
        if (!TryGetMember(ent, args.Actor, out var session, out var member) ||
            !session.Requests.TryGetValue(member.User, out var request) ||
            !request.Invite ||
            request.Checking)
        {
            return;
        }

        if (!args.Accept)
        {
            session.Requests.Remove(member.User);
            if (session.Members.TryGetValue(session.Host, out var host))
                PopupMember(session, host, "adt-canvas-invite-declined", ("name", member.Name));

            UpdateEasel(session);
            return;
        }

        request.Checking = true;
        InviteAcceptAsync(session, member, request, ent.Owner, args.Actor);
    }

    private async void InviteAcceptAsync(CanvasSession session, CanvasMember member, CanvasJoinRequest request,
        EntityUid easel, EntityUid actor)
    {
        string? access;
        try
        {
            access = await _moderation.CheckDrawAccess(member.User.UserId);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to check painting access for {member.User}: {e}");
            access = "adt-canvas-error-generic";
        }

        if (session.Closed || !session.Requests.TryGetValue(member.User, out var current) || current != request)
            return;

        session.Requests.Remove(member.User);

        if (access != null)
        {
            PopupMember(session, member, access);
            UpdateEasel(session);
            return;
        }

        Promote(session, member, easel, actor);
    }

    private void Refuse(CanvasSession session, NetUserId user)
    {
        var key = (session.PaintingUid, user);
        if (!_refusals.TryGetValue(key, out var refusal))
        {
            refusal = new CanvasRefusal();
            _refusals[key] = refusal;
        }

        refusal.Count++;
        refusal.CooldownUntil = _timing.RealTime + _joinCooldown;
    }

    #endregion

    #region Роли

    private void Promote(CanvasSession session, CanvasMember member, EntityUid easel, EntityUid actor)
    {
        if (!member.Present || member.Role != CanvasRole.Viewer)
            return;

        if (member.Player?.AttachedEntity is not { } attached || !CanPaint(attached))
        {
            PopupMember(session, member, "adt-canvas-error-ghost");
            UpdateEasel(session);
            return;
        }

        if (session.EditorCount() >= _maxEditors)
        {
            Popup(easel, actor, "adt-canvas-join-full");
            UpdateEasel(session);
            return;
        }

        member.Role = CanvasRole.Editor;
        session.Contributions.TryAdd(member.User, 0);

        _adminLog.Add(LogType.CanvasJoin, LogImpact.Low,
            $"{member.Player!.Name} ({member.User}) joined painting {session.PaintingUid} as editor");

        PopupMember(session, member, "adt-canvas-join-accepted");
        UpdateEasel(session);
    }

    private void Demote(CanvasSession session, CanvasMember member, CanvasCloseReason reason)
    {
        if (member.Role == CanvasRole.Host)
            return;

        member.Role = CanvasRole.Viewer;
        member.OpenStrokes.Clear();
        session.Requests.Remove(member.User);

        if (member.Player is { } player)
        {
            SendClosed(session, player, reason);
            SendManifest(session, player);
        }
        else
        {
            session.Members.Remove(member.User);
        }

        UpdateEasel(session);
    }

    private void OnKick(Entity<CanvasEaselComponent> ent, ref CanvasEaselKickMessage args)
    {
        if (!TryGetHost(ent, args.Actor, out var session) ||
            !session.Members.TryGetValue(args.User, out var member) ||
            member.Role != CanvasRole.Editor)
        {
            return;
        }

        var name = member.Player?.Name ?? member.Name;
        Refuse(session, args.User);
        PopupMember(session, member, "adt-canvas-kicked");
        Demote(session, member, CanvasCloseReason.Kicked);

        if (TryComp<ActorComponent>(args.Actor, out var actor))
        {
            _adminLog.Add(LogType.CanvasKick, LogImpact.Low,
                $"{actor.PlayerSession.Name} kicked {name} ({args.User}) from painting {session.PaintingUid}");
        }
    }

    private void OnLeave(Entity<CanvasEaselComponent> ent, ref CanvasEaselLeaveMessage args)
    {
        if (!TryGetMember(ent, args.Actor, out var session, out var member) || member.Role != CanvasRole.Editor)
            return;

        Demote(session, member, CanvasCloseReason.Left);
    }

    private void OnTransferHost(Entity<CanvasEaselComponent> ent, ref CanvasEaselTransferHostMessage args)
    {
        if (!TryGetHost(ent, args.Actor, out var session) ||
            !session.Members.TryGetValue(args.User, out var member) ||
            member.Role != CanvasRole.Editor ||
            !member.Present)
        {
            return;
        }

        if (HostsOtherSession(session, member.User))
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-host-busy");
            return;
        }

        TransferHost(session, member);
    }

    private bool TryPassHost(CanvasSession session)
    {
        var candidate = session.Members.Values
            .Where(m => m.Role == CanvasRole.Editor &&
                        m.Player?.AttachedEntity is { } attached &&
                        CanPaint(attached) &&
                        !HostsOtherSession(session, m.User))
            .OrderBy(m => m.Index)
            .FirstOrDefault();

        if (candidate == null)
            return false;

        TransferHost(session, candidate);
        return true;
    }

    private bool HostsOtherSession(CanvasSession session, NetUserId user)
    {
        foreach (var other in _sessions.Values)
        {
            if (other != session && other.Host == user)
                return true;
        }

        return false;
    }

    private void TransferHost(CanvasSession session, CanvasMember member)
    {
        var previous = session.Host;
        if (session.Members.TryGetValue(previous, out var old))
        {
            old.Role = CanvasRole.Editor;
            if (!old.Present)
                old.AwayUntil = _timing.RealTime + _hostGrace;
        }

        member.Role = CanvasRole.Host;
        session.Host = member.User;
        session.Contributions.TryAdd(member.User, 0);
        session.Requests.Remove(member.User);
        session.Changes++;

        if (TryComp<CanvasItemComponent>(session.Canvas, out var canvas))
            canvas.Owner = member.User;

        if (session.State == CanvasSessionState.HostAway && member.Present)
            session.State = CanvasSessionState.Active;

        _adminLog.Add(LogType.CanvasJoin, LogImpact.Low,
            $"Painting {session.PaintingUid} host passed from {previous} to {member.Player?.Name ?? member.Name} ({member.User})");

        PopupMember(session, member, "adt-canvas-host-received");
        UpdateEasel(session);
    }

    private void UpdateMembers(CanvasSession session, TimeSpan now)
    {
        var changed = false;

        if (session.Requests.Count > 0)
        {
            foreach (var (user, request) in session.Requests.ToList())
            {
                if (request.Checking || now < request.Expires)
                    continue;

                session.Requests.Remove(user);
                changed = true;
            }
        }

        foreach (var member in session.Members.Values.ToList())
        {
            if (member.Present || member.Role != CanvasRole.Editor || now < member.AwayUntil)
                continue;

            session.Members.Remove(member.User);
            changed = true;
        }

        if (changed)
            UpdateEasel(session);
    }

    #endregion

    #region Откат

    private void OnRevert(Entity<CanvasEaselComponent> ent, ref CanvasEaselRevertMessage args)
    {
        if (!TryGetHost(ent, args.Actor, out var session) || session.Submitting)
            return;

        var reverted = 0;
        for (var i = 0; i < session.Tiles.Length; i++)
        {
            var saved = session.SavedTiles[i];
            ref var tile = ref session.Tiles[i];
            if (tile.Content == saved)
                continue;

            if (saved.Kind == CanvasTileKind.Blob && !session.Blobs.ContainsKey(saved.Hash))
            {
                Log.Warning($"Painting {session.PaintingUid} cannot revert tile {i}: blob {saved.Hash} is missing");
                continue;
            }

            tile.Version++;
            tile.Content = saved;
            reverted++;

            var update = new MsgCanvasTileUpdated
            {
                Painting = session.PaintingUid,
                Key = session.KeyOf(i),
                Version = tile.Version,
                Content = saved,
            };

            foreach (var subscriber in session.Subscribers)
            {
                _net.ServerSendMessage(update, subscriber.Channel);
            }
        }

        if (reverted == 0)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-revert-nothing");
            return;
        }

        session.Changes++;
        session.DropUnreferencedBlobs();
        Popup(ent.Owner, args.Actor, "adt-canvas-revert-done");
    }

    #endregion

    #region Хелперы

    private bool TryGetMember(Entity<CanvasEaselComponent> ent, EntityUid actor,
        out CanvasSession session, out CanvasMember member)
    {
        session = default!;
        member = default!;

        if (!TryComp<ActorComponent>(actor, out var actorComp) ||
            ent.Comp.Session is not { } uid ||
            !_sessions.TryGetValue(uid, out var found) ||
            found.State == CanvasSessionState.Loading ||
            !found.Members.TryGetValue(actorComp.PlayerSession.UserId, out var foundMember) ||
            !foundMember.Present)
        {
            return false;
        }

        session = found;
        member = foundMember;
        return true;
    }

    private bool TryGetHost(Entity<CanvasEaselComponent> ent, EntityUid actor, out CanvasSession session)
    {
        if (!TryGetMember(ent, actor, out session, out var member) || member.Role != CanvasRole.Host)
            return false;

        return true;
    }

    private string HostName(CanvasSession session)
    {
        return session.Members.TryGetValue(session.Host, out var host) ? host.Name : "?";
    }

    private void PopupMember(CanvasSession session, CanvasMember member, string loc, params (string, object)[] args)
    {
        if (member.Player?.AttachedEntity is not { } attached || !Exists(session.Easel))
            return;

        _popup.PopupEntity(Loc.GetString(loc, args), session.Easel, attached);
    }

    #endregion
}
