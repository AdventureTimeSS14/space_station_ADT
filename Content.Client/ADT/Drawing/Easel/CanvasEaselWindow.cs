using System.Numerics;
using Content.Shared.ADT.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasEaselWindow : DefaultWindow
{
    private readonly Label _canvasLabel;
    private readonly RichTextLabel _sessionLabel;
    private readonly Button _startButton;
    private readonly Button _closeSessionButton;
    private readonly Label _draftsHeader;
    private readonly RichTextLabel _draftsHint;
    private readonly BoxContainer _draftsList;
    private readonly Label _pendingHeader;
    private readonly BoxContainer _pendingList;
    private readonly Label _approvedHeader;
    private readonly BoxContainer _approvedList;

    private CanvasEaselBoundUserInterfaceState? _state;
    private NetUserId? _localUser;
    private List<CanvasDraftInfo> _drafts = new();
    private List<CanvasDraftInfo> _pending = new();
    private List<CanvasDraftInfo> _approved = new();
    private int _slots;

    public CanvasEaselWindow()
    {
        Title = Loc.GetString("adt-canvas-easel-title");
        MinSize = new Vector2(380, 260);
        SetSize = new Vector2(420, 360);

        _canvasLabel = new Label();
        _sessionLabel = new RichTextLabel();

        _startButton = new Button
        {
            HorizontalExpand = true,
        };
        _startButton.OnPressed += _ => OnStart?.Invoke(null);

        _closeSessionButton = new Button
        {
            Text = Loc.GetString("adt-canvas-easel-finish"),
            HorizontalExpand = true,
        };
        _closeSessionButton.OnPressed += _ => OnCloseSession?.Invoke();

        _draftsHeader = new Label
        {
            StyleClasses = { "LabelHeading" },
        };
        _draftsHint = new RichTextLabel();
        _draftsList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };
        _pendingHeader = new Label
        {
            Text = Loc.GetString("adt-canvas-easel-pending"),
            StyleClasses = { "LabelHeading" },
        };
        _pendingList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        _approvedHeader = new Label
        {
            Text = Loc.GetString("adt-canvas-easel-approved"),
            StyleClasses = { "LabelHeading" },
        };
        _approvedList = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var lists = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };
        lists.AddChild(_draftsList);
        lists.AddChild(_pendingHeader);
        lists.AddChild(_pendingList);
        lists.AddChild(_approvedHeader);
        lists.AddChild(_approvedList);

        var buttons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };
        buttons.AddChild(_startButton);
        buttons.AddChild(_closeSessionButton);

        var scroll = new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            Children = { lists },
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(4),
        };
        root.AddChild(_canvasLabel);
        root.AddChild(_sessionLabel);
        root.AddChild(buttons);
        root.AddChild(_draftsHeader);
        root.AddChild(_draftsHint);
        root.AddChild(scroll);
        Contents.AddChild(root);

        Refresh();
    }

    public event Action<Guid?>? OnStart;

    public event Action? OnCloseSession;

    public event Action<Guid>? OnWithdraw;

    public event Action<Guid>? OnContinueAsNew;

    private static void SetGray(RichTextLabel label, string text)
    {
        label.SetMessage(FormattedMessage.FromUnformatted(text), Color.Gray);
    }

    public void UpdateState(CanvasEaselBoundUserInterfaceState state, NetUserId? localUser)
    {
        _state = state;
        _localUser = localUser;
        Refresh();
    }

    public void SetDrafts(List<CanvasDraftInfo> drafts, List<CanvasDraftInfo> pending, List<CanvasDraftInfo> approved, int slots)
    {
        _drafts = drafts;
        _pending = pending;
        _approved = approved;
        _slots = slots;
        Refresh();
    }

    private void Refresh()
    {
        var state = _state;
        var hasCanvas = state is { HasCanvas: true };
        var mySession = state is { SessionActive: true } && state.Host == _localUser;
        var otherSession = state is { SessionActive: true } && state.Host != _localUser;
        var myCanvas = hasCanvas && state!.CanvasPainted && state.CanvasOwner == _localUser;
        var blankCanvas = hasCanvas && !state!.CanvasPainted;

        _canvasLabel.Text = hasCanvas
            ? Loc.GetString("adt-canvas-easel-canvas", ("width", state!.CanvasSize.X), ("height", state.CanvasSize.Y))
            : Loc.GetString("adt-canvas-easel-no-canvas");

        if (otherSession)
            SetGray(_sessionLabel, Loc.GetString("adt-canvas-easel-busy", ("name", state!.HostName ?? "?")));
        else if (mySession)
            SetGray(_sessionLabel, Loc.GetString("adt-canvas-easel-mine"));
        else if (hasCanvas && state!.CanvasPainted && !myCanvas)
            SetGray(_sessionLabel, Loc.GetString("adt-canvas-easel-foreign"));
        else
            SetGray(_sessionLabel, string.Empty);

        _startButton.Text = mySession || myCanvas
            ? Loc.GetString("adt-canvas-easel-continue")
            : Loc.GetString("adt-canvas-easel-start");
        _startButton.Disabled = otherSession || !(mySession || myCanvas || blankCanvas);
        _closeSessionButton.Visible = mySession;

        _draftsHeader.Text = Loc.GetString("adt-canvas-easel-drafts", ("count", _drafts.Count), ("slots", _slots));
        SetGray(_draftsHint, _slots > 0 && _drafts.Count >= _slots
            ? Loc.GetString("adt-canvas-easel-drafts-full")
            : Loc.GetString("adt-canvas-easel-drafts-hint"));

        _draftsList.RemoveAllChildren();
        foreach (var draft in _drafts)
        {
            var sizeMatches = hasCanvas && state!.CanvasSize == new Vector2i(draft.Width, draft.Height);
            var title = string.IsNullOrWhiteSpace(draft.Title)
                ? Loc.GetString("adt-canvas-easel-untitled")
                : draft.Title;

            var label = new Label
            {
                Text = Loc.GetString("adt-canvas-easel-draft",
                    ("title", title),
                    ("width", draft.Width),
                    ("height", draft.Height),
                    ("date", draft.UpdatedAt.ToLocalTime().ToString("dd.MM HH:mm"))),
                HorizontalExpand = true,
                ClipText = true,
            };

            var uid = draft.PaintingUid;
            var button = new Button
            {
                Text = Loc.GetString("adt-canvas-easel-draft-open"),
                Disabled = !blankCanvas || !sizeMatches || state!.SessionActive,
                ToolTip = sizeMatches ? null : Loc.GetString("adt-canvas-easel-draft-size"),
            };
            button.OnPressed += _ => OnStart?.Invoke(uid);

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 4,
            };
            row.AddChild(label);
            row.AddChild(button);
            _draftsList.AddChild(row);
        }

        _pendingHeader.Visible = _pending.Count > 0;
        _pendingList.RemoveAllChildren();
        foreach (var pending in _pending)
        {
            var title = string.IsNullOrWhiteSpace(pending.Title)
                ? Loc.GetString("adt-canvas-easel-untitled")
                : pending.Title;

            var label = new Label
            {
                Text = Loc.GetString("adt-canvas-easel-draft",
                    ("title", title),
                    ("width", pending.Width),
                    ("height", pending.Height),
                    ("date", pending.UpdatedAt.ToLocalTime().ToString("dd.MM HH:mm"))),
                HorizontalExpand = true,
                ClipText = true,
            };

            var uid = pending.PaintingUid;
            var button = new Button
            {
                Text = Loc.GetString("adt-canvas-easel-withdraw"),
            };
            button.OnPressed += _ => OnWithdraw?.Invoke(uid);

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 4,
            };
            row.AddChild(label);
            row.AddChild(button);
            _pendingList.AddChild(row);
        }

        _approvedHeader.Visible = _approved.Count > 0;
        _approvedList.RemoveAllChildren();
        foreach (var approved in _approved)
        {
            var title = string.IsNullOrWhiteSpace(approved.Title)
                ? Loc.GetString("adt-canvas-easel-untitled")
                : approved.Title;

            var label = new Label
            {
                Text = Loc.GetString("adt-canvas-easel-draft",
                    ("title", title),
                    ("width", approved.Width),
                    ("height", approved.Height),
                    ("date", approved.UpdatedAt.ToLocalTime().ToString("dd.MM HH:mm"))),
                HorizontalExpand = true,
                ClipText = true,
            };

            var sizeMatches = hasCanvas && state!.CanvasSize == new Vector2i(approved.Width, approved.Height);
            var uid = approved.PaintingUid;
            var button = new Button
            {
                Text = Loc.GetString("adt-canvas-easel-continue-new"),
                Disabled = !blankCanvas || !sizeMatches || state!.SessionActive,
                ToolTip = sizeMatches ? null : Loc.GetString("adt-canvas-easel-draft-size"),
            };
            button.OnPressed += _ => OnContinueAsNew?.Invoke(uid);

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 4,
            };
            row.AddChild(label);
            row.AddChild(button);
            _approvedList.AddChild(row);
        }
    }
}
