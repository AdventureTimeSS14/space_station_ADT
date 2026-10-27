using System.Numerics;
using Content.Client.ADT.Drawing.Sync;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingViewerWindow : DefaultWindow
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly Guid _painting;
    private readonly CanvasViewSystem _view;
    private readonly PaintingViewControl _control;
    private readonly Label _status;

    public PaintingViewerWindow(Guid painting)
    {
        IoCManager.InjectDependencies(this);

        _painting = painting;
        _view = _systems.GetEntitySystem<CanvasViewSystem>();

        Title = Loc.GetString("adt-painting-viewer-title");
        MinSize = new Vector2(320, 260);
        SetSize = new Vector2(720, 640);

        _control = new PaintingViewControl(_clyde, _systems.GetEntitySystem<CanvasBlobSystem>());
        _status = new Label
        {
            Text = Loc.GetString("adt-painting-viewer-loading"),
            FontColorOverride = Color.Gray,
        };

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        root.AddChild(_status);
        root.AddChild(_control);
        Contents.AddChild(root);

        _view.ViewUpdated += OnViewUpdated;
        OnClose += OnClosed;

        if (_view.TryGetView(painting, out var info))
            Apply(info);
        else
            _view.RequestView(painting);
    }

    private void OnViewUpdated(Guid painting)
    {
        if (painting != _painting)
            return;

        if (_view.TryGetView(painting, out var info))
            Apply(info);
        else
            _view.RequestView(painting, true);
    }

    private void Apply(PaintingViewInfo info)
    {
        if (!info.Allowed || info.Levels == 0)
        {
            _status.Text = Loc.GetString("adt-painting-viewer-denied");
            return;
        }

        Title = string.IsNullOrWhiteSpace(info.Title)
            ? Loc.GetString("adt-painting-viewer-title")
            : info.Title;

        _status.Text = Loc.GetString("adt-painting-viewer-status",
            ("width", info.Width),
            ("height", info.Height),
            ("status", Loc.GetString($"adt-painting-status-{info.Status.ToString().ToLowerInvariant()}")));

        _control.SetInfo(info);
    }

    private void OnClosed()
    {
        _view.ViewUpdated -= OnViewUpdated;
        _control.Dispose();
    }
}
