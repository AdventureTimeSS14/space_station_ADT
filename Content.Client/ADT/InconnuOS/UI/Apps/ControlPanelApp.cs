using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.ADT.InconnuOS.UI.Apps;


public sealed class ControlPanelApp : OsAppControl
{
    private static readonly OsWallpaper[] Wallpapers =
    {
        OsWallpaper.Depths,
        OsWallpaper.Grid,
        OsWallpaper.Aurora,
        OsWallpaper.Circuitry,
        OsWallpaper.Plain,
    };

    private static readonly Color[] Accents =
    {
        Color.FromHex("#3f8fd0"),
        Color.FromHex("#4fb07a"),
        Color.FromHex("#c08a3e"),
        Color.FromHex("#b05a5a"),
        Color.FromHex("#8a6fc0"),
        Color.FromHex("#4fa8a8"),
    };

    private OptionButton _wallpaper = default!;
    private BoxContainer _accents = default!;
    private CheckBox _sounds = default!;
    private CheckBox _animations = default!;
    private CheckBox _clock = default!;
    private Slider _volume = default!;
    private Label _licence = default!;

    private bool _built;
    private bool _loading;

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (!_built)
            Build();

        Load();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        Load();
    }

    private void Build()
    {
        _built = true;

        _wallpaper = new OptionButton
        {
            MinWidth = 160f,
            Margin = new Thickness(10f, 0f, 10f, 4f),
        };

        foreach (var wallpaper in Wallpapers)
        {
            _wallpaper.AddItem(Loc.GetString($"os-wallpaper-{wallpaper.ToString().ToLowerInvariant()}"), (int) wallpaper);
        }

        _accents = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(10f, 0f, 10f, 4f),
        };

        foreach (var accent in Accents)
        {
            var swatch = new OsAccentSwatch(accent);
            var picked = accent;

            swatch.OnPressed += () => Change(settings => settings.Accent = picked);

            _accents.AddChild(swatch);
        }

        _sounds = new CheckBox
        {
            Text = Loc.GetString("os-settings-sounds"),
            Margin = new Thickness(10f, 0f, 10f, 0f),
        };

        _animations = new CheckBox
        {
            Text = Loc.GetString("os-settings-animations"),
            Margin = new Thickness(10f, 0f, 10f, 0f),
        };

        _clock = new CheckBox
        {
            Text = Loc.GetString("os-settings-clock"),
            Margin = new Thickness(10f, 0f, 10f, 0f),
        };

        _volume = new Slider
        {
            MinValue = 0f,
            MaxValue = 1f,
            Margin = new Thickness(10f, 0f, 10f, 6f),
            MinWidth = 180f,
            HorizontalAlignment = HAlignment.Left,
        };

        _licence = new Label
        {
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(10f, 0f, 10f, 4f),
        };

        var activate = new Button
        {
            Text = Loc.GetString("os-settings-activate"),
            Margin = new Thickness(10f, 0f, 10f, 8f),
            HorizontalAlignment = HAlignment.Left,
        };

        activate.OnPressed += _ => Context.Toast(Loc.GetString("os-settings-activate-failed",
            ("publisher", OsBrand.Publisher)));

        _wallpaper.OnItemSelected += args =>
        {
            _wallpaper.SelectId(args.Id);
            Change(settings => settings.Wallpaper = (OsWallpaper) args.Id);
        };

        _sounds.OnToggled += args => Change(settings => settings.Sounds = args.Pressed);
        _animations.OnToggled += args => Change(settings => settings.Animations = args.Pressed);
        _clock.OnToggled += args => Change(settings => settings.ShowClock = args.Pressed);
        _volume.OnReleased += _ => Change(settings => settings.Volume = _volume.Value);

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                OsWidgets.Caption(Loc.GetString("os-settings-appearance")),
                _wallpaper,
                _accents,
                OsWidgets.Caption(Loc.GetString("os-settings-sound")),
                _sounds,
                _volume,
                OsWidgets.Caption(Loc.GetString("os-settings-interface")),
                _animations,
                _clock,
                OsWidgets.Caption(Loc.GetString("os-settings-licence")),
                _licence,
                activate,
            },
        };

        AddChild(new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(4f),
            Children = { content },
        });
    }

    private void Change(Action<OsSettings> change)
    {
        if (_loading)
            return;

        var settings = Context.State.Settings.Clone();

        change(settings);

        Context.Send(new ADTOsSettingsMessage(settings));
    }

    private void Load()
    {
        _loading = true;

        var state = Context.State;
        var settings = state.Settings;

        _wallpaper.SelectId((int) settings.Wallpaper);
        _sounds.Pressed = settings.Sounds;
        _animations.Pressed = settings.Animations;
        _clock.Pressed = settings.ShowClock;
        _volume.Value = settings.Volume;

        _licence.Text = state.Activated
            ? Loc.GetString("os-mycomputer-licence-ok")
            : Loc.GetString("os-mycomputer-licence-no");

        foreach (var child in _accents.Children)
        {
            if (child is OsAccentSwatch swatch)
                swatch.Selected = swatch.Color == settings.Accent;
        }

        _loading = false;
    }
}

public sealed class OsAccentSwatch : Control
{
    public readonly Color Color;

    public bool Selected;

    public event Action? OnPressed;

    public OsAccentSwatch(Color color)
    {
        Color = color;

        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(26f, 22f);
        Margin = new Thickness(0f, 0f, 4f, 0f);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnPressed?.Invoke();
        args.Handle();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = UIScale;
        var box = new UIBox2(0f, 0f, PixelWidth, PixelHeight);

        OsDraw.RoundedRect(handle, box, 3f * scale, Selected ? OsStyle.TextBright : OsStyle.WindowBorder);
        OsDraw.RoundedRect(handle, new UIBox2(2f * scale, 2f * scale, PixelWidth - 2f * scale, PixelHeight - 2f * scale),
            3f * scale, Color);
    }
}
