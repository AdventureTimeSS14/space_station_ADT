using System.Linq;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Polls;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Polls.UI;

public sealed class PollPanelWindow : DefaultWindow
{
    private readonly TabContainer _tabs;
    private readonly ItemList _list;
    private readonly Button _new;
    private readonly Button _edit;
    private readonly Button _stats;
    private readonly Button _toggleClosed;
    private readonly ConfirmButton _wipe;
    private readonly ConfirmButton _delete;
    private readonly Button _refresh;
    private readonly Label _details;
    private readonly PollStatsView _statsView;
    private readonly Label _statsEmpty;
    private readonly PollEditor _editor;
    private readonly Button _save;
    private readonly Label _status;
    private readonly Label _noPermission;

    private List<PollAdminEntry> _entries = new();
    private string _serverName = string.Empty;
    private int? _selectedId;

    public event Action? RefreshRequested;
    public event Action<PollData>? SaveRequested;
    public event Action<int, bool>? SetClosedRequested;
    public event Action<int>? DeleteRequested;
    public event Action<int>? WipeRequested;
    public event Action<int, PollGroupBy, int>? StatsRequested;

    public PollPanelWindow(IPrototypeManager proto)
    {
        Title = Loc.GetString("adt-poll-panel-title");
        MinSize = new Vector2(900, 620);
        SetSize = new Vector2(1100, 760);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        _noPermission = new Label
        {
            Text = Loc.GetString("adt-poll-panel-no-permission"),
            FontColorOverride = Color.OrangeRed,
            Visible = false,
        };

        root.AddChild(_noPermission);

        _tabs = new TabContainer
        {
            VerticalExpand = true,
        };

        var pollsTab = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            Margin = new Thickness(6),
        };

        var left = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            MinWidth = 340,
            MaxWidth = 340,
        };

        _list = new ItemList
        {
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };

        left.AddChild(_list);

        _details = new Label
        {
            FontColorOverride = Color.Gray,
        };

        left.AddChild(_details);

        _new = new Button
        {
            Text = Loc.GetString("adt-poll-panel-new"),
        };

        _edit = new Button
        {
            Text = Loc.GetString("adt-poll-panel-edit"),
        };

        _stats = new Button
        {
            Text = Loc.GetString("adt-poll-panel-stats"),
        };

        _toggleClosed = new Button
        {
            Text = Loc.GetString("adt-poll-panel-close"),
        };

        _wipe = new ConfirmButton
        {
            Text = Loc.GetString("adt-poll-panel-wipe"),
            ConfirmationText = Loc.GetString("adt-poll-panel-confirm"),
        };

        _delete = new ConfirmButton
        {
            Text = Loc.GetString("adt-poll-panel-delete"),
            ConfirmationText = Loc.GetString("adt-poll-panel-confirm"),
        };

        _refresh = new Button
        {
            Text = Loc.GetString("adt-poll-panel-refresh"),
        };

        left.AddChild(ButtonRow(_new, _edit, _stats));
        left.AddChild(ButtonRow(_toggleClosed, _refresh));
        left.AddChild(ButtonRow(_wipe, _delete));

        pollsTab.AddChild(left);

        var right = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        _statsEmpty = new Label
        {
            Text = Loc.GetString("adt-poll-panel-stats-empty"),
            FontColorOverride = Color.Gray,
        };

        _statsView = new PollStatsView(proto)
        {
            Visible = false,
        };

        var statsBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(0, 0, 8, 0),
        };

        statsBox.AddChild(_statsEmpty);
        statsBox.AddChild(_statsView);

        right.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { statsBox },
        });

        pollsTab.AddChild(right);

        _tabs.AddChild(pollsTab);
        _tabs.SetTabTitle(0, Loc.GetString("adt-poll-panel-tab-polls"));

        var editorTab = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        _editor = new PollEditor(proto);

        var editorBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(0, 0, 8, 0),
        };

        editorBox.AddChild(_editor);

        editorTab.AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { editorBox },
        });

        _save = new Button
        {
            Text = Loc.GetString("adt-poll-panel-save"),
            StyleClasses = { "Caution" },
            HorizontalAlignment = HAlignment.Right,
            Margin = new Thickness(6),
        };

        editorTab.AddChild(_save);

        _tabs.AddChild(editorTab);
        _tabs.SetTabTitle(1, Loc.GetString("adt-poll-panel-tab-editor"));

        root.AddChild(_tabs);

        _status = new Label
        {
            Margin = new Thickness(6, 0),
        };

        root.AddChild(_status);

        Contents.AddChild(root);

        _list.OnItemSelected += _ => OnSelectionChanged();
        _list.OnItemDeselected += _ => OnSelectionChanged();

        _new.OnPressed += _ =>
        {
            _editor.Load(null, 0, _serverName);
            _tabs.CurrentTab = 1;
        };

        _edit.OnPressed += _ =>
        {
            if (GetSelected() is not { } entry)
                return;

            _editor.Load(entry.Poll, entry.Responses, _serverName);
            _tabs.CurrentTab = 1;
        };

        _stats.OnPressed += _ =>
        {
            if (GetSelected() is { } entry)
                StatsRequested?.Invoke(entry.Poll.Id, PollGroupBy.None, 0);
        };

        _toggleClosed.OnPressed += _ =>
        {
            if (GetSelected() is { } entry)
                SetClosedRequested?.Invoke(entry.Poll.Id, !entry.Poll.Closed);
        };

        _wipe.OnPressed += _ =>
        {
            if (GetSelected() is { } entry)
                WipeRequested?.Invoke(entry.Poll.Id);
        };

        _delete.OnPressed += _ =>
        {
            if (GetSelected() is { } entry)
                DeleteRequested?.Invoke(entry.Poll.Id);
        };

        _refresh.OnPressed += _ => RefreshRequested?.Invoke();
        _save.OnPressed += _ => SaveRequested?.Invoke(_editor.Build());

        _statsView.StatsRequested += (groupBy, page) =>
        {
            if (_statsView.PollId is { } id)
                StatsRequested?.Invoke(id, groupBy, page);
        };

        OnSelectionChanged();
    }

    public void SetState(PollPanelEuiState state)
    {
        _noPermission.Visible = !state.HasPermission;
        _tabs.Visible = state.HasPermission;
        _serverName = state.ServerName;
        _status.Text = state.Status.Length > 0 ? Loc.GetString(state.Status) : string.Empty;

        _entries = state.Polls;
        RebuildList();

        var statsPoll = state.Stats == null
            ? null
            : _entries.FirstOrDefault(e => e.Poll.Id == state.Stats.PollId);

        if (state.Stats != null && statsPoll != null)
        {
            _statsView.SetStats(statsPoll.Poll, state.Stats);
            _statsView.Visible = true;
            _statsEmpty.Visible = false;
        }
        else
        {
            _statsView.Visible = false;
            _statsEmpty.Visible = true;
        }
    }

    private void RebuildList()
    {
        _list.Clear();

        var now = DateTime.UtcNow;

        foreach (var entry in _entries)
        {
            var state = entry.Poll.IsActive(now)
                ? Loc.GetString("adt-poll-panel-state-active")
                : Loc.GetString("adt-poll-panel-state-closed");

            var item = _list.AddItem($"[{entry.Poll.Id}] {entry.Poll.Title} - {state} - {entry.Responses}");
            item.Metadata = entry.Poll.Id;

            if (entry.Poll.Id == _selectedId)
                item.Selected = true;
        }

        OnSelectionChanged();
    }

    private PollAdminEntry? GetSelected()
    {
        var selected = _list.GetSelected().FirstOrDefault();

        if (selected?.Metadata is not int id)
            return null;

        return _entries.FirstOrDefault(e => e.Poll.Id == id);
    }

    private void OnSelectionChanged()
    {
        var entry = GetSelected();
        _selectedId = entry?.Poll.Id;

        var hasSelection = entry != null;
        _edit.Disabled = !hasSelection;
        _stats.Disabled = !hasSelection;
        _toggleClosed.Disabled = !hasSelection;
        _wipe.Disabled = !hasSelection;
        _delete.Disabled = !hasSelection;

        if (entry == null)
        {
            _details.Text = string.Empty;
            _toggleClosed.Text = Loc.GetString("adt-poll-panel-close");
            return;
        }

        var poll = entry.Poll;

        _toggleClosed.Text = poll.Closed
            ? Loc.GetString("adt-poll-panel-open")
            : Loc.GetString("adt-poll-panel-close");

        var ends = poll.EndsAt is { } endsAt
            ? endsAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : Loc.GetString("adt-poll-panel-no-end");

        var servers = poll.Servers.Count == 0
            ? Loc.GetString("adt-poll-panel-all-servers")
            : string.Join(", ", poll.Servers);

        _details.Text = Loc.GetString("adt-poll-panel-details",
            ("mode", PollNames.ShowMode(poll.ShowMode)),
            ("ends", ends),
            ("servers", servers),
            ("responses", entry.Responses),
            ("pending", entry.Pending));
    }

    private static BoxContainer ButtonRow(params Button[] buttons)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var button in buttons)
        {
            button.HorizontalExpand = true;
            row.AddChild(button);
        }

        return row;
    }
}
