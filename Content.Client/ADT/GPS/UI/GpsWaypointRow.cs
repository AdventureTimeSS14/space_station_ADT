using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.ADT.GPS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.GPS.UI;

public sealed class GpsWaypointRow : PanelContainer
{
    [Dependency] private readonly IEyeManager _eye = default!;

    private static readonly StyleBoxFlat StripeBox = new() { BackgroundColor = new Color(1f, 1f, 1f, 0.05f) };

    private static readonly Color WaypointColor = Color.FromHex("#FFC44D");

    private readonly Label _name;
    private readonly ADTDirectionIcon _icon;
    private readonly Label _distance;
    private readonly Label _position;
    private readonly Button _remove;

    private Vector2? _delta;

    public event Action? OnRemovePressed;

    public GpsWaypointRow()
    {
        IoCManager.InjectDependencies(this);

        _name = new Label
        {
            MinWidth = 130,
            HorizontalExpand = true,
            ClipText = true,
            FontColorOverride = WaypointColor,
        };

        _icon = new ADTDirectionIcon(snap: false, minDistance: 0.5f)
        {
            SetSize = new Vector2(16, 16),
            VerticalAlignment = VAlignment.Center,
            ModulateSelfOverride = WaypointColor,
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

        _remove = new Button
        {
            Text = Loc.GetString("adt-gps-window-waypoint-remove"),
            MinWidth = 30,
            VerticalAlignment = VAlignment.Center,
            StyleClasses = { "negative" },
        };

        _remove.OnPressed += _ => OnRemovePressed?.Invoke();

        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
            Margin = new Thickness(4, 2, 4, 2),
        };

        row.AddChild(_name);
        row.AddChild(_icon);
        row.AddChild(_distance);
        row.AddChild(_position);
        row.AddChild(_remove);

        AddChild(row);
    }

    public void Update(GpsWaypointData waypoint, Vector2i? origin, bool striped)
    {
        PanelOverride = striped ? StripeBox : null;

        _name.Text = waypoint.Name;
        _position.Text = GpsSignalRow.FormatPosition(waypoint.Position);

        if (!waypoint.SameMap || origin == null)
        {
            _distance.Text = Loc.GetString("adt-gps-signal-off-map");
            _icon.Visible = false;
            _delta = null;
            return;
        }

        var delta = (Vector2) (waypoint.Position - origin.Value);

        _delta = delta;
        _icon.Visible = true;
        _distance.Text = Loc.GetString("adt-gps-signal-distance", ("distance", (int) delta.Length()));

        UpdateArrow();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdateArrow();
    }

    private void UpdateArrow()
    {
        if (_delta is not { } delta)
            return;

        _icon.UpdateDirection(delta, -_eye.CurrentEye.Rotation);
    }
}
