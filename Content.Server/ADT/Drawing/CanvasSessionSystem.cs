using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.ADT.CCVar;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Drawing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drawing;

public sealed partial class CanvasSessionSystem : EntitySystem
{
    private const uint BlankRgba = 0xFFFFFFFF;

    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly PaintingModerationSystem _moderation = default!;
    [Dependency] private readonly PaintingBlobStoreSystem _blobStore = default!;

    private readonly Dictionary<Guid, CanvasSession> _sessions = new();
    private readonly Dictionary<Guid, Task> _pendingSaves = new();

    private bool _enabled;
    private long _maxPaintingBytes;
    private int _maxSessions;
    private int _maxSessionsPerPlayer;
    private TimeSpan _hostGrace;
    private TimeSpan _autosaveInterval;
    private int _draftSlots;
    private int _draftTtlDays;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanvasEaselComponent, EntInsertedIntoContainerMessage>(OnCanvasInserted);
        SubscribeLocalEvent<CanvasEaselComponent, EntRemovedFromContainerMessage>(OnCanvasRemoved);
        SubscribeLocalEvent<CanvasEaselComponent, ComponentShutdown>(OnEaselShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.BuiEvents<CanvasEaselComponent>(CanvasEaselUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<BoundUIClosedEvent>(OnUiClosed);
            subs.Event<CanvasEaselStartMessage>(OnStart);
            subs.Event<CanvasEaselCloseSessionMessage>(OnCloseSession);
            subs.Event<CanvasEaselSubmitMessage>(OnSubmit);
            subs.Event<CanvasEaselWithdrawMessage>(OnWithdraw);
            subs.Event<CanvasEaselJoinRequestMessage>(OnJoinRequest);
            subs.Event<CanvasEaselJoinCancelMessage>(OnJoinCancel);
            subs.Event<CanvasEaselJoinDecisionMessage>(OnJoinDecision);
            subs.Event<CanvasEaselInviteMessage>(OnInvite);
            subs.Event<CanvasEaselInviteResponseMessage>(OnInviteResponse);
            subs.Event<CanvasEaselKickMessage>(OnKick);
            subs.Event<CanvasEaselLeaveMessage>(OnLeave);
            subs.Event<CanvasEaselTransferHostMessage>(OnTransferHost);
            subs.Event<CanvasEaselRevertMessage>(OnRevert);
        });

        Subs.CVar(_cfg, ADTCCVars.CanvasEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasMaxPaintingMb, v => _maxPaintingBytes = v * 1024L * 1024L, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasMaxSessions, v => _maxSessions = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasMaxSessionsPerPlayer, v => _maxSessionsPerPlayer = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasHostGrace, v => _hostGrace = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasAutosaveInterval, v => _autosaveInterval = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasDraftSlots, v => _draftSlots = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasDraftTtlDays, v => _draftTtlDays = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasPendingPerPlayer, v => _pendingPerPlayer = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasSubmitCooldown, v => _submitCooldown = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasApprovedPerPlayer, v => _approvedPerPlayer = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasMaxEditors, v => _maxEditors = Math.Max(1, v), true);
        Subs.CVar(_cfg, ADTCCVars.CanvasJoinRequestCooldown, v => _joinCooldown = TimeSpan.FromSeconds(v), true);

        _players.PlayerStatusChanged += OnPlayerStatusChanged;
        InitializeNet();
        InitializeLive();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        foreach (var session in _sessions.Values.ToList())
        {
            CloseSession(session, CanvasCloseReason.Closed);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.RealTime;
        UpdatePeers(now);

        if (_sessions.Count == 0)
            return;

        foreach (var session in _sessions.Values.ToList())
        {
            if (session.State == CanvasSessionState.Loading)
                continue;

            UpdateMembers(session, now);

            if (session.State == CanvasSessionState.HostAway && now >= session.AwayUntil && !TryPassHost(session))
            {
                CloseSession(session, CanvasCloseReason.HostAway);
                continue;
            }

            if (session.Dirty && !session.Saving && now >= session.NextAutosave)
                Autosave(session);
        }
    }

    #region Мольберт

    private void OnCanvasInserted(Entity<CanvasEaselComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnCanvasRemoved(Entity<CanvasEaselComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SlotId)
            return;

        if (ent.Comp.Session is { } uid &&
            _sessions.TryGetValue(uid, out var session) &&
            session.Canvas == args.Entity)
        {
            CloseSession(session, CanvasCloseReason.Removed);
        }

        UpdateVisuals(ent);
        UpdateUi(ent);
    }

    private void OnEaselShutdown(Entity<CanvasEaselComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Session is { } uid && _sessions.TryGetValue(uid, out var session))
            CloseSession(session, CanvasCloseReason.Removed);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        foreach (var session in _sessions.Values.ToList())
        {
            CloseSession(session, CanvasCloseReason.Closed);
        }

        foreach (var peer in _peers.Values)
        {
            peer.ClearOutgoing();
        }

        _refusals.Clear();
    }

    private bool TryGetCanvas(Entity<CanvasEaselComponent> ent, out EntityUid canvasUid, out CanvasItemComponent canvas)
    {
        canvasUid = default;
        canvas = default!;

        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SlotId) is not { } item ||
            !TryComp<CanvasItemComponent>(item, out var comp))
        {
            return false;
        }

        canvasUid = item;
        canvas = comp;
        return true;
    }

    private void UpdateVisuals(Entity<CanvasEaselComponent> ent)
    {
        var state = string.Empty;
        var offset = Vector2.Zero;
        var painting = string.Empty;
        var paintingSize = Vector2.Zero;
        var paintingOffset = Vector2.Zero;

        if (TryGetCanvas(ent, out var canvasUid, out var canvas))
        {
            state = canvas.PaintingUid != null ? canvas.EaselState + "-wip" : canvas.EaselState;
            offset = canvas.EaselOffset;

            if (TryComp<PaintingComponent>(canvasUid, out var paintingComp) &&
                paintingComp.PaintingUid is { } uid &&
                !paintingComp.Hidden &&
                TryComp<PaintingItemVisualsComponent>(canvasUid, out var visuals))
            {
                painting = uid.ToString();
                paintingSize = visuals.InnerSize;
                paintingOffset = visuals.InnerOffset + canvas.EaselOffset;
            }
        }

        _appearance.SetData(ent.Owner, CanvasEaselVisuals.Canvas, state);
        _appearance.SetData(ent.Owner, CanvasEaselVisuals.Offset, offset);
        _appearance.SetData(ent.Owner, CanvasEaselVisuals.Painting, painting);
        _appearance.SetData(ent.Owner, CanvasEaselVisuals.PaintingSize, paintingSize);
        _appearance.SetData(ent.Owner, CanvasEaselVisuals.PaintingOffset, paintingOffset);
    }

    public void RefreshEasels()
    {
        var query = EntityQueryEnumerator<CanvasEaselComponent>();
        while (query.MoveNext(out var uid, out var easel))
        {
            UpdateVisuals((uid, easel));
        }
    }

    private void UpdateUi(Entity<CanvasEaselComponent> ent)
    {
        var hasCanvas = TryGetCanvas(ent, out _, out var canvas);

        CanvasSession? session = null;
        if (ent.Comp.Session is { } uid)
            _sessions.TryGetValue(uid, out session);

        string? hostName = null;
        var participants = new List<CanvasParticipantInfo>();
        var requests = new List<CanvasJoinInfo>();
        if (session != null)
        {
            if (session.Members.TryGetValue(session.Host, out var hostMember))
                hostName = hostMember.Name;
            else if (_players.TryGetSessionById(session.Host, out var host))
                hostName = DisplayName(host);

            foreach (var member in session.Members.Values.OrderByDescending(m => m.Role).ThenBy(m => m.Index))
            {
                participants.Add(new CanvasParticipantInfo(member.User, member.Index, member.Name, member.Role, member.Present));
            }

            foreach (var (user, request) in session.Requests)
            {
                var name = session.Members.TryGetValue(user, out var member) ? member.Name : "?";
                requests.Add(new CanvasJoinInfo(user, name, request.Invite));
            }
        }

        var state = new CanvasEaselBoundUserInterfaceState(
            hasCanvas,
            hasCanvas ? canvas.Size : Vector2i.Zero,
            hasCanvas && canvas.PaintingUid != null,
            hasCanvas ? canvas.Owner : null,
            session != null,
            session?.Host,
            hostName,
            session?.PaintingUid,
            _maxEditors,
            participants,
            requests);

        _ui.SetUiState(ent.Owner, CanvasEaselUiKey.Key, state);
    }

    private bool CanPaint(EntityUid uid)
    {
        return _actionBlocker.CanComplexInteract(uid);
    }

    private string DisplayName(ICommonSession player)
    {
        return player.AttachedEntity is { } attached
            ? Identity.Name(attached, EntityManager)
            : player.Name;
    }

    private void UpdateEasel(CanvasSession session)
    {
        if (TryComp<CanvasEaselComponent>(session.Easel, out var easel))
        {
            UpdateVisuals((session.Easel, easel));
            UpdateUi((session.Easel, easel));
        }
    }

    private void Popup(EntityUid easel, EntityUid actor, string loc)
    {
        _popup.PopupEntity(Loc.GetString(loc), easel, actor);
    }

    #endregion

    #region BUI

    private void OnUiOpened(Entity<CanvasEaselComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);

        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        var player = actor.PlayerSession;
        SendDraftsAsync(ent.Owner, args.Actor, player.UserId);

        if (ent.Comp.Session is { } uid &&
            _sessions.TryGetValue(uid, out var session) &&
            session.State != CanvasSessionState.Loading)
        {
            Resume(session, player);
        }
    }

    private void OnUiClosed(Entity<CanvasEaselComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor) ||
            ent.Comp.Session is not { } uid ||
            !_sessions.TryGetValue(uid, out var session))
        {
            return;
        }

        Unsubscribe(session, actor.PlayerSession);
    }

    private void OnCloseSession(Entity<CanvasEaselComponent> ent, ref CanvasEaselCloseSessionMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor) ||
            ent.Comp.Session is not { } uid ||
            !_sessions.TryGetValue(uid, out var session) ||
            session.Host != actor.PlayerSession.UserId ||
            session.State == CanvasSessionState.Loading)
        {
            return;
        }

        CloseSession(session, CanvasCloseReason.Closed);
    }

    private void OnStart(Entity<CanvasEaselComponent> ent, ref CanvasEaselStartMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        var player = actor.PlayerSession;
        var user = player.UserId;

        if (!_enabled)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-disabled");
            return;
        }

        if (!CanPaint(args.Actor))
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-ghost");
            return;
        }

        if (ent.Comp.Session is { } existing && _sessions.TryGetValue(existing, out var running))
        {
            if (running.State != CanvasSessionState.Loading)
                Resume(running, player);
            else
                Popup(ent.Owner, args.Actor, "adt-canvas-error-busy");

            return;
        }

        if (!TryGetCanvas(ent, out var canvasUid, out var canvas))
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-no-canvas");
            return;
        }

        if (!CanvasConstants.IsValidSize(canvas.Size))
        {
            Log.Error($"Canvas {ToPrettyString(canvasUid)} has invalid size {canvas.Size}");
            Popup(ent.Owner, args.Actor, "adt-canvas-error-generic");
            return;
        }

        if (_sessions.Count >= _maxSessions)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-server-limit");
            return;
        }

        if (_sessions.Values.Count(s => s.Host == user) >= _maxSessionsPerPlayer)
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-player-limit");
            return;
        }

        Guid paintingUid;
        var create = false;
        Guid? parent = null;
        if (canvas.PaintingUid is { } painted)
        {
            if (canvas.Owner != user)
            {
                Popup(ent.Owner, args.Actor, "adt-canvas-error-not-owner");
                return;
            }

            paintingUid = painted;
        }
        else if (args.Draft is { } draft)
        {
            paintingUid = draft;
        }
        else if (args.Parent is { } parentUid)
        {
            paintingUid = Guid.NewGuid();
            parent = parentUid;
        }
        else
        {
            paintingUid = Guid.NewGuid();
            create = true;
        }

        if (_sessions.ContainsKey(paintingUid))
        {
            Popup(ent.Owner, args.Actor, "adt-canvas-error-busy");
            return;
        }

        var session = new CanvasSession(paintingUid, canvas.Size.X, canvas.Size.Y)
        {
            Easel = ent.Owner,
            Canvas = canvasUid,
            Host = user,
        };

        _sessions[paintingUid] = session;
        ent.Comp.Session = paintingUid;
        _itemSlots.SetLock(ent.Owner, ent.Comp.SlotId, true);
        UpdateUi(ent);

        LoadSessionAsync(session, player, create, parent);
    }

    private async void SendDraftsAsync(EntityUid easel, EntityUid actor, NetUserId user)
    {
        try
        {
            var notBefore = DateTime.UtcNow - TimeSpan.FromDays(_draftTtlDays);
            var drafts = await _db.GetPaintingDraftsAsync(user, notBefore);
            var pending = await _db.GetOwnerPaintingsAsync(user, AdtPaintingStatus.Submitted);
            var approved = await _db.GetOwnerPaintingsAsync(user, AdtPaintingStatus.Approved);

            if (!Exists(easel) || !_ui.IsUiOpen(easel, CanvasEaselUiKey.Key, actor))
                return;

            var message = new CanvasEaselDraftsMessage(ToInfos(drafts), ToInfos(pending), ToInfos(approved), _draftSlots);
            _ui.ServerSendUiMessage(easel, CanvasEaselUiKey.Key, message, actor);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load canvas drafts for {user}: {e}");
        }
    }

    private static List<CanvasDraftInfo> ToInfos(List<PaintingDraftRecord> records)
    {
        var infos = new List<CanvasDraftInfo>(records.Count);
        foreach (var record in records)
        {
            infos.Add(new CanvasDraftInfo(record.PaintingUid, record.Title, record.Width, record.Height, record.UpdatedAt));
        }

        return infos;
    }

    #endregion

    #region Сессия

    private async void LoadSessionAsync(CanvasSession session, ICommonSession player, bool create, Guid? parent)
    {
        var user = player.UserId;

        try
        {
            if (_pendingSaves.TryGetValue(session.PaintingUid, out var pending))
            {
                try
                {
                    await pending;
                }
                catch (Exception)
                {
                }
            }

            var access = await _moderation.CheckDrawAccess(user);
            if (access != null)
            {
                FailLoad(session, player, access);
                return;
            }

            var now = DateTime.UtcNow;

            if (create)
            {
                var notBefore = now - TimeSpan.FromDays(_draftTtlDays);
                await _db.ExpirePaintingDraftsAsync(user, Math.Max(0, _draftSlots - 1), notBefore, null);

                int? roundId = _ticker.RoundId > 0 ? _ticker.RoundId : null;
                await _db.CreatePaintingAsync(session.PaintingUid, user, session.Width, session.Height, roundId, now);

                for (var i = 0; i < session.Tiles.Length; i++)
                {
                    session.Tiles[i].Content = CanvasTileContent.Solid(BlankRgba);
                    session.SavedTiles[i] = session.Tiles[i].Content;
                }

                session.Changes++;
            }
            else
            {
                if (parent is { } parentUid)
                {
                    var notBefore = now - TimeSpan.FromDays(_draftTtlDays);
                    await _db.ExpirePaintingDraftsAsync(user, Math.Max(0, _draftSlots - 1), notBefore, null);

                    int? roundId = _ticker.RoundId > 0 ? _ticker.RoundId : null;
                    if (!await _db.CreatePaintingFromParentAsync(parentUid, session.PaintingUid, user,
                            session.Width, session.Height, roundId, now))
                    {
                        FailLoad(session, player, "adt-canvas-error-parent");
                        return;
                    }
                }

                var snapshot = await _db.LoadPaintingAsync(session.PaintingUid);
                if (snapshot != null)
                    await _blobStore.ResolveAsync(snapshot.ExternalBlobs, snapshot.Blobs);
                var error = CheckSnapshot(snapshot, session, user);
                if (error != null)
                {
                    FailLoad(session, player, error);
                    return;
                }

                ApplySnapshot(session, snapshot!);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to open painting {session.PaintingUid}: {e}");
            FailLoad(session, player, "adt-canvas-error-generic");
            return;
        }

        if (session.Closed)
            return;

        if (TryComp<CanvasItemComponent>(session.Canvas, out var canvas))
        {
            canvas.PaintingUid = session.PaintingUid;
            canvas.Owner = user;
            _appearance.SetData(session.Canvas, CanvasItemVisuals.Painted, true);
        }

        session.State = CanvasSessionState.Active;
        session.NextAutosave = _timing.RealTime + _autosaveInterval;
        _adminLog.Add(LogType.CanvasSessionStart, LogImpact.Low,
            $"{player.Name} ({user}) opened painting {session.PaintingUid} on {ToPrettyString(session.Easel)}");

        if (player.Status == SessionStatus.InGame &&
            player.AttachedEntity is { } attached &&
            _ui.IsUiOpen(session.Easel, CanvasEaselUiKey.Key, attached))
        {
            Subscribe(session, player);
        }
        else
        {
            session.State = CanvasSessionState.HostAway;
            session.AwayUntil = _timing.RealTime + _hostGrace;
        }

        foreach (var viewer in _ui.GetActors(session.Easel, CanvasEaselUiKey.Key).ToList())
        {
            if (TryComp<ActorComponent>(viewer, out var viewerActor) && viewerActor.PlayerSession != player)
                Subscribe(session, viewerActor.PlayerSession);
        }

        UpdateEasel(session);
    }

    private string? CheckSnapshot(PaintingSnapshot? snapshot, CanvasSession session, NetUserId user)
    {
        if (snapshot == null)
            return "adt-canvas-error-draft-missing";

        switch (snapshot.Status)
        {
            case AdtPaintingStatus.Draft:
                break;

            case AdtPaintingStatus.Submitted:
                return "adt-canvas-error-submitted";

            case AdtPaintingStatus.Approved:
            case AdtPaintingStatus.Hidden:
                return "adt-canvas-error-approved";

            default:
                return "adt-canvas-error-draft-missing";
        }

        if (snapshot.OwnerUserId != user.UserId)
            return "adt-canvas-error-not-owner";

        if (snapshot.Width != session.Width || snapshot.Height != session.Height)
            return "adt-canvas-error-draft-size";

        return null;
    }

    private void ApplySnapshot(CanvasSession session, PaintingSnapshot snapshot)
    {
        foreach (var tile in snapshot.Tiles)
        {
            if (!session.TryGetIndex(tile.Key, out var index))
                continue;

            var content = tile.Content;
            if (content.Kind == CanvasTileKind.Blob && !snapshot.Blobs.ContainsKey(content.Hash))
            {
                Log.Warning($"Painting {session.PaintingUid} misses blob {content.Hash}, tile {tile.Key} reset");
                content = CanvasTileContent.Empty;
            }

            session.Tiles[index].Content = content;
            session.SavedTiles[index] = content;
        }

        foreach (var (hash, data) in snapshot.Blobs)
        {
            session.Blobs[hash] = data;
            session.PersistedBlobs.Add(hash);
        }

        session.DropUnreferencedBlobs();
    }

    private void FailLoad(CanvasSession session, ICommonSession player, string loc)
    {
        if (player.AttachedEntity is { } attached && Exists(session.Easel))
            Popup(session.Easel, attached, loc);

        session.State = CanvasSessionState.Loading;
        CloseSession(session, CanvasCloseReason.Error);
    }

    private void Resume(CanvasSession session, ICommonSession player)
    {
        if (player.UserId == session.Host && session.State == CanvasSessionState.HostAway)
            session.State = CanvasSessionState.Active;

        Subscribe(session, player);
        UpdateEasel(session);
    }

    private void Subscribe(CanvasSession session, ICommonSession player)
    {
        if (!session.Members.TryGetValue(player.UserId, out var member))
        {
            if (!session.TryAllocateIndex(out var index))
                return;

            var role = player.UserId == session.Host ? CanvasRole.Host : CanvasRole.Viewer;
            member = new CanvasMember(player.UserId, index, DisplayName(player), role)
            {
                SampleTokens = _strokeRate,
                CursorTokens = CursorRate,
            };
            session.Members[player.UserId] = member;

            if (role == CanvasRole.Host)
                session.Contributions.TryAdd(player.UserId, 0);
        }

        member.Player = player;
        member.Name = DisplayName(player);
        member.LastRefill = _timing.RealTime;
        session.Subscribers.Add(player);
        SendManifest(session, player);
    }

    private void Unsubscribe(CanvasSession session, ICommonSession player)
    {
        if (!session.Subscribers.Remove(player))
            return;

        if (_peers.TryGetValue(player.UserId, out var peer))
            peer.ClearOutgoing();

        if (session.Members.TryGetValue(player.UserId, out var member))
        {
            member.Player = null;
            member.OpenStrokes.Clear();

            if (member.Role == CanvasRole.Viewer)
            {
                session.Members.Remove(player.UserId);
                session.Requests.Remove(player.UserId);
            }
            else
            {
                member.AwayUntil = _timing.RealTime + _hostGrace;
            }
        }

        if (session.Host == player.UserId && session.State == CanvasSessionState.Active)
        {
            session.State = CanvasSessionState.HostAway;
            session.AwayUntil = _timing.RealTime + _hostGrace;
        }

        if (!session.Closed)
            UpdateEasel(session);
    }

    private void CloseSession(CanvasSession session, CanvasCloseReason reason)
    {
        if (session.Closed)
            return;

        session.Closed = true;
        _sessions.Remove(session.PaintingUid);

        foreach (var subscriber in session.Subscribers)
        {
            SendClosed(session, subscriber, reason);
            if (_peers.TryGetValue(subscriber.UserId, out var peer))
                peer.ClearOutgoing();
        }

        session.Subscribers.Clear();

        if (TryComp<CanvasEaselComponent>(session.Easel, out var easel) && easel.Session == session.PaintingUid)
        {
            easel.Session = null;
            if (!TerminatingOrDeleted(session.Easel))
            {
                _itemSlots.SetLock(session.Easel, easel.SlotId, false);
                UpdateVisuals((session.Easel, easel));
                UpdateUi((session.Easel, easel));
            }
        }

        if (session.State != CanvasSessionState.Loading && session.Dirty)
            Autosave(session);
    }

    public void CloseSessionsOf(NetUserId user)
    {
        foreach (var session in _sessions.Values.ToList())
        {
            if (session.Host == user)
            {
                CloseSession(session, CanvasCloseReason.Closed);
                continue;
            }

            if (session.Members.TryGetValue(user, out var member) && member.CanDraw)
                Demote(session, member, CanvasCloseReason.Kicked);
        }
    }

    private void Autosave(CanvasSession session)
    {
        if (session.Saving)
            return;

        session.Saving = true;
        var changes = session.Changes;

        var tiles = new List<PaintingTileRecord>(session.Tiles.Length);
        for (var i = 0; i < session.Tiles.Length; i++)
        {
            tiles.Add(new PaintingTileRecord(session.KeyOf(i), session.Tiles[i].Content));
        }

        var newBlobs = new Dictionary<CanvasHash, byte[]>();
        long bytes = 0;
        foreach (var hash in session.ReferencedHashes())
        {
            if (!session.Blobs.TryGetValue(hash, out var data))
                continue;

            bytes += data.Length;
            if (!session.PersistedBlobs.Contains(hash))
                newBlobs[hash] = data;
        }

        foreach (var (uid, pending) in _pendingSaves.ToList())
        {
            if (pending.IsCompleted)
                _pendingSaves.Remove(uid);
        }

        var contributions = new Dictionary<Guid, int>();
        foreach (var user in session.Contributions.Keys.ToList())
        {
            contributions[user.UserId] = session.Contributions[user];
            session.Contributions[user] = 0;
        }

        _pendingSaves[session.PaintingUid] = SaveAsync(session, tiles, newBlobs, bytes, changes, session.Host.UserId, contributions);
    }

    private async Task SaveAsync(CanvasSession session, List<PaintingTileRecord> tiles,
        Dictionary<CanvasHash, byte[]> newBlobs, long bytes, int changes, Guid owner, Dictionary<Guid, int> contributions)
    {
        try
        {
            var locations = await _blobStore.StoreAsync(newBlobs);
            await _db.SavePaintingAutosaveAsync(session.PaintingUid, tiles, newBlobs, locations, bytes, DateTime.UtcNow, owner, contributions);
            session.PersistedBlobs.UnionWith(newBlobs.Keys);
            session.SavedChanges = changes;

            for (var i = 0; i < tiles.Count && i < session.SavedTiles.Length; i++)
            {
                session.SavedTiles[i] = tiles[i].Content;
            }

            session.DropUnreferencedBlobs();
        }
        catch (Exception e)
        {
            foreach (var (user, count) in contributions)
            {
                var id = new NetUserId(user);
                session.Contributions[id] = session.Contributions.GetValueOrDefault(id) + count;
            }

            var lost = session.Closed ? ", session is closed and changes are lost" : string.Empty;
            Log.Error($"Failed to autosave painting {session.PaintingUid}{lost}: {e}");
        }
        finally
        {
            session.Saving = false;
            session.NextAutosave = _timing.RealTime + _autosaveInterval;
        }
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        _peers.Remove(args.Session.UserId);

        foreach (var session in _sessions.Values.ToList())
        {
            Unsubscribe(session, args.Session);
        }
    }

    #endregion
}
