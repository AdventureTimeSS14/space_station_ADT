using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.ADT.Drawing.Database;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.ADT.Drawing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Drawing;

public sealed class PaintingReportSystem : EntitySystem
{
    private const int MaxReasonLength = 256;
    private const int MaxEntries = 200;

    private static readonly TimeSpan ReportCooldown = TimeSpan.FromMinutes(2);

    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png"));

    [Dependency] private readonly QuickDialogSystem _dialog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly PaintingModerationSystem _moderation = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastReport = new();
    private readonly HashSet<(NetUserId User, Guid Painting)> _reported = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintingComponent, GetVerbsEvent<Verb>>(OnPaintingVerbs);
        SubscribeLocalEvent<PaintingFrameComponent, GetVerbsEvent<Verb>>(OnFrameVerbs);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeNetworkEvent<PaintingReportsRequestEvent>(OnRequest);
        SubscribeNetworkEvent<PaintingReportResolveEvent>(OnResolve);
        SubscribeNetworkEvent<PaintingReportHideEvent>(OnHide);
        SubscribeNetworkEvent<PaintingReportBanEvent>(OnBan);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _lastReport.Clear();
        _reported.Clear();
    }

    #region Подача жалобы

    private void OnPaintingVerbs(Entity<PaintingComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        AddVerb(ref args, ent.Owner, ent.Comp);
    }

    private void OnFrameVerbs(Entity<PaintingFrameComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (_itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SlotId) is { } item &&
            TryComp<PaintingComponent>(item, out var painting))
        {
            AddVerb(ref args, ent.Owner, painting);
        }
    }

    private void AddVerb(ref GetVerbsEvent<Verb> args, EntityUid target, PaintingComponent painting)
    {
        if (painting.PaintingUid is not { } uid ||
            painting.Hidden ||
            !TryComp<ActorComponent>(args.User, out var actor))
        {
            return;
        }

        var player = actor.PlayerSession;
        var user = args.User;
        var title = painting.Title;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("adt-painting-report-verb"),
            Icon = VerbIcon,
            Priority = -10,
            Act = () => OpenReport(player, user, target, uid, title),
        });
    }

    private void OpenReport(ICommonSession player, EntityUid user, EntityUid target, Guid painting, string title)
    {
        if (_reported.Contains((player.UserId, painting)))
        {
            _popup.PopupEntity(Loc.GetString("adt-painting-report-already"), target, user);
            return;
        }

        if (_lastReport.TryGetValue(player.UserId, out var last) && _timing.RealTime - last < ReportCooldown)
        {
            _popup.PopupEntity(Loc.GetString("adt-painting-report-cooldown"), target, user);
            return;
        }

        _dialog.OpenDialog(player,
            Loc.GetString("adt-painting-report-title"),
            Loc.GetString("adt-painting-report-prompt"),
            (string reason) => Report(player, user, target, painting, title, reason));
    }

    private async void Report(ICommonSession player, EntityUid user, EntityUid target, Guid painting, string title, string reason)
    {
        reason = reason.Trim();
        if (reason.Length == 0)
            return;

        if (reason.Length > MaxReasonLength)
            reason = reason.Substring(0, MaxReasonLength);

        if (!_reported.Add((player.UserId, painting)))
            return;

        _lastReport[player.UserId] = _timing.RealTime;

        try
        {
            if (await _db.HasOpenPaintingReportAsync(painting, player.UserId))
            {
                Popup(target, user, "adt-painting-report-already");
                return;
            }

            int? roundId = _ticker.RoundId > 0 ? _ticker.RoundId : null;
            var server = await _serverDbEntry.ServerEntity;
            await _db.AddPaintingReportAsync(painting, player.UserId, reason, DateTime.UtcNow, roundId, server.Id);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save painting report {painting}: {e}");
            _reported.Remove((player.UserId, painting));
            Popup(target, user, "adt-canvas-mod-failed");
            return;
        }

        var name = string.IsNullOrWhiteSpace(title) ? Loc.GetString("adt-canvas-easel-untitled") : title;
        if (Exists(user))
        {
            _chat.SendAdminAlert(user, Loc.GetString("adt-painting-report-admin",
                ("title", FormattedMessage.EscapeText(name)),
                ("uid", painting.ToString()),
                ("reason", FormattedMessage.EscapeText(reason))));

            _adminLog.Add(LogType.CanvasReport, LogImpact.Medium,
                $"{ToPrettyString(user):player} reported painting {painting} \"{name}\" on {ToPrettyString(target)}: {reason}");
        }

        Popup(target, user, "adt-painting-report-sent");
        BroadcastChanged();
    }

    private void Popup(EntityUid target, EntityUid user, string loc)
    {
        if (Exists(target) && Exists(user))
            _popup.PopupEntity(Loc.GetString(loc), target, user);
    }

    #endregion

    #region Вкладка жалоб

    private void OnRequest(PaintingReportsRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!_moderation.CanModerate(args.SenderSession))
            return;

        SendStateAsync(args.SenderSession, ev.Resolved);
    }

    private void OnResolve(PaintingReportResolveEvent ev, EntitySessionEventArgs args)
    {
        var admin = args.SenderSession;
        if (!_moderation.CanModerate(admin))
            return;

        RunAsync(admin, ResolveAsync(admin, ev.ReportId));
    }

    private void OnHide(PaintingReportHideEvent ev, EntitySessionEventArgs args)
    {
        var admin = args.SenderSession;
        if (!_moderation.CanModerate(admin))
            return;

        RunAsync(admin, HideAsync(admin, ev.ReportId));
    }

    private void OnBan(PaintingReportBanEvent ev, EntitySessionEventArgs args)
    {
        var admin = args.SenderSession;
        if (!_moderation.CanModerate(admin))
            return;

        RunAsync(admin, BanAsync(admin, ev.ReportId, ev.Minutes, ev.Reason));
    }

    private async Task<string> ResolveAsync(ICommonSession admin, int reportId)
    {
        if (!await _db.ResolvePaintingReportAsync(reportId, admin.UserId, DateTime.UtcNow))
            return Loc.GetString("adt-painting-reports-not-found");

        _adminLog.Add(LogType.CanvasReport, LogImpact.Low, $"{admin.Name} resolved painting report {reportId}");
        return Loc.GetString("adt-painting-reports-resolved");
    }

    private async Task<string> HideAsync(ICommonSession admin, int reportId)
    {
        var report = await _db.GetPaintingReportAsync(reportId);
        if (report == null)
            return Loc.GetString("adt-painting-reports-not-found");

        var result = await _moderation.SetHidden(admin, report.PaintingUid, true);
        await _db.ResolvePaintingReportsForAsync(report.PaintingUid, admin.UserId, DateTime.UtcNow);
        return result;
    }

    private async Task<string> BanAsync(ICommonSession admin, int reportId, int minutes, string reason)
    {
        var report = await _db.GetPaintingReportAsync(reportId);
        if (report?.OwnerUserId is not { } owner)
            return Loc.GetString("adt-painting-reports-not-found");

        return await _moderation.Ban(admin, owner, Math.Max(0, minutes), reason);
    }

    private async void RunAsync(ICommonSession admin, Task<string> action)
    {
        string status;
        try
        {
            status = await action;
        }
        catch (Exception e)
        {
            Log.Error($"Painting report action failed: {e}");
            status = Loc.GetString("adt-canvas-mod-failed");
        }

        RaiseNetworkEvent(new PaintingReportsStatusEvent
        {
            Status = status,
        }, admin);

        BroadcastChanged();
    }

    private async void SendStateAsync(ICommonSession admin, bool resolved)
    {
        try
        {
            var reports = await _db.GetPaintingReportsAsync(resolved, MaxEntries);

            var owners = reports
                .Where(r => r.OwnerUserId != null)
                .Select(r => r.OwnerUserId!.Value)
                .Distinct()
                .ToList();
            var stats = await _db.GetPaintingOwnerStatsAsync(owners, DateTime.UtcNow);

            var users = owners
                .Concat(reports.Select(r => r.ReporterUserId))
                .Concat(reports.Where(r => r.ResolvedBy != null).Select(r => r.ResolvedBy!.Value))
                .Distinct();

            var names = new Dictionary<Guid, string>();
            foreach (var user in users)
            {
                var record = await _db.GetPlayerRecordByUserId(new NetUserId(user));
                names[user] = record?.LastSeenUserName ?? user.ToString();
            }

            if (admin.Status == Robust.Shared.Enums.SessionStatus.Disconnected)
                return;

            var state = new PaintingReportsStateEvent
            {
                Resolved = resolved,
                CanBan = _moderation.CanBan(admin),
                Entries = reports.Select(r => ToEntry(r, stats, names)).ToList(),
            };

            RaiseNetworkEvent(state, admin);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load painting reports: {e}");
        }
    }

    private void BroadcastChanged()
    {
        foreach (var admin in _admins.ActiveAdmins)
        {
            if (_moderation.CanModerate(admin))
                RaiseNetworkEvent(new PaintingReportsChangedEvent(), admin);
        }
    }

    private static PaintingReportEntry ToEntry(PaintingReportRecord record,
        Dictionary<Guid, PaintingOwnerStats> stats, Dictionary<Guid, string> names)
    {
        var entry = new PaintingReportEntry
        {
            ReportId = record.ReportId,
            PaintingUid = record.PaintingUid,
            Title = record.Title ?? string.Empty,
            Status = record.Status is { } status ? (PaintingStatus) status : null,
            OwnerId = record.OwnerUserId,
            ReporterName = names.GetValueOrDefault(record.ReporterUserId) ?? record.ReporterUserId.ToString(),
            Reason = record.Reason,
            CreatedAt = record.CreatedAt,
            RoundId = record.RoundId,
            OpenReports = record.OpenReports,
            ResolverName = record.ResolvedBy is { } resolver ? names.GetValueOrDefault(resolver) : null,
            ResolvedAt = record.ResolvedAt,
        };

        if (record.OwnerUserId is { } owner)
        {
            entry.OwnerName = names.GetValueOrDefault(owner) ?? owner.ToString();
            entry.OwnerBanned = stats.GetValueOrDefault(owner)?.Banned ?? false;
        }

        return entry;
    }

    #endregion
}
