using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.ADT.GPS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.GPS.UI;

public sealed class GpsSignalRow : PanelContainer
{
    private static readonly StyleBoxFlat StripeBox = new() { BackgroundColor = new Color(1f, 1f, 1f, 0.05f) };

    private const float FarDistance = 120f;

    private readonly Label _tag;
    private readonly ADTMarqueeLabel _description;
    private readonly ADTDirectionIcon _icon;
    private readonly Label _distance;
    private readonly Label _position;

    public GpsSignalRow()
    {
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
        PanelOverride = striped ? StripeBox : null;

        _tag.Text = signal.Tag;
        _tag.FontColorOverride = signal.Color;
        _description.Text = signal.Description ?? string.Empty;

        if (signal.Position == null)
        {
            _position.Text = Loc.GetString("adt-gps-signal-unknown");
            _distance.Text = Loc.GetString("adt-gps-signal-unknown");
            _icon.Visible = false;
            return;
        }

        _position.Text = FormatPosition(signal.Position.Value);

        if (!signal.SameMap || origin == null)
        {
            _distance.Text = Loc.GetString("adt-gps-signal-off-map");
            _icon.Visible = false;
            return;
        }

        var delta = (Vector2) (signal.Position.Value - origin.Value);
        var distance = delta.Length();

        _icon.Visible = true;
        _icon.UpdateDirection(delta, Angle.Zero);
        _icon.ModulateSelfOverride = GetProximityColor(distance);
        _distance.Text = Loc.GetString("adt-gps-signal-distance", ("distance", (int) distance));
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
