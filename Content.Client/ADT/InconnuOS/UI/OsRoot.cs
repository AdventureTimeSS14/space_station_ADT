using Content.Shared.ADT.InconnuOS;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public sealed class OsRoot : Control
{
    private readonly OsContext _context;
    private readonly OsWindowManager _manager;
    private readonly OsDesktop _desktop;
    private readonly OsBootScreen _boot;
    private readonly OsCrashScreen _crash;
    private readonly OsPowerScreen _power;

    private TimeSpan _bootedAt;
    private bool _crashShown;
    private bool _powering;

    public event Action<BoundUserInterfaceMessage>? OnMessage;

    public event Action? OnSessionReset;

    public OsRoot(
        IPrototypeManager prototypes,
        IResourceCache cache,
        IGameTiming timing,
        IEntityManager entities,
        OsAppRegistry registry,
        ADTOsBuiState state,
        OsSession? session)
    {
        _context = new OsContext(prototypes, cache, timing, entities, state);
        _manager = new OsWindowManager(registry, _context);

        _desktop = new OsDesktop(_context, _manager, timing);
        _boot = new OsBootScreen(cache, timing);
        _crash = new OsCrashScreen(cache);
        _power = new OsPowerScreen(cache);

        _context.Send = message => OnMessage?.Invoke(message);
        _context.Toast = text => _desktop.Toast(text);
        _context.ShowMenu = (screen, entries) => _desktop.ShowMenu(screen, entries);
        _context.OpenApp = OpenApp;
        _context.OpenFile = OpenFile;
        _context.Windows = () => _manager.Windows;
        _context.CloseWindow = window => _manager.Close(window);
        _context.FocusWindow = window => _manager.Toggle(window);

        _manager.OnWindowsChanged += _context.RaiseWindowsChanged;

        _desktop.OnPowerPicked += RequestPower;
        _crash.OnRebootPressed += () => OnMessage?.Invoke(new ADTOsPowerMessage(OsPowerAction.Reboot));
        _power.OnFinished += action => OnMessage?.Invoke(new ADTOsPowerMessage(action));

        UserInterfaceManager.OnKeyBindDown += OnAnyKeyBindDown;

        AddChild(_desktop);
        AddChild(_boot);
        AddChild(_power);
        AddChild(_crash);

        _bootedAt = state.BootedAt;
        _boot.SetState(state);

        _desktop.StateChanged();

        if (session != null)
            RestoreSession(session);

        UpdateScreens();
    }

    public void SetState(ADTOsBuiState state)
    {
        var rebooted = state.BootedAt != _bootedAt;

        _bootedAt = state.BootedAt;
        _context.SetState(state);
        _boot.SetState(state);

        if (rebooted)
        {
            _powering = false;
            _power.Visible = false;
            _manager.CloseAll();

            OnSessionReset?.Invoke();
        }

        _desktop.StateChanged();
        _manager.StateChanged();

        UpdateScreens();
    }

    public void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is ADTOsErrorMessage error)
        {
            _desktop.Toast(OsErrors.GetMessage(error.Error, error.Detail));
            return;
        }

        _context.Receive(message);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateScreens();
    }

    private void UpdateScreens()
    {
        var state = _context.State;

        if (state.Crashed)
        {
            if (!_crashShown)
            {
                _crashShown = true;
                _manager.CloseAll();
                _crash.Reset();
            }

            Show(desktop: false, boot: false, power: false, crash: true);
            return;
        }

        _crashShown = false;

        if (_powering)
        {
            Show(desktop: true, boot: false, power: true, crash: false);
            return;
        }

        var elapsed = (float) _context.Uptime.TotalSeconds;

        if (elapsed < OsBootTimeline.WelcomeEnd)
        {
            Show(desktop: false, boot: true, power: false, crash: false);
            return;
        }

        Show(desktop: true, boot: elapsed < OsBootTimeline.Total, power: false, crash: false);
    }

    private void Show(bool desktop, bool boot, bool power, bool crash)
    {
        _desktop.Visible = desktop;
        _boot.Visible = boot;
        _power.Visible = power;
        _crash.Visible = crash;
    }

    private void RequestPower(OsPowerAction action)
    {
        if (_powering)
            return;

        _powering = true;

        _desktop.CloseMenus();
        _manager.CloseAll();

        OnSessionReset?.Invoke();

        _power.Begin(action, _context.Accent);
        UpdateScreens();
    }

    private void OnAnyKeyBindDown(Control control)
    {
        for (var current = control; current != null; current = current.Parent)
        {
            if (current is not OsWindow window)
                continue;

            if (window.Parent == _manager.Layer)
                _manager.Focus(window);

            return;
        }
    }

    public OsSession SaveSession()
    {
        var session = new OsSession
        {
            BootedAt = _bootedAt,
        };

        foreach (var child in _manager.Layer.Children)
        {
            if (child is not OsWindow { Closing: false } window)
                continue;

            session.Windows.Add(new OsWindowSave
            {
                AppId = window.Proto.ID,
                Position = window.RestorePosition,
                Size = window.RestoreSize,
                Minimized = window.Minimized,
                Maximized = window.Maximized,
                State = window.App.SaveState(),
            });
        }

        return session;
    }

    private void RestoreSession(OsSession session)
    {
        var state = _context.State;

        if (session.BootedAt != state.BootedAt || state.Crashed)
            return;

        foreach (var saved in session.Windows)
        {
            if (!_context.Prototypes.TryIndex<ADTOsAppPrototype>(saved.AppId, out var proto))
                continue;

            if (!IsInstalled(proto.ID))
                continue;

            var window = _manager.Open(proto, null, false);

            window.WindowPosition = saved.Position;
            window.WindowSize = saved.Size;

            if (saved.Maximized)
                window.SetMaximizedInstant(true, saved.Position, saved.Size);

            if (saved.State != null)
                window.App.LoadState(saved.State);

            if (saved.Minimized)
                _manager.MinimizeInstant(window);
        }
    }

    private bool IsInstalled(string appId)
    {
        foreach (var id in _context.State.Apps)
        {
            if (id.Id == appId)
                return true;
        }

        return false;
    }

    private void OpenApp(string appId, string? argument)
    {
        if (!_context.Prototypes.TryIndex<ADTOsAppPrototype>(appId, out var app))
            return;

        _manager.Open(app, argument);
    }

    private void OpenFile(string path)
    {
        if (_context.FindHandler(path) is not { } app)
        {
            _desktop.Toast(Loc.GetString("os-file-no-handler", ("name", OsPath.GetName(path))));
            return;
        }

        _manager.Open(app, path);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            UserInterfaceManager.OnKeyBindDown -= OnAnyKeyBindDown;
    }
}
