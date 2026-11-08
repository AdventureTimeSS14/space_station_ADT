using Content.Shared.ADT.Cooking.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Cooking.UI;

public sealed class ADTStoveWindow : DefaultWindow
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const int Step = 15;

    public event Action<int>? OnTimerSelected;

    private readonly Label _status;
    private readonly GridContainer _presets;
    private EntityUid _stove;
    private int _shownSeconds = -1;

    public ADTStoveWindow()
    {
        IoCManager.InjectDependencies(this);

        MinSize = new(320, 0);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
        };

        root.AddChild(new Label
        {
            Text = Loc.GetString("adt-stove-timer-title"),
            StyleClasses = { "LabelHeading" },
        });

        _status = new Label();
        root.AddChild(_status);

        _presets = new GridContainer { Columns = 4 };
        root.AddChild(_presets);

        var controls = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        var minus = new Button { Text = Loc.GetString("adt-stove-timer-minus", ("seconds", Step)), HorizontalExpand = true };
        minus.OnPressed += _ => OnTimerSelected?.Invoke(Math.Max(0, Remaining() - Step));
        controls.AddChild(minus);

        var plus = new Button { Text = Loc.GetString("adt-stove-timer-plus", ("seconds", Step)), HorizontalExpand = true };
        plus.OnPressed += _ => OnTimerSelected?.Invoke(Remaining() + Step);
        controls.AddChild(plus);

        var stop = new Button { Text = Loc.GetString("adt-stove-timer-stop"), HorizontalExpand = true };
        stop.OnPressed += _ => OnTimerSelected?.Invoke(0);
        controls.AddChild(stop);

        root.AddChild(controls);
        Contents.AddChild(root);
    }

    public void Setup(EntityUid stove, ADTStoveComponent comp)
    {
        _stove = stove;
        _presets.RemoveAllChildren();

        foreach (var seconds in comp.OvenTimerPresets)
        {
            var button = new Button
            {
                Text = FormatTime(seconds),
                HorizontalExpand = true,
            };
            button.OnPressed += _ => OnTimerSelected?.Invoke(seconds);
            _presets.AddChild(button);
        }

        UpdateStatus();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateStatus();
    }

    private int Remaining()
    {
        if (!_entMan.TryGetComponent<ADTStoveComponent>(_stove, out var comp))
            return 0;

        if (comp.OvenTimerRemaining is { } paused)
            return Math.Max(0, (int) Math.Ceiling(paused.TotalSeconds));

        if (comp.OvenTimerEnd is not { } end)
            return 0;

        return Math.Max(0, (int) Math.Ceiling((end - _timing.CurTime).TotalSeconds));
    }

    private void UpdateStatus()
    {
        var seconds = Remaining();
        if (seconds == _shownSeconds)
            return;

        _shownSeconds = seconds;
        _status.Text = seconds > 0
            ? Loc.GetString("adt-stove-timer-remaining", ("time", FormatTime(seconds)))
            : Loc.GetString("adt-stove-timer-off");
    }

    private static string FormatTime(int seconds)
    {
        return $"{seconds / 60}:{seconds % 60:D2}";
    }
}
