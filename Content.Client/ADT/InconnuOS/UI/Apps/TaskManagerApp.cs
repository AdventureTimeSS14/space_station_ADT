using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class TaskManagerApp : OsAppControl
{
    private readonly BoxContainer _tasks;

    private readonly OsInfoRow _machine;
    private readonly OsInfoRow _uptime;
    private readonly OsInfoRow _load;
    private readonly OsUsageBar _loadBar;

    private int _power;
    private int _budget;
    private bool _subscribed;

    public TaskManagerApp()
    {
        _machine = new OsInfoRow(Loc.GetString("os-tasks-machine"), string.Empty);
        _uptime = new OsInfoRow(Loc.GetString("os-tasks-uptime"), string.Empty);
        _load = new OsInfoRow(Loc.GetString("os-tasks-load"), string.Empty);

        _loadBar = new OsUsageBar(Color.FromHex("#3f8fd0"))
        {
            HorizontalExpand = true,
            Margin = new Thickness(10f, 2f, 10f, 6f),
        };

        _tasks = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        var reboot = OsWidgets.Small(Loc.GetString("os-tasks-reboot"));
        var shutdown = OsWidgets.Small(Loc.GetString("os-tasks-shutdown"));

        reboot.OnPressed += _ => Context.Send(new ADTOsPowerMessage(OsPowerAction.Reboot));
        shutdown.OnPressed += _ => Context.Send(new ADTOsPowerMessage(OsPowerAction.Shutdown));

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children =
            {
                OsWidgets.Caption(Loc.GetString("os-tasks-system")),
                _machine,
                _uptime,
                _load,
                _loadBar,
                OsWidgets.Caption(Loc.GetString("os-tasks-windows")),
                new OsPanel
                {
                    VerticalExpand = true,
                    Margin = new Thickness(4f, 0f, 4f, 4f),
                    Children =
                    {
                        new ScrollContainer
                        {
                            HorizontalExpand = true,
                            VerticalExpand = true,
                            Margin = new Thickness(1f),
                            Children = { _tasks },
                        },
                    },
                },
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Margin = new Thickness(4f, 0f, 4f, 4f),
                    Children = { reboot, shutdown },
                },
            },
        });
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (!_subscribed)
        {
            _subscribed = true;

            Context.MessageReceived += OnServerMessage;
            Context.WindowsChanged += RefreshTasks;

            Context.Send(new ADTOsRequestCircuitMessage());
        }

        RefreshTasks();
        Refresh();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        Refresh();
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsCircuitStateMessage circuit)
            return;

        _power = circuit.State.PowerUsed;
        _budget = circuit.State.Limits.PowerBudget;

        Refresh();
    }

    private void RefreshTasks()
    {
        _tasks.RemoveAllChildren();

        foreach (var window in Context.Windows())
        {
            if (window.Closing)
                continue;

            var target = window;

            var state = window.Minimized
                ? Loc.GetString("os-tasks-minimized")
                : Loc.GetString("os-tasks-active");

            var row = new OsListRow(window.Proto.Icon, window.Title, state, Context.Accent);

            row.OnSelected += () => Context.FocusWindow(target);
            row.OnActivated += () => Context.CloseWindow(target);

            _tasks.AddChild(row);
        }
    }

    private void Refresh()
    {
        _machine.Value = Context.State.MachineName;
        _loadBar.Accent = Context.Accent;

        _load.Value = _budget <= 0
            ? Loc.GetString("os-tasks-load-none")
            : Loc.GetString("os-tasks-load-value", ("used", _power), ("total", _budget));

        _loadBar.Fraction = _budget <= 0 ? 0f : _power / (float) _budget;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _uptime.Value = OsFormat.Time(Context.Uptime);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !_subscribed)
            return;

        Context.MessageReceived -= OnServerMessage;
        Context.WindowsChanged -= RefreshTasks;
    }
}
