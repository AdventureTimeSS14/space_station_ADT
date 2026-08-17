using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.ADT.GPS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.GPS.UI;

public sealed class GpsSignalRow : PanelContainer
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly StyleBoxFlat StripeBox = new() { BackgroundColor = new Color(1f, 1f, 1f, 0.05f) };
    private static readonly StyleBoxFlat SosBox = new() { BackgroundColor = new Color(1f, 0f, 0f, 0.15f) };

    private static readonly Color SosColor = Color.FromHex("#FF4444");

    private const float FarDistance = 120f;
    private const float SosBlinkRate = 1.5f;

    private readonly Label _tag;
    private readonly ADTMarqueeLabel _description;
    private readonly ADTDirectionIcon _icon;
    private readonly Label _distance;
    private readonly Label _position;

    private Vector2? _delta;
    private bool _sos;

    public GpsSignalRow()
    {
        IoCManager.InjectDependencies(this);

        _tag = new Label
        {
            MinWidth = 130,
            ClipText = true,
        };

        _description = new ADTMarqueeLabel
        {
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };
        _description.AddStyleClasses(StyleNano.StyleClassLabelSubText);

        _icon = new ADTDirectionIcon(snap: false, minDistance: 0.5f)
        {
            SetSize = new Vector2(16, 16),
            VerticalAlignment = VAlignment.Center,
        };

        _distance = new Label
        {
            MinWidth = 55,
            Align = Label.AlignMode.Right,
        };

        _position = new Label
        {
            MinWidth = 95,
            Align = Label.AlignMode.Right,
            StyleClasses = { StyleNano.StyleClassLabelSubText },
        };

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
            Margin = new Thickness(4, 2, 4, 2),
        };

        row.AddChild(_tag);
        row.AddChild(_description);
        row.AddChild(_icon);
        row.AddChild(_distance);
        row.AddChild(_position);

        AddChild(row);
    }

    public void Update(GpsSignalData signal, Vector2i? origin, bool striped)
    {
        _sos = signal.Sos;

        PanelOverride = signal.Sos
            ? SosBox
            : striped ? StripeBox : null;

        _tag.Text = signal.Sos
            ? Loc.GetString("adt-gps-signal-sos-tag", ("tag", signal.Tag))
            : signal.Tag;

        _tag.FontColorOverride = signal.Sos ? SosColor : signal.Color;

        _description.Text = signal.Sos
            ? Loc.GetString("adt-gps-signal-sos-desc")
            : signal.Description ?? string.Empty;

        if (signal.Position == null)
        {
            _position.Text = Loc.GetString("adt-gps-signal-unknown");
            _distance.Text = Loc.GetString("adt-gps-signal-unknown");
            _icon.Visible = false;
            _delta = null;
            return;
        }

        _position.Text = FormatPosition(signal.Position.Value);

        if (!signal.SameMap || origin == null)
        {
            _distance.Text = Loc.GetString("adt-gps-signal-off-map");
            _icon.Visible = false;
            _delta = null;
            return;
        }

        var delta = (Vector2) (signal.Position.Value - origin.Value);
        var distance = delta.Length();

        _delta = delta;
        _icon.Visible = true;
        _icon.ModulateSelfOverride = signal.Sos ? SosColor : GetProximityColor(distance);
        _distance.Text = Loc.GetString("adt-gps-signal-distance", ("distance", (int) distance));

        UpdateArrow();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateArrow();

        if (_sos)
            Modulate = Color.White.WithAlpha(0.6f + 0.4f * MathF.Abs(MathF.Sin((float) _timing.RealTime.TotalSeconds * SosBlinkRate)));
        else
            Modulate = Color.White;
    }

    private void UpdateArrow()
    {
        if (_delta is not { } delta)
            return;

        _icon.UpdateDirection(delta, -_eye.CurrentEye.Rotation);
    }

    private static Color GetProximityColor(float distance)
    {
        var t = Math.Clamp(distance / FarDistance, 0f, 1f);

        return Color.FromHsv(new Vector4(MathHelper.Lerp(1f / 3f, 0f, t), 0.85f, 1f, 1f));
    }

    public static string FormatPosition(Vector2i position)
    {
        return Loc.GetString("adt-gps-signal-position", ("x", position.X), ("y", position.Y));
    }
}
