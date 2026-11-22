using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client.ADT.Drawing.View;
using Content.Shared.Administration;
using Content.Shared.ADT.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Drawing.Moderation;

public sealed class PaintingReportsTab : BoxContainer
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    private readonly PaintingReportsSystem _reports;
    private readonly CanvasViewSystem _view;

    private readonly OptionButton _filter;
    private readonly Label _status;
    private readonly ItemList _list;
    private readonly TextureRect _thumb;
    private readonly Label _thumbStatus;
    private readonly Label _details;
    private readonly Button _openButton;
    private readonly Button _resolveButton;
    private readonly Button _hideButton;
    private readonly LineEdit _banMinutes;
    private readonly LineEdit _banReason;
    private readonly Button _banButton;

    private PaintingReportsStateEvent? _state;
    private PaintingReportEntry? _selected;
    private bool _resolved;

    public PaintingReportsTab()
    {
        IoCManager.InjectDependencies(this);
        _reports = _systems.GetEntitySystem<PaintingReportsSystem>();
        _view = _systems.GetEntitySystem<CanvasViewSystem>();

        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        Margin = new Thickness(4);

        TabContainer.SetTabTitle(this, Loc.GetString("adt-painting-reports-tab"));

        _filter = new OptionButton();
        _filter.AddItem(Loc.GetString("adt-painting-reports-filter-open"), 0);
        _filter.AddItem(Loc.GetString("adt-painting-reports-filter-resolved"), 1);
        _filter.OnItemSelected += args =>
        {
            _filter.SelectId(args.Id);
            _resolved = args.Id == 1;
            Refresh();
        };

        var refresh = new Button
        {
            Text = Loc.GetString("adt-canvas-queue-refresh"),
        };
        refresh.OnPressed += _ => Refresh();

        _status = new Label
        {
            FontColorOverride = Color.Gold,
            HorizontalExpand = true,
            ClipText = true,
        };

        _list = new ItemList
        {
            MinWidth = 320,
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };
        _list.OnItemSelected += args => Select(args.ItemList[args.ItemIndex].Metadata as PaintingReportEntry);

        _thumb = new TextureRect
        {
            MinSize = new Vector2(256, 256),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
        };
        _thumbStatus = new Label
        {
            FontColorOverride = Color.Gray,
        };
        _details = new Label
        {
            HorizontalExpand = true,
        };

        _openButton = ActionButton("adt-canvas-queue-open", () =>
        {
            if (_selected != null)
                new PaintingViewerWindow(_selected.PaintingUid).OpenCentered();
        });

        _resolveButton = ActionButton("adt-painting-reports-resolve", () => Send(e => new PaintingReportResolveEvent
        {
            ReportId = e.ReportId,
        }));

        _hideButton = ActionButton("adt-painting-reports-hide", () => Send(e => new PaintingReportHideEvent
        {
            ReportId = e.ReportId,
        }));

        _banMinutes = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-canvas-queue-ban-minutes"),
            MinWidth = 90,
        };
        _banReason = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-canvas-queue-reason"),
            HorizontalExpand = true,
        };
        _banButton = ActionButton("adt-canvas-queue-ban", () => Send(e => new PaintingReportBanEvent
        {
            ReportId = e.ReportId,
            Minutes = int.TryParse(_banMinutes.Text, out var minutes) ? Math.Max(0, minutes) : 0,
            Reason = _banReason.Text,
        }));

        var details = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        details.AddChild(_thumb);
        details.AddChild(_thumbStatus);
        details.AddChild(_details);
        details.AddChild(Row(_openButton, _resolveButton, _hideButton));
        details.AddChild(Row(_banMinutes, _banReason, _banButton));

        var detailsScroll = new ScrollContainer
        {
            HScrollEnabled = false,
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { details },
        };

        var body = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            VerticalExpand = true,
        };
        body.AddChild(_list);
        body.AddChild(detailsScroll);

        AddChild(Row(_filter, refresh, _status));
        AddChild(body);

        Select(null);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _reports.StateReceived += OnState;
        _reports.StatusReceived += OnStatus;
        _reports.Changed += OnChanged;
        _view.ThumbnailChanged += OnThumbnailChanged;
        _admin.AdminStatusUpdated += UpdateAccess;

        UpdateAccess();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _reports.StateReceived -= OnState;
        _reports.StatusReceived -= OnStatus;
        _reports.Changed -= OnChanged;
        _view.ThumbnailChanged -= OnThumbnailChanged;
        _admin.AdminStatusUpdated -= UpdateAccess;
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        base.VisibilityChanged(newVisible);

        if (newVisible)
            Refresh();
    }

    private void UpdateAccess()
    {
        TabContainer.SetTabVisible(this, _admin.HasFlag(AdminFlags.Admin));
    }

    private void Refresh()
    {
        if (_admin.HasFlag(AdminFlags.Admin))
            _reports.Request(_resolved);
    }

    private void OnChanged()
    {
        if (Visible)
            Refresh();
    }

    private void OnStatus(string status)
    {
        _status.Text = status;
    }

    private void OnState(PaintingReportsStateEvent state)
    {
        if (state.Resolved != _resolved)
            return;

        _state = state;

        var selectedId = _selected?.ReportId;
        PaintingReportEntry? reselect = null;

        _list.Clear();
        foreach (var entry in state.Entries)
        {
            var text = Loc.GetString("adt-painting-reports-entry",
                ("title", TitleOf(entry)),
                ("reporter", entry.ReporterName),
                ("date", entry.CreatedAt.ToLocalTime().ToString("dd.MM HH:mm")),
                ("count", entry.OpenReports));

            var item = _list.AddItem(text, metadata: entry);
            if (entry.ReportId == selectedId)
            {
                item.Selected = true;
                reselect = entry;
            }
        }

        Select(reselect);
    }

    private void Select(PaintingReportEntry? entry)
    {
        _selected = entry;

        var has = entry != null;
        _openButton.Disabled = !has || entry!.Status is null or PaintingStatus.Deleted or PaintingStatus.Expired;
        _resolveButton.Disabled = !has || _resolved;
        _hideButton.Disabled = !has || entry!.Status is not (PaintingStatus.Approved or PaintingStatus.Submitted);
        _banButton.Disabled = !has || _state?.CanBan != true || entry!.OwnerId == null;

        if (entry == null)
        {
            _details.Text = Loc.GetString("adt-painting-reports-empty");
            _thumb.Texture = null;
            _thumbStatus.Text = string.Empty;
            return;
        }

        var status = entry.Status is { } painting
            ? Loc.GetString($"adt-painting-status-{painting.ToString().ToLowerInvariant()}")
            : "-";

        _details.Text = Loc.GetString("adt-painting-reports-details",
            ("title", TitleOf(entry)),
            ("status", status),
            ("owner", string.IsNullOrEmpty(entry.OwnerName) ? "-" : entry.OwnerName),
            ("banned", entry.OwnerBanned ? Loc.GetString("adt-canvas-queue-banned") : string.Empty),
            ("reporter", entry.ReporterName),
            ("reason", entry.Reason),
            ("date", entry.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")),
            ("round", entry.RoundId?.ToString() ?? "-"),
            ("count", entry.OpenReports),
            ("resolver", entry.ResolverName ?? "-"),
            ("resolved", entry.ResolvedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "-"));

        UpdateThumb();
    }

    private void UpdateThumb()
    {
        if (_selected == null)
            return;

        if (_view.TryGetThumbnail(_selected.PaintingUid, out var texture))
        {
            _thumb.Texture = texture;
            _thumbStatus.Text = string.Empty;
            return;
        }

        _thumb.Texture = null;
        _thumbStatus.Text = Loc.GetString("adt-canvas-queue-thumb-loading");
    }

    private void OnThumbnailChanged(Guid painting)
    {
        if (_selected?.PaintingUid == painting)
            UpdateThumb();
    }

    private void Send(Func<PaintingReportEntry, EntityEventArgs> factory)
    {
        if (_selected != null)
            _reports.Send(factory(_selected));
    }

    private static string TitleOf(PaintingReportEntry entry)
    {
        return string.IsNullOrWhiteSpace(entry.Title) ? Loc.GetString("adt-canvas-easel-untitled") : entry.Title;
    }

    private static Button ActionButton(string loc, Action action)
    {
        var button = new Button
        {
            Text = Loc.GetString(loc),
        };
        button.OnPressed += _ => action();
        return button;
    }

    private static BoxContainer Row(params Control[] children)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var child in children)
        {
            row.AddChild(child);
        }

        return row;
    }
}
