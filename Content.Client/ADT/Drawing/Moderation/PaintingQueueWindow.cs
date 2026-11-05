using System.Numerics;
using Content.Client.ADT.Drawing.View;
using Content.Shared.ADT.Drawing;
using Content.Shared.Eui;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Drawing.Moderation;

public sealed class PaintingQueueWindow : DefaultWindow
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    private readonly CanvasViewSystem _view;

    private readonly OptionButton _filter;
    private readonly Label _status;
    private readonly Label _storage;
    private readonly ItemList _list;
    private readonly TextureRect _thumb;
    private readonly Label _thumbStatus;
    private readonly Label _details;
    private readonly Button _openButton;
    private readonly Button _approveButton;
    private readonly Button _rejectButton;
    private readonly LineEdit _rejectReason;
    private readonly Button _hideButton;
    private readonly Button _pinButton;
    private readonly Button _deleteButton;
    private readonly Button _spawnButton;
    private readonly LineEdit _banMinutes;
    private readonly LineEdit _banReason;
    private readonly Button _banButton;
    private readonly Button _unbanButton;

    private PaintingQueueEuiState? _state;
    private PaintingQueueEntry? _selected;
    private bool _deleteArmed;

    public PaintingQueueWindow()
    {
        IoCManager.InjectDependencies(this);
        _view = _systems.GetEntitySystem<CanvasViewSystem>();

        Title = Loc.GetString("adt-canvas-queue-title");
        MinSize = new Vector2(760, 480);
        SetSize = new Vector2(980, 640);

        _filter = new OptionButton();
        foreach (var filter in Enum.GetValues<PaintingQueueFilter>())
        {
            _filter.AddItem(Loc.GetString($"adt-canvas-queue-filter-{filter.ToString().ToLowerInvariant()}"), (int) filter);
        }
        _filter.OnItemSelected += args =>
        {
            _filter.SelectId(args.Id);
            MessageRequested?.Invoke(new PaintingQueueEuiMsg.Refresh
            {
                Filter = (PaintingQueueFilter) args.Id,
            });
        };

        var refresh = new Button
        {
            Text = Loc.GetString("adt-canvas-queue-refresh"),
        };
        refresh.OnPressed += _ => MessageRequested?.Invoke(new PaintingQueueEuiMsg.Refresh
        {
            Filter = _state?.Filter ?? PaintingQueueFilter.Submitted,
        });

        _status = new Label
        {
            FontColorOverride = Color.Gold,
            HorizontalExpand = true,
            ClipText = true,
        };
        _storage = new Label
        {
            FontColorOverride = Color.Gray,
        };

        var top = Row(_filter, refresh, _status, _storage);

        _list = new ItemList
        {
            MinWidth = 320,
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };
        _list.OnItemSelected += args => Select(args.ItemList[args.ItemIndex].Metadata as PaintingQueueEntry);

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

        _approveButton = ActionButton("adt-canvas-queue-approve", () => Send(e => new PaintingQueueEuiMsg.Approve
        {
            Painting = e.PaintingUid,
        }));

        _rejectReason = new LineEdit
        {
            PlaceHolder = Loc.GetString("adt-canvas-queue-reason"),
            HorizontalExpand = true,
        };
        _rejectButton = ActionButton("adt-canvas-queue-reject", () => Send(e => new PaintingQueueEuiMsg.Reject
        {
            Painting = e.PaintingUid,
            Reason = _rejectReason.Text,
        }));

        _hideButton = ActionButton("adt-canvas-queue-hide", () => Send(e => new PaintingQueueEuiMsg.SetHidden
        {
            Painting = e.PaintingUid,
            Hidden = e.Status != PaintingStatus.Hidden,
        }));

        _pinButton = ActionButton("adt-canvas-queue-pin", () => Send(e => new PaintingQueueEuiMsg.SetPinned
        {
            Painting = e.PaintingUid,
            Pinned = !e.Pinned,
        }));

        _deleteButton = ActionButton("adt-canvas-queue-delete", OnDeletePressed);

        _spawnButton = ActionButton("adt-canvas-queue-spawn", () => Send(e => new PaintingQueueEuiMsg.Spawn
        {
            Painting = e.PaintingUid,
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
        _banButton = ActionButton("adt-canvas-queue-ban", () => Send(e => new PaintingQueueEuiMsg.Ban
        {
            UserId = e.OwnerId,
            Minutes = int.TryParse(_banMinutes.Text, out var minutes) ? Math.Max(0, minutes) : 0,
            Reason = _banReason.Text,
        }));
        _unbanButton = ActionButton("adt-canvas-queue-unban", () => Send(e => new PaintingQueueEuiMsg.Unban
        {
            UserId = e.OwnerId,
        }));

        var details = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        details.AddChild(_thumb);
        details.AddChild(_thumbStatus);
        details.AddChild(_details);
        details.AddChild(Row(_openButton, _approveButton, _hideButton, _pinButton, _deleteButton, _spawnButton));
        details.AddChild(Row(_rejectReason, _rejectButton));
        details.AddChild(Row(_banMinutes, _banReason, _banButton, _unbanButton));

        var detailsScroll = new ScrollContainer
        {
            HScrollEnabled = false,
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { details },
        };

        var body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            VerticalExpand = true,
        };
        body.AddChild(_list);
        body.AddChild(detailsScroll);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };
        root.AddChild(top);
        root.AddChild(body);
        Contents.AddChild(root);

        _view.ThumbnailChanged += OnThumbnailChanged;
        OnClose += () => _view.ThumbnailChanged -= OnThumbnailChanged;

        Select(null);
    }

    public event Action<EuiMessageBase>? MessageRequested;

    public void UpdateState(PaintingQueueEuiState state)
    {
        _state = state;
        _filter.SelectId((int) state.Filter);
        _status.Text = state.Status;
        _storage.Text = state.ApprovedLimitBytes > 0
            ? Loc.GetString("adt-canvas-queue-storage-limit",
                ("used", state.ApprovedTotalBytes / (1024 * 1024)),
                ("limit", state.ApprovedLimitBytes / (1024 * 1024)))
            : Loc.GetString("adt-canvas-queue-storage", ("used", state.ApprovedTotalBytes / (1024 * 1024)));

        var selectedUid = _selected?.PaintingUid;
        PaintingQueueEntry? reselect = null;

        _list.Clear();
        foreach (var entry in state.Entries)
        {
            var date = (entry.SubmittedAt ?? entry.ReviewedAt)?.ToLocalTime().ToString("dd.MM HH:mm") ?? "-";
            var text = Loc.GetString("adt-canvas-queue-entry",
                ("title", entry.Title),
                ("owner", entry.OwnerName),
                ("date", date));

            var item = _list.AddItem(text, metadata: entry);
            if (entry.PaintingUid == selectedUid)
            {
                item.Selected = true;
                reselect = entry;
            }
        }

        Select(reselect);
    }

    private void Select(PaintingQueueEntry? entry)
    {
        _selected = entry;
        _deleteArmed = false;
        _deleteButton.Text = Loc.GetString("adt-canvas-queue-delete");

        var state = _state;
        var has = entry != null && state != null;
        _openButton.Disabled = !has;
        _approveButton.Disabled = !has || !state!.CanModerate || entry!.Status != PaintingStatus.Submitted;
        _rejectButton.Disabled = !has || !state!.CanModerate || entry!.Status != PaintingStatus.Submitted;
        _hideButton.Disabled = !has || !state!.CanModerate;
        _pinButton.Disabled = !has || !state!.CanModerate || entry!.Status is not (PaintingStatus.Approved or PaintingStatus.Hidden);
        _deleteButton.Disabled = !has || !state!.CanDelete;
        _spawnButton.Disabled = !has || !state!.CanSpawn || entry!.Status != PaintingStatus.Approved;
        _banButton.Disabled = !has || !state!.CanBan;
        _unbanButton.Disabled = !has || !state!.CanBan || !entry!.OwnerBanned;

        if (entry == null)
        {
            _details.Text = Loc.GetString("adt-canvas-queue-empty");
            _thumb.Texture = null;
            _thumbStatus.Text = string.Empty;
            return;
        }

        _hideButton.Text = Loc.GetString(entry.Status == PaintingStatus.Hidden ? "adt-canvas-queue-unhide" : "adt-canvas-queue-hide");
        _pinButton.Text = Loc.GetString(entry.Pinned ? "adt-canvas-queue-unpin" : "adt-canvas-queue-pin");

        _details.Text = Loc.GetString("adt-canvas-queue-details",
            ("title", entry.Title),
            ("width", entry.Width),
            ("height", entry.Height),
            ("status", Loc.GetString($"adt-painting-status-{entry.Status.ToString().ToLowerInvariant()}")),
            ("owner", entry.OwnerName),
            ("approved", entry.OwnerApproved),
            ("rejected", entry.OwnerRejected),
            ("banned", entry.OwnerBanned ? Loc.GetString("adt-canvas-queue-banned") : string.Empty),
            ("contributors", string.Join(", ", entry.Contributors)),
            ("round", entry.RoundId?.ToString() ?? "-"),
            ("submitted", entry.SubmittedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "-"),
            ("reviewed", entry.ReviewedAt?.ToLocalTime().ToString("dd.MM.yyyy HH:mm") ?? "-"),
            ("reviewer", entry.ReviewerName ?? "-"),
            ("comment", entry.ReviewComment ?? "-"),
            ("size", entry.Bytes / 1024),
            ("description", string.IsNullOrWhiteSpace(entry.Description) ? "-" : entry.Description));

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

    private void OnDeletePressed()
    {
        if (_selected == null)
            return;

        if (!_deleteArmed)
        {
            _deleteArmed = true;
            _deleteButton.Text = Loc.GetString("adt-canvas-queue-delete-confirm");
            return;
        }

        Send(e => new PaintingQueueEuiMsg.Delete
        {
            Painting = e.PaintingUid,
        });
    }

    private void Send(Func<PaintingQueueEntry, EuiMessageBase> factory)
    {
        if (_selected != null)
            MessageRequested?.Invoke(factory(_selected));
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
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var child in children)
        {
            row.AddChild(child);
        }

        return row;
    }
}
