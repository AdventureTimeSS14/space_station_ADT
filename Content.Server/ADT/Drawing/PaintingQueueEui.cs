using System.Linq;
using System.Threading.Tasks;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.ADT.Drawing;
using Content.Shared.Eui;

namespace Content.Server.ADT.Drawing;

public sealed class PaintingQueueEui : BaseEui
{
    private const int MaxEntries = 100;

    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private readonly PaintingModerationSystem _moderation;
    private readonly ISawmill _sawmill;

    private PaintingQueueFilter _filter = PaintingQueueFilter.Submitted;
    private List<PaintingQueueEntry> _entries = new();
    private long _approvedBytes;
    private string _status = string.Empty;
    private int _refreshVersion;

    public PaintingQueueEui()
    {
        IoCManager.InjectDependencies(this);

        _moderation = _systems.GetEntitySystem<PaintingModerationSystem>();
        _sawmill = _log.GetSawmill("adt.canvas.queue");
    }

    public override void Opened()
    {
        base.Opened();
        RefreshAsync();
    }

    public override EuiStateBase GetNewState()
    {
        return new PaintingQueueEuiState
        {
            Filter = _filter,
            Entries = _entries,
            CanModerate = _moderation.CanModerate(Player),
            CanDelete = _moderation.CanDelete(Player),
            CanBan = _moderation.CanBan(Player),
            CanSpawn = _moderation.CanSpawn(Player),
            ApprovedTotalBytes = _approvedBytes,
            ApprovedLimitBytes = _moderation.ApprovedLimitBytes,
            Status = _status,
        };
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is CloseEuiMessage)
            return;

        switch (msg)
        {
            case PaintingQueueEuiMsg.Spawn spawn:
                RunAsync(_moderation.SpawnPainting(Player, spawn.Painting));
                return;

            case PaintingQueueEuiMsg.Ban ban:
                RunAsync(_moderation.Ban(Player, ban.UserId, ban.Minutes, ban.Reason));
                return;

            case PaintingQueueEuiMsg.Unban unban:
                RunAsync(_moderation.Unban(Player, unban.UserId));
                return;
        }

        if (!_moderation.CanModerate(Player))
        {
            _status = Loc.GetString("adt-canvas-mod-no-access");
            StateDirty();
            return;
        }

        switch (msg)
        {
            case PaintingQueueEuiMsg.Refresh refresh:
                _filter = refresh.Filter;
                RefreshAsync();
                break;

            case PaintingQueueEuiMsg.Approve approve:
                RunAsync(_moderation.Review(Player, approve.Painting, true, null));
                break;

            case PaintingQueueEuiMsg.Reject reject:
                RunAsync(_moderation.Review(Player, reject.Painting, false, reject.Reason));
                break;

            case PaintingQueueEuiMsg.SetHidden hide:
                RunAsync(_moderation.SetHidden(Player, hide.Painting, hide.Hidden));
                break;

            case PaintingQueueEuiMsg.Delete delete:
                RunAsync(_moderation.Delete(Player, delete.Painting));
                break;

            case PaintingQueueEuiMsg.SetPinned pin:
                RunAsync(_moderation.SetPinned(Player, pin.Painting, pin.Pinned));
                break;
        }
    }

    private async void RunAsync(Task<string> action)
    {
        try
        {
            _status = await action;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Painting moderation action failed: {e}");
            _status = Loc.GetString("adt-canvas-mod-failed");
        }

        if (IsShutDown)
            return;

        RefreshAsync();
    }

    private async void RefreshAsync()
    {
        var version = ++_refreshVersion;
        try
        {
            var status = _filter switch
            {
                PaintingQueueFilter.Approved => AdtPaintingStatus.Approved,
                PaintingQueueFilter.Hidden => AdtPaintingStatus.Hidden,
                PaintingQueueFilter.Rejected => AdtPaintingStatus.Rejected,
                _ => AdtPaintingStatus.Submitted,
            };

            var summaries = await _db.GetPaintingsByStatusAsync(status, MaxEntries);
            var owners = summaries.Select(s => s.OwnerUserId).Distinct().ToList();
            var stats = await _db.GetPaintingOwnerStatsAsync(owners, DateTime.UtcNow);
            var names = new Dictionary<Guid, string>();

            var users = owners
                .Concat(summaries.SelectMany(s => s.Contributors.Select(c => c.UserId)))
                .Concat(summaries.Where(s => s.ReviewerUserId != null).Select(s => s.ReviewerUserId!.Value))
                .Distinct();

            foreach (var user in users)
            {
                var record = await _db.GetPlayerRecordByUserId(new Robust.Shared.Network.NetUserId(user));
                names[user] = record?.LastSeenUserName ?? user.ToString();
            }

            var approvedBytes = await _db.GetApprovedPaintingBytesAsync();

            if (IsShutDown || version != _refreshVersion)
                return;

            _entries = summaries.Select(s => ToEntry(s, stats, names)).ToList();
            _approvedBytes = approvedBytes;
            StateDirty();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load painting queue: {e}");
            if (IsShutDown)
                return;

            _status = Loc.GetString("adt-canvas-mod-failed");
            StateDirty();
        }
    }

    private static PaintingQueueEntry ToEntry(PaintingSummary summary, Dictionary<Guid, PaintingOwnerStats> stats,
        Dictionary<Guid, string> names)
    {
        var owner = stats.GetValueOrDefault(summary.OwnerUserId) ?? new PaintingOwnerStats();
        return new PaintingQueueEntry
        {
            PaintingUid = summary.PaintingUid,
            Title = summary.Title,
            Description = summary.Description ?? string.Empty,
            Width = summary.Width,
            Height = summary.Height,
            Status = (PaintingStatus) summary.Status,
            OwnerId = summary.OwnerUserId,
            OwnerName = names.GetValueOrDefault(summary.OwnerUserId) ?? summary.OwnerUserId.ToString(),
            SubmittedAt = summary.SubmittedAt,
            ReviewedAt = summary.ReviewedAt,
            ReviewerName = summary.ReviewerUserId is { } reviewer ? names.GetValueOrDefault(reviewer) : null,
            ReviewComment = summary.ReviewComment,
            RoundId = summary.RoundId,
            Bytes = summary.BytesTotal,
            Pinned = summary.Pinned,
            Contributors = summary.Contributors
                .Select(c => $"{names.GetValueOrDefault(c.UserId) ?? c.UserId.ToString()} ({c.Tiles})")
                .ToList(),
            OwnerApproved = owner.Approved,
            OwnerRejected = owner.Rejected,
            OwnerBanned = owner.Banned,
        };
    }
}
