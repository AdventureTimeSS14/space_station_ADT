using System.Numerics;
using Content.Shared.ADT.ClockGreeting;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.ADT.ClockGreeting;

/// <summary>
/// Показывает приветствие с датой и временем смены в правом нижнем углу, через время скрывает.
/// </summary>
public sealed class ClockGreetingController : UIController
{
    private const float ShowTime = 6f;
    private const float Padding = 40f;

    private ClockGreetingUI? _ui;
    private float _timer;

    public override void Initialize()
    {
        UIManager.OnScreenChanged += OnScreenChanged;
        SubscribeNetworkEvent<ClockGreetingMessage>(OnGreeting);
    }

    private void OnGreeting(ClockGreetingMessage msg, EntitySessionEventArgs args)
    {
        var screen = UIManager.ActiveScreen;
        if (screen == null)
            return;

        var date = Loc.GetString("clock-greeting-date",
            ("day", msg.Day),
            ("month", Loc.GetString($"clock-greeting-month-{msg.Month}")),
            ("year", msg.Year));
        var earthTime = Loc.GetString("clock-greeting-earth-time", ("time", $"{msg.Hour:D2}:{msg.Minute:D2}"));
        var shift = Loc.GetString("clock-greeting-shift", ("time", $"{msg.ShiftHours:D2}:{msg.ShiftMinutes:D2}"));

        _ui ??= screen.GetOrAddWidget<ClockGreetingUI>();
        _ui.SetText($"{date}\n{earthTime}\n{shift}");
        _ui.Visible = true;
        _timer = 0;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_ui == null || !_ui.Visible)
            return;

        _timer += args.DeltaSeconds;
        if (_timer >= ShowTime)
        {
            _ui.Visible = false;
            return;
        }

        var screen = UIManager.ActiveScreen;
        if (screen == null)
            return;

        var pos = new Vector2(
            screen.Size.X - _ui.DesiredSize.X - Padding,
            screen.Size.Y - _ui.DesiredSize.Y - Padding);
        LayoutContainer.SetPosition(_ui, pos);
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        if (ev.Old != null && _ui != null)
        {
            ev.Old.RemoveWidget<ClockGreetingUI>();
            _ui = null;
        }
    }
}
