using System.Numerics;
using Content.Shared.ADT.ClockGreeting;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.ADT.ClockGreeting;

public sealed class ClockGreetingController : UIController
{
    private const float WaitTime = 10f;
    private const float TypeInterval = 0.075f;
    private const float HoldTime = 2.5f;
    private const float DeleteInterval = 0.04f;
    private const float Padding = 40f;

    private readonly SoundSpecifier _tickSound = new SoundPathSpecifier("/Audio/ADT/Effects/textwriter.ogg");
    private readonly AudioParams _typeParams = AudioParams.Default.AddVolume(-6f).WithVariation(0.1f);
    private readonly AudioParams _deleteParams = AudioParams.Default.AddVolume(-10f).WithVariation(0.1f);

    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private ClockGreetingUI? _ui;
    private string _text = string.Empty;
    private int _visibleChars;
    private float _timer;
    private GreetingPhase _phase = GreetingPhase.Hidden;

    private enum GreetingPhase
    {
        Hidden,
        Waiting,
        Typing,
        Holding,
        Deleting,
    }

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
            ("day", msg.Date.Day),
            ("month", Loc.GetString($"clock-greeting-month-{msg.Date.Month}")),
            ("year", msg.Date.Year));
        var earthTime = Loc.GetString("clock-greeting-earth-time", ("time", $"{msg.Date.Hour:D2}:{msg.Date.Minute:D2}"));
        var shift = Loc.GetString("clock-greeting-shift", ("time", $"{(int)msg.Shift.TotalHours:D2}:{msg.Shift.Minutes:D2}"));

        _ui ??= screen.GetOrAddWidget<ClockGreetingUI>();
        _text = $"{date}\n{earthTime}\n{shift}";
        _visibleChars = 0;
        _timer = 0;
        _phase = GreetingPhase.Waiting;
        _ui.MinSize = Vector2.Zero;
        _ui.SetText(string.Empty);
        _ui.Visible = false;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_ui == null || _phase == GreetingPhase.Hidden)
            return;

        _timer += args.DeltaSeconds;

        if (_phase == GreetingPhase.Waiting)
        {
            if (_timer >= WaitTime)
            {
                _timer = 0;
                _phase = GreetingPhase.Typing;
                _ui.Visible = true;
            }
            return;
        }

        var screen = UIManager.ActiveScreen;
        if (screen == null)
            return;

        if (_phase != GreetingPhase.Deleting)
        {
            var pos = new Vector2(
                screen.Size.X - _ui.DesiredSize.X - Padding,
                screen.Size.Y - _ui.DesiredSize.Y - Padding);
            LayoutContainer.SetPosition(_ui, pos);
        }

        switch (_phase)
        {
            case GreetingPhase.Typing:
                while (_timer >= TypeInterval && _visibleChars < _text.Length)
                {
                    _timer -= TypeInterval;
                    var next = _text[_visibleChars++];
                    _ui.SetText(_text[.._visibleChars]);
                    if (next != '\n')
                        _audio.PlayGlobal(_tickSound, EntityUid.Invalid, _typeParams);
                }

                if (_visibleChars >= _text.Length)
                {
                    _timer = 0;
                    _phase = GreetingPhase.Holding;
                }
                break;

            case GreetingPhase.Holding:
                if (_timer >= HoldTime)
                {
                    _timer = 0;
                    _phase = GreetingPhase.Deleting;
                    _ui.MinSize = _ui.DesiredSize;
                }
                break;

            case GreetingPhase.Deleting:
                while (_timer >= DeleteInterval && _visibleChars > 0)
                {
                    _timer -= DeleteInterval;
                    _visibleChars--;
                    _ui.SetText(_text[.._visibleChars]);
                    _audio.PlayGlobal(_tickSound, EntityUid.Invalid, _deleteParams);
                }

                if (_visibleChars == 0)
                {
                    _ui.Visible = false;
                    _phase = GreetingPhase.Hidden;
                }
                break;
        }
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        if (ev.Old != null && _ui != null)
        {
            ev.Old.RemoveWidget<ClockGreetingUI>();
            _ui = null;
            _phase = GreetingPhase.Hidden;
        }
    }
}
