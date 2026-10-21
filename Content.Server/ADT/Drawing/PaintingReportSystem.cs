using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
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

    private static readonly TimeSpan ReportCooldown = TimeSpan.FromMinutes(2);

    private static readonly SpriteSpecifier VerbIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png"));

    [Dependency] private readonly QuickDialogSystem _dialog = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    private readonly Dictionary<NetUserId, TimeSpan> _lastReport = new();
    private readonly HashSet<(NetUserId User, Guid Painting)> _reported = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintingComponent, GetVerbsEvent<Verb>>(OnPaintingVerbs);
        SubscribeLocalEvent<PaintingFrameComponent, GetVerbsEvent<Verb>>(OnFrameVerbs);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _lastReport.Clear();
        _reported.Clear();
    }

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

    private void Report(ICommonSession player, EntityUid user, EntityUid target, Guid painting, string title, string reason)
    {
        reason = reason.Trim();
        if (reason.Length == 0)
            return;

        if (reason.Length > MaxReasonLength)
            reason = reason.Substring(0, MaxReasonLength);

        if (!_reported.Add((player.UserId, painting)))
            return;

        _lastReport[player.UserId] = _timing.RealTime;

        var name = string.IsNullOrWhiteSpace(title) ? Loc.GetString("adt-canvas-easel-untitled") : title;
        _chat.SendAdminAlert(user, Loc.GetString("adt-painting-report-admin",
            ("title", FormattedMessage.EscapeText(name)),
            ("uid", painting.ToString()),
            ("reason", FormattedMessage.EscapeText(reason))));

        _adminLog.Add(LogType.CanvasReport, LogImpact.Medium,
            $"{ToPrettyString(user):player} reported painting {painting} \"{name}\" on {ToPrettyString(target)}: {reason}");

        if (Exists(target) && Exists(user))
            _popup.PopupEntity(Loc.GetString("adt-painting-report-sent"), target, user);
    }
}
