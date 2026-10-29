using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Drawing;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Drawing;

public sealed class PaintingModerationSystem : EntitySystem
{
    public const string NotificationChannel = "adt_painting_changed";

    private static readonly EntProtoId SpawnedPainting = "ADTPainting";

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly PaintingBlobStoreSystem _blobStore = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly CanvasSessionSystem _canvas = default!;
    [Dependency] private readonly PaintingFrameSystem _frames = default!;

    private int _draftTtlDays;
    private int _rejectTtlDays;
    private long _approvedLimitBytes;
    private bool _cleanedOnStart;
    private bool _whitelist;

    public long ApprovedLimitBytes => _approvedLimitBytes;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);

        Subs.CVar(_cfg, ADTCCVars.CanvasDraftTtlDays, v => _draftTtlDays = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasRejectTtlDays, v => _rejectTtlDays = v, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasApprovedTotalMb, v => _approvedLimitBytes = v * 1024L * 1024L, true);
        Subs.CVar(_cfg, ADTCCVars.CanvasWhitelist, v => _whitelist = v, true);

        _db.SubscribeToJsonNotification<PaintingChangedNotification>(_taskManager, Log, NotificationChannel, OnRemoteChanged);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    #region Права

    public bool CanModerate(ICommonSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.Admin);
    }

    public bool CanDelete(ICommonSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.Admin);
    }

    public bool CanBan(ICommonSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.Ban);
    }

    public bool CanSpawn(ICommonSession player)
    {
        return _admins.HasAdminFlag(player, AdminFlags.Spawn);
    }

    public async Task<string?> CheckDrawAccess(Guid userId)
    {
        if (await _db.GetPaintingBanAsync(userId, DateTime.UtcNow) != null)
            return "adt-canvas-error-banned";

        if (_whitelist && !await _db.IsPaintingWhitelistedAsync(userId))
            return "adt-canvas-error-not-whitelisted";

        return null;
    }

    #endregion

    #region Действия админов

    public async Task<string> Review(ICommonSession admin, Guid paintingUid, bool approve, string? reason)
    {
        if (!CanModerate(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason))
            reason = null;

        if (!approve && reason == null)
            return Loc.GetString("adt-canvas-mod-reason-required");

        if (reason != null && reason.Length > CanvasConstants.MaxReasonLength)
            reason = reason.Substring(0, CanvasConstants.MaxReasonLength);

        var summary = await _db.GetPaintingSummaryAsync(paintingUid);
        if (summary == null)
            return Loc.GetString("adt-canvas-mod-not-found");

        if (!await _db.ReviewPaintingAsync(paintingUid, approve, admin.UserId, reason, DateTime.UtcNow))
            return Loc.GetString("adt-canvas-mod-already-reviewed");

        var verb = approve ? "approved" : "rejected";
        _adminLog.Add(LogType.CanvasModeration, LogImpact.Medium,
            $"{admin.Name} {verb} painting {paintingUid} \"{summary.Title}\" by {summary.OwnerUserId}, reason: {reason ?? "-"}");

        ApplyLocal(paintingUid, false, false);
        Broadcast(paintingUid, false, false, false);
        NotifyAuthorDecision(summary.OwnerUserId, paintingUid, summary.Title, approve, reason);

        if (approve)
            CheckStorageLimit();

        return Loc.GetString(approve ? "adt-canvas-mod-approved" : "adt-canvas-mod-rejected", ("title", summary.Title));
    }

    public async Task<string> SetHidden(ICommonSession admin, Guid paintingUid, bool hidden)
    {
        if (!CanModerate(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        var summary = await _db.GetPaintingSummaryAsync(paintingUid);
        if (summary == null)
            return Loc.GetString("adt-canvas-mod-not-found");

        if (summary.Status is AdtPaintingStatus.Approved or AdtPaintingStatus.Hidden)
            await _db.SetPaintingHiddenAsync(paintingUid, hidden, DateTime.UtcNow);

        var verb = hidden ? "hid" : "unhid";
        _adminLog.Add(LogType.CanvasModeration, LogImpact.Medium,
            $"{admin.Name} {verb} painting {paintingUid} \"{summary.Title}\" by {summary.OwnerUserId}");

        ApplyLocal(paintingUid, hidden, hidden);
        Broadcast(paintingUid, hidden, hidden, false);

        return Loc.GetString(hidden ? "adt-canvas-mod-hidden" : "adt-canvas-mod-unhidden", ("title", summary.Title));
    }

    public async Task<string> Delete(ICommonSession admin, Guid paintingUid)
    {
        if (!CanDelete(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        var summary = await _db.GetPaintingSummaryAsync(paintingUid);
        if (summary == null || !await _db.DeletePaintingAsync(paintingUid, DateTime.UtcNow))
            return Loc.GetString("adt-canvas-mod-not-found");

        _adminLog.Add(LogType.CanvasModeration, LogImpact.High,
            $"{admin.Name} deleted painting {paintingUid} \"{summary.Title}\" by {summary.OwnerUserId}");

        ApplyLocal(paintingUid, true, true);
        Broadcast(paintingUid, true, true, false);

        return Loc.GetString("adt-canvas-mod-deleted", ("title", summary.Title));
    }

    public async Task<string> SetPinned(ICommonSession admin, Guid paintingUid, bool pinned)
    {
        if (!CanModerate(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        if (!await _db.SetPaintingPinnedAsync(paintingUid, pinned))
            return Loc.GetString("adt-canvas-mod-not-found");

        var verb = pinned ? "pinned" : "unpinned";
        _adminLog.Add(LogType.CanvasModeration, LogImpact.Low, $"{admin.Name} {verb} painting {paintingUid}");
        return Loc.GetString(pinned ? "adt-canvas-mod-pinned" : "adt-canvas-mod-unpinned");
    }

    public async Task<string> SpawnPainting(ICommonSession admin, Guid paintingUid)
    {
        if (!CanSpawn(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        var summary = await _db.GetPaintingSummaryAsync(paintingUid);
        if (summary == null)
            return Loc.GetString("adt-canvas-mod-not-found");

        if (summary.Status != AdtPaintingStatus.Approved)
            return Loc.GetString("adt-canvas-mod-spawn-not-approved");

        if (admin.AttachedEntity is not { } attached || TerminatingOrDeleted(attached))
            return Loc.GetString("adt-canvas-mod-spawn-no-entity");

        var item = Spawn(SpawnedPainting, Transform(attached).Coordinates);
        var painting = EnsureComp<PaintingComponent>(item);
        painting.PaintingUid = summary.PaintingUid;
        painting.Size = new Vector2i(summary.Width, summary.Height);
        painting.Title = summary.Title;
        Dirty(item, painting);

        _adminLog.Add(LogType.CanvasModeration, LogImpact.Low,
            $"{admin.Name} spawned painting {paintingUid} \"{summary.Title}\" as {ToPrettyString(item)}");
        return Loc.GetString("adt-canvas-mod-spawned", ("title", summary.Title));
    }

    public async Task<string> Ban(ICommonSession admin, Guid userId, int minutes, string reason)
    {
        if (!CanBan(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        reason = reason.Trim();
        if (reason.Length == 0)
            return Loc.GetString("adt-canvas-mod-reason-required");

        if (reason.Length > CanvasConstants.MaxReasonLength)
            reason = reason.Substring(0, CanvasConstants.MaxReasonLength);

        var now = DateTime.UtcNow;
        DateTime? expires = minutes > 0 ? now + TimeSpan.FromMinutes(minutes) : null;
        await _db.AddPaintingBanAsync(userId, admin.UserId, reason, now, expires);

        _adminLog.Add(LogType.CanvasDrawingBan, LogImpact.High,
            $"{admin.Name} banned {userId} from painting for {(minutes > 0 ? minutes + " min" : "ever")}, reason: {reason}");

        _canvas.CloseSessionsOf(new NetUserId(userId));
        BroadcastRevoked(userId, false);
        return Loc.GetString("adt-canvas-mod-banned");
    }

    public async Task<string> Unban(ICommonSession admin, Guid userId)
    {
        if (!CanBan(admin))
            return Loc.GetString("adt-canvas-mod-no-access");

        if (!await _db.RemovePaintingBanAsync(userId))
            return Loc.GetString("adt-canvas-mod-not-banned");

        _adminLog.Add(LogType.CanvasDrawingBan, LogImpact.Medium, $"{admin.Name} unbanned {userId} from painting");
        return Loc.GetString("adt-canvas-mod-unbanned");
    }

    public async Task<string> WhitelistAdd(ICommonSession? admin, Guid userId, string name)
    {
        if (!await _db.AddPaintingWhitelistAsync(userId, admin?.UserId.UserId ?? Guid.Empty, DateTime.UtcNow))
            return Loc.GetString("adt-canvas-mod-whitelist-exists", ("name", name));

        _adminLog.Add(LogType.CanvasWhitelist, LogImpact.Medium,
            $"{admin?.Name ?? "Server"} added {name} ({userId}) to painting whitelist");
        return Loc.GetString("adt-canvas-mod-whitelist-added", ("name", name));
    }

    public async Task<string> WhitelistRemove(ICommonSession? admin, Guid userId, string name)
    {
        if (!await _db.RemovePaintingWhitelistAsync(userId))
            return Loc.GetString("adt-canvas-mod-whitelist-missing", ("name", name));

        _adminLog.Add(LogType.CanvasWhitelist, LogImpact.Medium,
            $"{admin?.Name ?? "Server"} removed {name} ({userId}) from painting whitelist");

        if (_whitelist)
            _canvas.CloseSessionsOf(new NetUserId(userId));

        BroadcastRevoked(userId, true);
        return Loc.GetString("adt-canvas-mod-whitelist-removed", ("name", name));
    }

    #endregion

    #region Применение изменений

    public void OnSubmitted(Guid paintingUid, string title)
    {
        AnnounceSubmissionAsync(title);
    }

    private async void AnnounceSubmissionAsync(string title)
    {
        try
        {
            var pending = await _db.GetPaintingsByStatusAsync(AdtPaintingStatus.Submitted, 200);
            _chat.SendAdminAnnouncement(Loc.GetString("adt-canvas-mod-new-submission",
                ("title", title),
                ("count", pending.Count)));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to announce painting submission: {e}");
        }
    }

    public void ApplyWithdrawn(Guid paintingUid)
    {
        var query = EntityQueryEnumerator<PaintingComponent>();
        var found = new List<EntityUid>();
        while (query.MoveNext(out var uid, out var painting))
        {
            if (painting.PaintingUid == paintingUid)
                found.Add(uid);
        }

        foreach (var uid in found)
        {
            _frames.EjectFromFrame(uid);
            RemComp<PaintingComponent>(uid);
        }

        _canvas.RefreshEasels();
        _canvas.InvalidatePainting(paintingUid, false);
        Broadcast(paintingUid, false, false, true);
    }

    private void ApplyLocal(Guid paintingUid, bool hidden, bool dropCache)
    {
        var query = EntityQueryEnumerator<PaintingComponent>();
        while (query.MoveNext(out var uid, out var painting))
        {
            if (painting.PaintingUid != paintingUid || painting.Hidden == hidden)
                continue;

            painting.Hidden = hidden;
            Dirty(uid, painting);
        }

        _frames.RefreshFor(paintingUid);
        _canvas.RefreshEasels();
        _canvas.InvalidatePainting(paintingUid, dropCache);
    }

    private void Broadcast(Guid paintingUid, bool hidden, bool dropCache, bool withdrawn)
    {
        SendChange(new PaintingChangedNotification
        {
            Painting = paintingUid,
            Hidden = hidden,
            DropCache = dropCache,
            Withdrawn = withdrawn,
        });
    }

    private void BroadcastRevoked(Guid userId, bool whitelist)
    {
        SendChange(new PaintingChangedNotification
        {
            RevokedUser = userId,
            Whitelist = whitelist,
        });
    }

    private async void SendChange(PaintingChangedNotification payload)
    {
        try
        {
            var server = await _serverDbEntry.ServerEntity;
            payload.ServerId = server.Id;

            await _db.SendNotification(new DatabaseNotification
            {
                Channel = NotificationChannel,
                Payload = JsonSerializer.Serialize(payload),
            });
        }
        catch (Exception e)
        {
            Log.Error($"Failed to broadcast painting change {payload.Painting}: {e}");
        }
    }

    private async void OnRemoteChanged(PaintingChangedNotification notification)
    {
        try
        {
            var server = await _serverDbEntry.ServerEntity;
            if (notification.ServerId == server.Id)
                return;

            if (notification.RevokedUser is { } revoked)
            {
                if (!notification.Whitelist || _whitelist)
                    _canvas.CloseSessionsOf(new NetUserId(revoked));

                return;
            }

            if (notification.Withdrawn)
            {
                var query = EntityQueryEnumerator<PaintingComponent>();
                var found = new List<EntityUid>();
                while (query.MoveNext(out var uid, out var painting))
                {
                    if (painting.PaintingUid == notification.Painting)
                        found.Add(uid);
                }

                foreach (var uid in found)
                {
                    _frames.EjectFromFrame(uid);
                    RemComp<PaintingComponent>(uid);
                }

                _canvas.InvalidatePainting(notification.Painting, false);
                return;
            }

            ApplyLocal(notification.Painting, notification.Hidden, notification.DropCache);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to apply remote painting change {notification.Painting}: {e}");
        }
    }

    #endregion

    #region Уведомления автора

    public void NotifyPlayer(ICommonSession player, string message)
    {
        _chat.DispatchServerMessage(player, message);
    }

    private async void NotifyAuthorDecision(Guid owner, Guid paintingUid, string title, bool approved, string? reason)
    {
        try
        {
            if (!_players.TryGetSessionById(new NetUserId(owner), out var player) || player.Status != SessionStatus.InGame)
                return;

            NotifyPlayer(player, DecisionText(title, approved, reason));
            var notified = new List<Guid>
            {
                paintingUid,
            };
            await _db.MarkPaintingsNotifiedAsync(notified);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to notify author of painting {paintingUid}: {e}");
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.InGame)
            return;

        try
        {
            var decisions = await _db.GetUnnotifiedPaintingDecisionsAsync(args.Session.UserId);
            if (decisions.Count == 0 || args.Session.Status != SessionStatus.InGame)
                return;

            var notified = new List<Guid>();
            foreach (var decision in decisions)
            {
                NotifyPlayer(args.Session, DecisionText(decision.Title, decision.Status == AdtPaintingStatus.Approved, decision.Comment));
                notified.Add(decision.PaintingUid);
            }

            await _db.MarkPaintingsNotifiedAsync(notified);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to deliver painting decisions to {args.Session.Name}: {e}");
        }
    }

    private string DecisionText(string title, bool approved, string? reason)
    {
        if (approved)
            return Loc.GetString("adt-canvas-decision-approved", ("title", title));

        return Loc.GetString("adt-canvas-decision-rejected", ("title", title), ("reason", reason ?? "-"));
    }

    #endregion

    #region Обслуживание

    private void OnRoundStarting(RoundStartingEvent args)
    {
        if (_cleanedOnStart)
            return;

        _cleanedOnStart = true;
        CleanupAsync();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        CleanupAsync();
    }

    private async void CleanupAsync()
    {
        try
        {
            var result = await _db.CleanupPaintingsAsync(
                DateTime.UtcNow,
                TimeSpan.FromDays(_draftTtlDays),
                TimeSpan.FromDays(_rejectTtlDays));

            if (result.ExpiredDrafts > 0 || result.DeletedRejected > 0 || result.DeletedBlobs > 0)
            {
                Log.Info($"Painting cleanup: {result.ExpiredDrafts} drafts expired, " +
                         $"{result.DeletedRejected} rejected deleted, {result.DeletedBlobs} blobs removed");
            }

            await _blobStore.DeleteAsync(result.DeletedLocations);

            CheckStorageLimit();
        }
        catch (Exception e)
        {
            Log.Error($"Painting cleanup failed: {e}");
        }
    }

    private async void CheckStorageLimit()
    {
        if (_approvedLimitBytes <= 0)
            return;

        try
        {
            var total = await _db.GetApprovedPaintingBytesAsync();
            if (total < _approvedLimitBytes * 9 / 10)
                return;

            _chat.SendAdminAnnouncement(Loc.GetString("adt-canvas-mod-storage-warning",
                ("used", total / (1024 * 1024)),
                ("limit", _approvedLimitBytes / (1024 * 1024))));
        }
        catch (Exception e)
        {
            Log.Error($"Failed to check painting storage: {e}");
        }
    }

    #endregion

    private sealed class PaintingChangedNotification
    {
        [JsonPropertyName("painting")]
        public Guid Painting { get; set; }

        [JsonPropertyName("server_id")]
        public int ServerId { get; set; }

        [JsonPropertyName("hidden")]
        public bool Hidden { get; set; }

        [JsonPropertyName("drop_cache")]
        public bool DropCache { get; set; }

        [JsonPropertyName("withdrawn")]
        public bool Withdrawn { get; set; }

        [JsonPropertyName("revoked_user")]
        public Guid? RevokedUser { get; set; }

        [JsonPropertyName("whitelist")]
        public bool Whitelist { get; set; }
    }
}
