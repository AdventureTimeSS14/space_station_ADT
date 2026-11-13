using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Sync;
using Content.Client.ADT.Drawing.UI;
using Content.Shared.ADT.Drawing;
using Robust.Client.Player;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasEaselBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly CanvasSyncSystem _sync;

    private CanvasEaselWindow? _window;
    private DrawingWindow? _drawing;
    private CanvasSessionPanel? _panel;
    private CanvasSyncSession? _session;
    private CanvasSubmitWindow? _submit;
    private CanvasEaselBoundUserInterfaceState? _state;
    private CanvasDocument? _submitDocument;
    private (string Title, string Description)? _pendingSubmit;
    private bool _disposing;

    public CanvasEaselBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sync = EntMan.System<CanvasSyncSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = new CanvasEaselWindow();
        _window.OnStart += draft => SendMessage(new CanvasEaselStartMessage(draft));
        _window.OnCloseSession += () => SendMessage(new CanvasEaselCloseSessionMessage());
        _window.OnWithdraw += painting => SendMessage(new CanvasEaselWithdrawMessage(painting));
        _window.OnContinueAsNew += painting => SendMessage(new CanvasEaselStartMessage(null, painting));
        _window.OnClose += OnStartWindowClosed;
        _window.OpenCentered();

        _sync.SessionStarted += OnSessionStarted;
        _sync.SessionUpdated += OnSessionUpdated;
        _sync.SessionClosed += OnSessionClosed;

        if (_sync.Active is { Detached: false } active)
            OnSessionStarted(active);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not CanvasEaselBoundUserInterfaceState easel)
            return;

        _state = easel;
        _window?.UpdateState(easel, _player.LocalSession?.UserId);

        if (easel.Painting is { } painting)
            _sync.SetParticipants(painting, easel.Participants, _player.LocalSession?.UserId);

        RefreshSession();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is CanvasEaselDraftsMessage drafts)
            _window?.SetDrafts(drafts.Drafts, drafts.Pending, drafts.Approved, drafts.Slots);
    }

    private void OnSessionStarted(CanvasSyncSession session)
    {
        if (_disposing)
            return;

        if (_drawing != null && _session != session)
            CloseDrawing();

        _session = session;
        if (_drawing == null)
        {
            _drawing = new DrawingWindow(session.Document, false);
            _drawing.FinishPressed += () => SendMessage(new CanvasEaselCloseSessionMessage());
            _drawing.SubmitPressed += OpenSubmit;
            _drawing.OnClose += OnDrawingClosed;
            _drawing.Canvas.Collaboration = session.Collaboration;

            if (_drawing.SessionPanel != null)
            {
                _panel = CreatePanel();
                _drawing.SessionPanel.AddChild(_panel);
            }

            _drawing.OpenCentered();
            EntMan.System<CanvasRulesSystem>().ShowIfNeeded();
        }

        if (_window != null)
            _window.Visible = false;

        RefreshSession();
    }

    private CanvasSessionPanel CreatePanel()
    {
        var panel = new CanvasSessionPanel();
        panel.JoinRequested += () => SendMessage(new CanvasEaselJoinRequestMessage());
        panel.JoinCancelled += () => SendMessage(new CanvasEaselJoinCancelMessage());
        panel.JoinDecided += (user, accept) => SendMessage(new CanvasEaselJoinDecisionMessage(user, accept));
        panel.InviteRequested += user => SendMessage(new CanvasEaselInviteMessage(user));
        panel.InviteAnswered += accept => SendMessage(new CanvasEaselInviteResponseMessage(accept));
        panel.KickRequested += user => SendMessage(new CanvasEaselKickMessage(user));
        panel.HostTransferRequested += user => SendMessage(new CanvasEaselTransferHostMessage(user));
        panel.RevertRequested += () => SendMessage(new CanvasEaselRevertMessage());
        panel.LeaveRequested += OnLeavePressed;
        return panel;
    }

    private void OnLeavePressed()
    {
        if (_session != null && _session.Document.HasPendingWork)
        {
            _panel?.ShowHint(Loc.GetString("adt-canvas-session-leave-wait"));
            return;
        }

        _panel?.ShowHint(null);
        SendMessage(new CanvasEaselLeaveMessage());
    }

    private void OpenSubmit()
    {
        if (_submit != null)
        {
            _submit.MoveToFront();
            return;
        }

        _submit = new CanvasSubmitWindow();
        _submit.Submitted += OnSubmitted;
        _submit.OnClose += () => _submit = null;
        _submit.OpenCentered();
    }

    private void OnSubmitted(string title, string description)
    {
        if (_session is not { } session)
            return;

        var document = session.Document;
        if (!document.MergeVisibleLayers())
        {
            _panel?.ShowHint(Loc.GetString("adt-canvas-submit-merge-failed"));
            return;
        }

        if (!document.HasPendingWork)
        {
            SendMessage(new CanvasEaselSubmitMessage(title, description));
            return;
        }

        ClearPendingSubmit();
        _pendingSubmit = (title, description);
        _submitDocument = document;
        document.WorkSettled += OnWorkSettled;
        _panel?.ShowHint(Loc.GetString("adt-canvas-submit-waiting"));
    }

    private void OnWorkSettled()
    {
        var pending = _pendingSubmit;
        var document = _submitDocument;
        ClearPendingSubmit();

        if (pending is not { } submit || document == null || _session?.Document != document)
            return;

        _panel?.ShowHint(null);
        SendMessage(new CanvasEaselSubmitMessage(submit.Title, submit.Description));
    }

    private void ClearPendingSubmit()
    {
        if (_submitDocument != null)
            _submitDocument.WorkSettled -= OnWorkSettled;

        _submitDocument = null;
        _pendingSubmit = null;
    }

    private void OnSessionUpdated(CanvasSyncSession session)
    {
        if (session == _session)
            RefreshSession();
    }

    private void RefreshSession()
    {
        if (_drawing == null || _session is not { } session)
            return;

        var state = _state is { } s && s.Painting == session.Painting ? s : null;
        var role = session.Collaboration.LocalRole;

        if (state != null)
            _panel?.Refresh(state, _player.LocalSession?.UserId);

        _drawing.SetHost(role == CanvasRole.Host);

        if (session.Error != null)
        {
            _drawing.SetSessionStatus(Loc.GetString(session.Error), true);
            return;
        }

        if (!session.Loaded)
        {
            var text = Loc.GetString("adt-canvas-sync-loading",
                ("loaded", Math.Min(session.LoadedBlobs, session.TotalBlobs)),
                ("total", session.TotalBlobs));
            _drawing.SetSessionStatus(text, true);
            return;
        }

        if (role == CanvasRole.Viewer)
        {
            _drawing.SetSessionStatus(Loc.GetString("adt-canvas-session-viewer", ("name", state?.HostName ?? "?")), false, true);
            return;
        }

        _drawing.SetSessionStatus(null, false);
    }

    private void OnSessionClosed(CanvasSyncSession session, CanvasCloseReason reason)
    {
        if (session != _session)
            return;

        CloseDrawing();

        if (_window != null && !_disposing)
            _window.Visible = true;
    }

    private void OnDrawingClosed()
    {
        if (_disposing || _drawing == null)
            return;

        if (_session != null)
            _sync.Detach(_session.Painting);

        ClearPendingSubmit();
        _drawing = null;
        _panel = null;
        _session = null;
        Close();
    }

    private void OnStartWindowClosed()
    {
        if (_disposing || _drawing != null)
            return;

        Close();
    }

    private void CloseDrawing()
    {
        ClearPendingSubmit();

        var drawing = _drawing;
        _drawing = null;
        _panel = null;
        _session = null;

        if (drawing == null)
            return;

        drawing.OnClose -= OnDrawingClosed;
        drawing.Close();
        drawing.Canvas.Collaboration = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _disposing = true;
        _sync.SessionStarted -= OnSessionStarted;
        _sync.SessionUpdated -= OnSessionUpdated;
        _sync.SessionClosed -= OnSessionClosed;

        if (_session != null)
            _sync.Detach(_session.Painting);

        CloseDrawing();
        _submit?.Close();
        _window?.Close();
        _window = null;
    }
}
