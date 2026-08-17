using Content.Client.Pinpointer.UI;
using Content.Client.ADT.Medical.CrewMonitoring;
using Content.Client.ADT.Shuttles.UI;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Threading;
using Content.Shared.Atmos;
using Content.Shared.Pinpointer;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringNavMapControl : NavMapControl
{
    // #ADT-Tweak Start - New Monitor: radar/navmap fields + corner alert UI
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;

    public NetEntity? Focus;
    public readonly Dictionary<NetEntity, string> LocalizedNames = new();
    public EntityCoordinates? SensorRangeCenter;
    public float SensorRange;

    protected override Vector2 MidPointVector => PixelSize / 2f;
    protected override int MidPoint =>
        Math.Max(1, (int)(Math.Min(PixelWidth, PixelHeight) / 2f));
    protected override int ScaledMinimapRadius =>
        Math.Max(1, MidPoint - (int)(MinimapMargin * UIScale));

    private readonly SharedTransformSystem _transform;
    private readonly GridRadarRenderer _gridRenderer;
    private readonly IGameTiming _gameTiming;
    private List<Entity<MapGridComponent>> _grids = new();
    private readonly List<Vector2> _transformedEdgeVerts = new();
    private readonly Dictionary<EntityUid, ForeignNavCache> _foreignNavCaches = new();
    private readonly HashSet<EntityUid> _seenForeignGrids = new();
    private readonly List<EntityUid> _staleForeignCaches = new();

    private MapId _cachedGridQueryMap = MapId.Nullspace;
    private Vector2 _cachedGridQueryCenter;
    private float _cachedGridQueryRange = -1f;
    private TimeSpan _cachedGridQueryTime;

    /// <summary>
    /// When the monitoring server is offline, neighbor/frame grid transforms are
    /// snapped once and reused so the map does not keep drifting with live physics.
    /// </summary>
    private bool _freezeGridTransforms;
    private Matrix3x2 _frozenWorldToFrame = Matrix3x2.Identity;
    private MapId _frozenMapId = MapId.Nullspace;
    private Vector2 _frozenCoverageCenter;
    private float _frozenCoverageRange;
    private readonly List<FrozenGridSnapshot> _frozenGrids = new();
    private readonly Dictionary<NetEntity, Vector2> _frozenBlipWorldPositions = new();

    private readonly Label _trackedEntityLabel;
    private readonly PanelContainer _trackedEntityPanel;

    private readonly PanelContainer _alertPanel;
    private readonly PanelContainer _volumePanel;
    private readonly Button _alertButton;
    private readonly TextureRect _volumeIcon;
    private readonly CrewMonitoringVerticalSlider _volumeSlider;
    private readonly BoxContainer _cornerStack;

    private Color _cornerAccent = Color.FromHex("#4CAF50");
    private Color _cornerTextDark = Color.FromHex("#0A120C");
    private Color _cornerPanelBg = Color.FromHex("#1A221A");
    private bool _suppressAlertCallback;
    private bool _suppressVolumeCallback;
    private float? _pendingLocalVolume;

    /// <summary>Pressed = alerts enabled (not muted).</summary>
    public event Action<bool>? OnAlertEnabledChanged;
    public event Action<float>? OnAlertVolumeChanged;
    // #ADT-Tweak End

    // #ADT-Tweak Start - New Monitor: ctor radar + corner controls
    public CrewMonitoringNavMapControl()
    {
        _transform = EntManager.System<SharedTransformSystem>();
        _gameTiming = IoCManager.Resolve<IGameTiming>();
        _gridRenderer = new GridRadarRenderer(
            EntManager.System<SharedMapSystem>(),
            _parallel);

        WallColor = new Color(192, 122, 196);
        TileColor = new Color(71, 42, 72);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));
        WorldMinRange = 16f;
        WorldMaxRange = 256f;
        WorldRange = 48f;
        ActualRadarRange = 48f;
        PostWallDrawingAction += DrawRadarOverlay;

        _trackedEntityLabel = new Label
        {
            Margin = new Thickness(8f, 6f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Center,
            Modulate = Color.White,
        };
        _trackedEntityPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = BackgroundColor },
            Visible = false,
        };
        _trackedEntityPanel.AddChild(_trackedEntityLabel);

        // Match the zoom / beacons / recenter strip height (label margin 8+8 + line).
        const float cornerWidth = 30f;
        const float volumePanelWidth = cornerWidth + 10f;

        _alertButton = new Button
        {
            ToggleMode = true,
            HorizontalExpand = true,
            Text = "ON",
            Margin = new Thickness(2),
        };
        _alertButton.OnToggled += args =>
        {
            if (_suppressAlertCallback)
                return;
            ApplyAlertButtonVisuals(args.Pressed);
            OnAlertEnabledChanged?.Invoke(args.Pressed);
        };

        _alertPanel = new PanelContainer
        {
            MinWidth = volumePanelWidth,
            MaxWidth = volumePanelWidth,
            Children = { _alertButton },
        };

        var volumeTex = IoCManager.Resolve<IResourceCache>()
            .GetResource<TextureResource>("/Textures/Interface/CrewMonitoring/volume.png")
            .Texture;
        _volumeIcon = new TextureRect
        {
            Texture = volumeTex,
            SetSize = new Vector2(10, 10),
            HorizontalAlignment = HAlignment.Center,
            Stretch = TextureRect.StretchMode.KeepCentered,
            Margin = new Thickness(1, 6, 1, 4),
        };
        _volumeSlider = new CrewMonitoringVerticalSlider
        {
            HorizontalAlignment = HAlignment.Center,
            HorizontalExpand = true,
            MinHeight = 72,
            MinWidth = 14,
            Margin = new Thickness(3, 0, 3, 4),
        };
        _volumeSlider.OnValueChanged += value =>
        {
            if (_suppressVolumeCallback)
                return;

            // The slider is authoritative locally while the server round-trip is pending.
            // Otherwise heartbeat states containing the previous value make it jump.
            _pendingLocalVolume = value;
            OnAlertVolumeChanged?.Invoke(value);
        };

        _volumePanel = new PanelContainer
        {
            MinWidth = volumePanelWidth,
            MaxWidth = volumePanelWidth,
            MinHeight = 110,
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children = { _volumeIcon, _volumeSlider },
                },
            },
        };

        // Same-width blocks stacked in the bottom-right corner.
        _cornerStack = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Children = { _volumePanel, _alertPanel },
        };

        // MapGridControl is a LayoutContainer with a fixed SizeFull (~648). Clear that so
        // the control fills the left panel; otherwise corner anchors sit inside a tiny box.
        SetSize = new Vector2(float.NaN, float.NaN);
        MinSize = Vector2.Zero;
        HorizontalExpand = true;
        VerticalExpand = true;

        // Stretch the zoom/beacons strip across the full map width (base uses a fixed 650px).
        var topContainer = (BoxContainer) Children[0];
        topContainer.HorizontalExpand = true;
        topContainer.VerticalExpand = true;
        // Without Wide, LayoutContainer only sizes this child to content — overlays cluster
        // in the top-left instead of the real map corners.
        LayoutContainer.SetAnchorPreset(topContainer, LayoutContainer.LayoutPreset.Wide);

        if (topContainer.Children[0] is PanelContainer topPanel)
        {
            topPanel.SetWidth = float.NaN;
            topPanel.MinWidth = 0;
            topPanel.HorizontalExpand = true;
            topPanel.HorizontalAlignment = HAlignment.Stretch;
        }

        // DrawingControl is a plain Control in NavMapControl, so LayoutContainer
        // anchors do nothing there — replace it so overlays can sit in corners.
        var oldDrawing = topContainer.Children[1];
        topContainer.RemoveChild(oldDrawing);
        var drawingArea = new LayoutContainer
        {
            Name = "DrawingControl",
            VerticalExpand = true,
            HorizontalExpand = true,
            Margin = new Thickness(0),
        };
        topContainer.AddChild(drawingArea);
        drawingArea.AddChild(_trackedEntityPanel);
        drawingArea.AddChild(_cornerStack);

        // Pin focus card flush to the bottom-left corner; grow up/right from that point.
        SetAnchorPreset(_trackedEntityPanel, LayoutPreset.BottomLeft);
        SetMarginLeft(_trackedEntityPanel, 0);
        SetMarginTop(_trackedEntityPanel, 0);
        SetMarginRight(_trackedEntityPanel, 0);
        SetMarginBottom(_trackedEntityPanel, 0);
        SetGrowHorizontal(_trackedEntityPanel, GrowDirection.End);
        SetGrowVertical(_trackedEntityPanel, GrowDirection.Begin);

        SetAnchorPreset(_cornerStack, LayoutPreset.BottomRight);
        SetMarginLeft(_cornerStack, 0);
        SetMarginTop(_cornerStack, 0);
        SetMarginRight(_cornerStack, 0);
        SetMarginBottom(_cornerStack, 0);
        SetGrowHorizontal(_cornerStack, GrowDirection.Begin);
        SetGrowVertical(_cornerStack, GrowDirection.Begin);

        ApplyAlertButtonVisuals(true);

        // Recenter on the connected monitoring server, not the grid physics center.
        RecenterButton.OnPressed += _ => RecenterToConnectedServer();
    }
    //ADT-Tweak End

    //ADT-Tweak Start - New Monitor: theme / alert / draw / foreign nav helpers
    /// <summary>
    /// Snaps the map view to <see cref="SensorRangeCenter"/> (connected server) when available.
    /// </summary>
    public void RecenterToConnectedServer()
    {
        if (SensorRangeCenter is not { } center || !center.IsValid(EntManager))
            return;

        CenterToCoordinates(center);
        // Cancel NavMapControl's default Recentering→Offset=0 so we stay on the server.
        TargetOffset = Offset;
        Recentering = false;
    }

    /// Outline color for neighboring grids / shuttles drawn in radar overlay.
    public Color NeighborGridColor { get; set; } = Color.FromHex("#7DD5E8");

    public void ApplyTheme(Color wall, Color tile, Color? neighborGrid = null, StyleBoxFlat? toolbarButton = null, Color? toolbarPanel = null)
    {
        WallColor = wall;
        TileColor = tile;
        WindowColor = Color.FromHsv(new Vector4(
            Color.ToHsv(wall).X,
            Math.Clamp(Color.ToHsv(wall).Y * 0.55f, 0.15f, 0.7f),
            Math.Clamp(Color.ToHsv(wall).Z * 1.15f, 0.75f, 0.98f),
            1f));
        BackgroundColor = Color.FromSrgb(tile.WithAlpha(BackgroundOpacity));
        if (neighborGrid is { } n)
            NeighborGridColor = n;

        var hsv = Color.ToHsv(wall);
        var h = hsv.X;
        var s = Math.Clamp(hsv.Y, 0.12f, 0.9f);

        // Toolbar strip — visibly themed panel (not near-black).
        var panelBg = toolbarPanel ?? Color.FromHsv(new Vector4(h, s * 0.50f, 0.22f, 1f));
        var panelBorder = Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.7f, 0.3f, 0.85f), 0.70f, 1f));
        NavMapTopPanel.RemoveStyleClass(StyleClass.PanelDark);
        NavMapTopPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = panelBg,
            BorderColor = panelBorder,
            BorderThickness = new Thickness(1),
        };
        NavMapTopPanel.ModulateSelfOverride = Color.White;

        // Recenter — own bright StyleBox so disabled stylesheet modulate cannot mute a shared box.
        var btnStyle = toolbarButton != null
            ? new StyleBoxFlat
            {
                BackgroundColor = toolbarButton.BackgroundColor,
                BorderColor = toolbarButton.BorderColor,
                BorderThickness = toolbarButton.BorderThickness,
            }
            : new StyleBoxFlat
            {
                BackgroundColor = Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.75f, 0.35f, 0.85f), 0.55f, 1f)),
                BorderColor = Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.55f, 0.25f, 0.7f), 0.85f, 1f)),
                BorderThickness = new Thickness(1),
            };
        btnStyle.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        btnStyle.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
        RecenterButton.StyleBoxOverride = btnStyle;
        // Defeat ButtonColorDisabled (#30313c) modulate that would crush the theme.
        RecenterButton.ModulateSelfOverride = Color.White;

        var labelColor = Color.FromHsv(new Vector4(h, Math.Clamp(s * 0.35f, 0f, 0.5f), 0.95f, 1f));
        ZoomLabel.FontColorOverride = labelColor;
        ZoomLabel.ModulateSelfOverride = Color.White;

        _trackedEntityPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = panelBg,
            BorderColor = wall,
            BorderThickness = new Thickness(1),
        };

        ApplyCornerTheme(panelBg, panelBorder, wall);
    }

    public void ApplyCornerTheme(Color panelBg, Color panelBorder, Color accent)
    {
        _cornerPanelBg = panelBg;
        _cornerAccent = Color.FromHsv(new Vector4(
            Color.ToHsv(accent).X,
            Math.Clamp(Color.ToHsv(accent).Y * 0.9f, 0.45f, 0.85f),
            0.72f,
            1f));
        _cornerTextDark = Color.FromHsv(new Vector4(
            Color.ToHsv(accent).X,
            Math.Clamp(Color.ToHsv(accent).Y * 0.55f, 0.2f, 0.6f),
            0.08f,
            1f));

        var blockStyle = new StyleBoxFlat
        {
            BackgroundColor = panelBg,
            BorderColor = panelBorder,
            BorderThickness = new Thickness(2),
        };
        _alertPanel.PanelOverride = blockStyle;
        _volumePanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = panelBg,
            BorderColor = panelBorder,
            BorderThickness = new Thickness(2),
        };

        _volumeIcon.ModulateSelfOverride = _cornerAccent;
        _volumeSlider.TrackColor = Color.FromHsv(new Vector4(Color.ToHsv(accent).X, 0.35f, 0.12f, 1f));
        _volumeSlider.FillColor = Color.FromHsv(new Vector4(Color.ToHsv(accent).X, 0.65f, 0.45f, 1f));
        _volumeSlider.GrabberColor = _cornerAccent;

        ApplyAlertButtonVisuals(_alertButton.Pressed);
    }

    public void SetAlertControls(bool alertsEnabled, float volume)
    {
        _suppressAlertCallback = true;
        _alertButton.Pressed = alertsEnabled;
        ApplyAlertButtonVisuals(alertsEnabled);
        _suppressAlertCallback = false;

        var serverVolume = Math.Clamp(volume, 0f, 1f);
        if (_pendingLocalVolume is { } localVolume)
        {
            if (MathHelper.CloseToPercent(localVolume, serverVolume))
                _pendingLocalVolume = null;
            else
                return;
        }

        _suppressVolumeCallback = true;
        _volumeSlider.SetValueSilent(serverVolume);
        _suppressVolumeCallback = false;
    }

    private void ApplyAlertButtonVisuals(bool enabled)
    {
        _alertButton.Text = enabled ? "ON" : "OFF";
        _alertButton.ModulateSelfOverride = Color.White;

        if (enabled)
        {
            var onStyle = new StyleBoxFlat
            {
                BackgroundColor = _cornerAccent,
                BorderColor = _cornerAccent,
                BorderThickness = new Thickness(1),
            };
            onStyle.SetContentMarginOverride(StyleBox.Margin.All, 2);
            _alertButton.StyleBoxOverride = onStyle;
            _alertButton.Label.FontColorOverride = _cornerTextDark;
        }
        else
        {
            var offStyle = new StyleBoxFlat
            {
                BackgroundColor = Color.Transparent,
                BorderColor = _cornerAccent,
                BorderThickness = new Thickness(1),
            };
            offStyle.SetContentMarginOverride(StyleBox.Margin.All, 2);
            _alertButton.StyleBoxOverride = offStyle;
            _alertButton.Label.FontColorOverride = _cornerAccent;
        }

        _alertButton.Label.ModulateSelfOverride = Color.White;
    }

    public void ApplyCheckboxTheme(Color color)
    {
        BeaconsCheckbox.StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent };
        BeaconsCheckbox.ModulateSelfOverride = Color.White;
        BeaconsCheckbox.TextureRect.ModulateSelfOverride = color;
        BeaconsCheckbox.Label.FontColorOverride = color;
        BeaconsCheckbox.Label.ModulateSelfOverride = Color.White;
    }

    /// <summary>
    /// Freeze / unfreeze radar grid motion. Grid matrices are captured only on the
    /// rising edge (live → frozen). After a UI rebuild while still frozen, pass
    /// <paramref name="syncBlips"/> so new markers get a snapshot without unfreezing grids.
    /// </summary>
    public void SetGridTransformsFrozen(bool frozen, bool syncBlips = false)
    {
        if (!frozen)
        {
            if (!_freezeGridTransforms)
                return;

            _freezeGridTransforms = false;
            ClearFrozenTransforms();
            return;
        }

        if (!_freezeGridTransforms)
        {
            _freezeGridTransforms = true;
            CaptureFrozenTransforms();
            return;
        }

        if (syncBlips)
            SyncFrozenBlips();
    }

    public new void CenterToCoordinates(EntityCoordinates coordinates)
    {
        if (MapUid == null || !coordinates.IsValid(EntManager))
            return;

        var source = _transform.ToMapCoordinates(coordinates);
        if (!EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform) ||
            source.MapId != frameXform.MapID)
        {
            return;
        }

        var worldToFrame = _freezeGridTransforms
            ? _frozenWorldToFrame
            : _transform.GetInvWorldMatrix(MapUid.Value);
        var framePosition = Vector2.Transform(source.Position, worldToFrame);

        if (EntManager.HasComponent<MapGridComponent>(MapUid.Value))
        {
            base.CenterToCoordinates(new EntityCoordinates(MapUid.Value, framePosition));
            return;
        }

        TargetOffset = framePosition;
        Recentering = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // NavMapControl intentionally returns early without a MapGridComponent.
        // A monitoring server may itself be in open space, where its frame is
        // the map entity; draw the complete radar fallback in that case.
        if (MapUid == null || EntManager.HasComponent<MapGridComponent>(MapUid.Value))
            return;

        handle.DrawRect(PixelSizeBox, BackgroundColor);
        DrawRadarOverlay(handle);
        DrawMapSpaceBlips(handle);
    }

    private void DrawRadarOverlay(DrawingHandleScreen handle)
    {
        if (MapUid == null ||
            !EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform))
        {
            return;
        }

        DrawRangeRings(handle);

        Matrix3x2 worldToFrame;
        Vector2 coverageCenter;
        float coverageRange;
        MapId mapId;

        if (_freezeGridTransforms)
        {
            worldToFrame = _frozenWorldToFrame;
            coverageCenter = _frozenCoverageCenter;
            coverageRange = _frozenCoverageRange;
            mapId = _frozenMapId;
        }
        else
        {
            mapId = frameXform.MapID;
            worldToFrame = _transform.GetInvWorldMatrix(MapUid.Value);
            if (!TryGetCoverage(mapId, out coverageCenter, out coverageRange))
                return;

            RefreshGridQuery(mapId, coverageCenter, coverageRange);
        }

        var offset = GetOffset();
        var frameToView =
            Matrix3x2.CreateTranslation(-offset) *
            Matrix3x2.CreateScale(new Vector2(MinimapScale, -MinimapScale)) *
            Matrix3x2.CreateTranslation(MidPointVector);

        var rangeSq = coverageRange * coverageRange;
        _seenForeignGrids.Clear();

        if (_freezeGridTransforms)
        {
            foreach (var frozen in _frozenGrids)
            {
                if (frozen.Uid == MapUid || !EntManager.EntityExists(frozen.Uid))
                    continue;

                if (!EntManager.TryGetComponent<MapGridComponent>(frozen.Uid, out var gridComp))
                    continue;

                var worldAabb = frozen.GridToWorld.TransformBox(gridComp.LocalAABB);
                if (!CircleIntersectsBox(coverageCenter, rangeSq, worldAabb))
                    continue;

                var grid = new Entity<MapGridComponent>(frozen.Uid, gridComp);
                var gridToView = frozen.GridToWorld * worldToFrame * frameToView;
                _gridRenderer.DrawGrid(
                    handle,
                    gridToView,
                    grid,
                    NeighborGridColor,
                    0.025f);

                if (EntManager.TryGetComponent<NavMapComponent>(frozen.Uid, out var navMap))
                {
                    _seenForeignGrids.Add(frozen.Uid);
                    DrawForeignNavStructures(
                        handle,
                        frozen.Uid,
                        navMap,
                        gridComp.TileSize,
                        gridToView,
                        WallColor,
                        WindowColor);
                }
            }
        }
        else
        {
            foreach (var grid in _grids)
            {
                if (grid.Owner == MapUid)
                    continue;

                var gridToWorld = _transform.GetWorldMatrix(grid.Owner);
                var worldAabb = gridToWorld.TransformBox(grid.Comp.LocalAABB);
                if (!CircleIntersectsBox(coverageCenter, rangeSq, worldAabb))
                    continue;

                var gridToView = gridToWorld * worldToFrame * frameToView;
                _gridRenderer.DrawGrid(
                    handle,
                    gridToView,
                    grid,
                    NeighborGridColor,
                    0.025f);

                // Floor fill alone hides shuttle / wood / reinforced layout — overlay NavMap walls & glass.
                if (EntManager.TryGetComponent<NavMapComponent>(grid.Owner, out var navMap))
                {
                    _seenForeignGrids.Add(grid.Owner);
                    DrawForeignNavStructures(
                        handle,
                        grid.Owner,
                        navMap,
                        grid.Comp.TileSize,
                        gridToView,
                        WallColor,
                        WindowColor);
                }
            }
        }

        PruneForeignNavCaches();
    }

    private bool TryGetCoverage(MapId mapId, out Vector2 coverageCenter, out float coverageRange)
    {
        coverageCenter = default;
        coverageRange = 0f;
        var offset = GetOffset();

        if (SensorRangeCenter != null && SensorRange > 0f)
        {
            var rangeCenter = _transform.ToMapCoordinates(SensorRangeCenter.Value);
            if (rangeCenter.MapId != mapId)
                return false;

            coverageCenter = rangeCenter.Position;
            coverageRange = SensorRange;
            return true;
        }

        if (MapUid == null)
            return false;

        coverageCenter = Vector2.Transform(
            offset,
            _transform.GetWorldMatrix(MapUid.Value));
        coverageRange = WorldRange * 1.5f;
        return true;
    }

    private void RefreshGridQuery(MapId mapId, Vector2 coverageCenter, float coverageRange)
    {
        var now = _gameTiming.CurTime;
        var queryChanged =
            _cachedGridQueryMap != mapId ||
            _cachedGridQueryRange < 0f ||
            Math.Abs(_cachedGridQueryRange - coverageRange) > 0.5f ||
            (_cachedGridQueryCenter - coverageCenter).LengthSquared() > 4f ||
            now - _cachedGridQueryTime > TimeSpan.FromSeconds(0.25);

        if (!queryChanged)
            return;

        _grids.Clear();
        var extent = new Vector2(coverageRange, coverageRange);
        _mapManager.FindGridsIntersecting(
            mapId,
            new Box2(coverageCenter - extent, coverageCenter + extent),
            ref _grids,
            approx: true,
            includeMap: false);
        _cachedGridQueryMap = mapId;
        _cachedGridQueryCenter = coverageCenter;
        _cachedGridQueryRange = coverageRange;
        _cachedGridQueryTime = now;
    }

    private void CaptureFrozenTransforms()
    {
        ClearFrozenTransforms();

        if (MapUid == null ||
            !EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform))
        {
            return;
        }

        var mapId = frameXform.MapID;
        if (!TryGetCoverage(mapId, out var coverageCenter, out var coverageRange))
            return;

        RefreshGridQuery(mapId, coverageCenter, coverageRange);

        _frozenMapId = mapId;
        _frozenWorldToFrame = _transform.GetInvWorldMatrix(MapUid.Value);
        _frozenCoverageCenter = coverageCenter;
        _frozenCoverageRange = coverageRange;

        foreach (var grid in _grids)
        {
            if (grid.Owner == MapUid)
                continue;

            _frozenGrids.Add(new FrozenGridSnapshot(
                grid.Owner,
                _transform.GetWorldMatrix(grid.Owner)));
        }

        foreach (var (entity, blip) in TrackedEntities)
        {
            var mapPosition = _transform.ToMapCoordinates(blip.Coordinates);
            if (mapPosition.MapId != mapId)
                continue;

            _frozenBlipWorldPositions[entity] = mapPosition.Position;
        }
    }

    private void ClearFrozenTransforms()
    {
        _frozenGrids.Clear();
        _frozenBlipWorldPositions.Clear();
        _frozenMapId = MapId.Nullspace;
        _frozenCoverageRange = 0f;
    }

    /// <summary>
    /// Keep frozen world positions for markers that still exist; snapshot newcomers once.
    /// Does not touch frozen grid matrices.
    /// </summary>
    private void SyncFrozenBlips()
    {
        var next = new Dictionary<NetEntity, Vector2>();
        foreach (var (entity, blip) in TrackedEntities)
        {
            if (_frozenBlipWorldPositions.TryGetValue(entity, out var existing))
            {
                next[entity] = existing;
                continue;
            }

            var mapPosition = _transform.ToMapCoordinates(blip.Coordinates);
            if (mapPosition.MapId != _frozenMapId)
                continue;

            next[entity] = mapPosition.Position;
        }

        _frozenBlipWorldPositions.Clear();
        foreach (var (entity, position) in next)
            _frozenBlipWorldPositions[entity] = position;
    }

    /// <summary>
    /// Draws wall + window outlines from another grid's NavMap into the monitoring view.
    /// Edge geometry is cached per grid and invalidated via <see cref="NavMapComponent.DataVersion"/>.
    /// </summary>
    private void DrawForeignNavStructures(
        DrawingHandleScreen handle,
        EntityUid gridUid,
        NavMapComponent navMap,
        ushort tileSize,
        Matrix3x2 gridToView,
        Color wallColor,
        Color windowColor)
    {
        if (!_foreignNavCaches.TryGetValue(gridUid, out var cache) ||
            cache.DataVersion != navMap.DataVersion ||
            cache.TileSize != tileSize)
        {
            cache = RebuildForeignNavCache(gridUid, navMap, tileSize);
        }

        DrawTransformedEdges(handle, gridToView, wallColor, cache.Walls);
        DrawTransformedEdges(handle, gridToView, windowColor, cache.Windows);
    }

    private ForeignNavCache RebuildForeignNavCache(EntityUid gridUid, NavMapComponent navMap, ushort tileSize)
    {
        if (!_foreignNavCaches.TryGetValue(gridUid, out var cache))
        {
            cache = new ForeignNavCache();
            _foreignNavCaches[gridUid] = cache;
        }

        cache.DataVersion = navMap.DataVersion;
        cache.TileSize = tileSize;
        cache.Walls.Clear();
        cache.Windows.Clear();

        CollectStructureEdges(navMap, tileSize, NavMapChunkType.Wall, SharedNavMapSystem.WallMask, hatch: true, cache.Walls);
        CollectStructureEdges(navMap, tileSize, NavMapChunkType.Window, SharedNavMapSystem.WindowMask, hatch: false, cache.Windows);
        return cache;
    }

    private void PruneForeignNavCaches()
    {
        _staleForeignCaches.Clear();
        foreach (var uid in _foreignNavCaches.Keys)
        {
            if (!_seenForeignGrids.Contains(uid))
                _staleForeignCaches.Add(uid);
        }

        foreach (var uid in _staleForeignCaches)
            _foreignNavCaches.Remove(uid);
    }

    private void CollectStructureEdges(
        NavMapComponent navMap,
        int tileSize,
        NavMapChunkType category,
        int categoryMask,
        bool hatch,
        List<(Vector2 Start, Vector2 End)> output)
    {
        var southMask = (int) AtmosDirection.South << (int) category;
        var eastMask = (int) AtmosDirection.East << (int) category;
        var westMask = (int) AtmosDirection.West << (int) category;
        var northMask = (int) AtmosDirection.North << (int) category;

        foreach (var (chunkOrigin, chunk) in navMap.Chunks)
        {
            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & categoryMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) category;
                var relative = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunkOrigin * SharedNavMapSystem.ChunkSize + relative) * tileSize;
                var isFull = tileData == SharedNavMapSystem.AllDirMask;

                if (!isFull && category == NavMapChunkType.Wall)
                {
                    // Thin / diagonal walls: approximate with a tile outline.
                    AddTileOutline(tile, tileSize, output);
                    continue;
                }

                var drawN = (tileData & (int) AtmosDirection.North) != 0;
                var drawE = (tileData & (int) AtmosDirection.East) != 0;
                var drawS = (tileData & (int) AtmosDirection.South) != 0;
                var drawW = (tileData & (int) AtmosDirection.West) != 0;

                NavMapChunk? neighborChunk;

                if (drawN)
                {
                    var neighborData = 0;
                    if (relative.Y != SharedNavMapSystem.ChunkSize - 1)
                        neighborData = chunk.TileData[i + 1];
                    else if (navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Up, out neighborChunk))
                        neighborData = neighborChunk.TileData[i + 1 - SharedNavMapSystem.ChunkSize];

                    if ((neighborData & southMask) == 0)
                        output.Add((new Vector2(tile.X, tile.Y + tileSize), new Vector2(tile.X + tileSize, tile.Y + tileSize)));
                }

                if (drawE)
                {
                    var neighborData = 0;
                    if (relative.X != SharedNavMapSystem.ChunkSize - 1)
                        neighborData = chunk.TileData[i + SharedNavMapSystem.ChunkSize];
                    else if (navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Right, out neighborChunk))
                        neighborData = neighborChunk.TileData[i + SharedNavMapSystem.ChunkSize - SharedNavMapSystem.ArraySize];

                    if ((neighborData & westMask) == 0)
                        output.Add((new Vector2(tile.X + tileSize, tile.Y), new Vector2(tile.X + tileSize, tile.Y + tileSize)));
                }

                if (drawS)
                {
                    var neighborData = 0;
                    if (relative.Y != 0)
                        neighborData = chunk.TileData[i - 1];
                    else if (navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Down, out neighborChunk))
                        neighborData = neighborChunk.TileData[i - 1 + SharedNavMapSystem.ChunkSize];

                    if ((neighborData & northMask) == 0)
                        output.Add((new Vector2(tile.X, tile.Y), new Vector2(tile.X + tileSize, tile.Y)));
                }

                if (drawW)
                {
                    var neighborData = 0;
                    if (relative.X != 0)
                        neighborData = chunk.TileData[i - SharedNavMapSystem.ChunkSize];
                    else if (navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Left, out neighborChunk))
                        neighborData = neighborChunk.TileData[i - SharedNavMapSystem.ChunkSize + SharedNavMapSystem.ArraySize];

                    if ((neighborData & eastMask) == 0)
                        output.Add((new Vector2(tile.X, tile.Y), new Vector2(tile.X, tile.Y + tileSize)));
                }

                if (hatch && isFull)
                    output.Add((new Vector2(tile.X, tile.Y), new Vector2(tile.X + tileSize, tile.Y + tileSize)));
            }
        }
    }

    private static void AddTileOutline(Vector2i tile, int tileSize, List<(Vector2 Start, Vector2 End)> output)
    {
        var x0 = (float) tile.X;
        var y0 = (float) tile.Y;
        var x1 = x0 + tileSize;
        var y1 = y0 + tileSize;
        output.Add((new Vector2(x0, y0), new Vector2(x1, y0)));
        output.Add((new Vector2(x1, y0), new Vector2(x1, y1)));
        output.Add((new Vector2(x1, y1), new Vector2(x0, y1)));
        output.Add((new Vector2(x0, y1), new Vector2(x0, y0)));
    }

    private void DrawTransformedEdges(
        DrawingHandleScreen handle,
        Matrix3x2 gridToView,
        Color color,
        List<(Vector2 Start, Vector2 End)> edges)
    {
        if (edges.Count == 0)
            return;

        _transformedEdgeVerts.Clear();
        if (_transformedEdgeVerts.Capacity < edges.Count * 2)
            _transformedEdgeVerts.Capacity = edges.Count * 2;

        foreach (var (start, end) in edges)
        {
            _transformedEdgeVerts.Add(Vector2.Transform(start, gridToView));
            _transformedEdgeVerts.Add(Vector2.Transform(end, gridToView));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, _transformedEdgeVerts, color);
    }

    private static bool CircleIntersectsBox(Vector2 center, float radiusSq, Box2 box)
    {
        var closest = Vector2.Clamp(center, box.BottomLeft, box.TopRight);
        return Vector2.DistanceSquared(center, closest) <= radiusSq;
    }

    private sealed class ForeignNavCache
    {
        public int DataVersion = -1;
        public ushort TileSize;
        public readonly List<(Vector2 Start, Vector2 End)> Walls = new();
        public readonly List<(Vector2 Start, Vector2 End)> Windows = new();
    }

    /// <summary>
    /// Concentric rings + spokes centered on the monitoring server, sized to its wireless range.
    /// </summary>
    private void DrawRangeRings(DrawingHandleScreen handle)
    {
        if (!TryGetSensorViewOrigin(out var origin, out var range))
            return;

        var screenRadius = range * MinimapScale;

        // Quarter / half / three-quarter / full range rings.
        for (var i = 1; i <= 4; i++)
        {
            var radius = screenRadius * (i / 4f);
            var color = i == 4
                ? Color.Gold.WithAlpha(0.8f)
                : Color.LightGray.WithAlpha(0.08f);
            handle.DrawCircle(origin, radius, color, false);
        }

        for (var i = 0; i < 8; i++)
        {
            Angle angle = Math.PI / 8f * i;
            var extent = angle.ToVec() * screenRadius;
            handle.DrawLine(
                origin - extent,
                origin + extent,
                Color.LightGray.WithAlpha(0.05f));
        }
    }

    private bool TryGetSensorViewOrigin(out Vector2 viewOrigin, out float range)
    {
        viewOrigin = default;
        range = 0f;

        if (MapUid == null)
            return false;

        Vector2 coverageWorld;
        float coverageRange;
        Matrix3x2 worldToFrame;

        if (_freezeGridTransforms)
        {
            if (_frozenCoverageRange <= 0f)
                return false;

            coverageWorld = _frozenCoverageCenter;
            coverageRange = _frozenCoverageRange;
            worldToFrame = _frozenWorldToFrame;
        }
        else
        {
            if (SensorRangeCenter == null ||
                SensorRange <= 0f ||
                !EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform))
            {
                return false;
            }

            var rangeCenter = _transform.ToMapCoordinates(SensorRangeCenter.Value);
            if (rangeCenter.MapId != frameXform.MapID)
                return false;

            coverageWorld = rangeCenter.Position;
            coverageRange = SensorRange;
            worldToFrame = _transform.GetInvWorldMatrix(MapUid.Value);
        }

        var framePosition =
            Vector2.Transform(coverageWorld, worldToFrame) -
            GetOffset();
        viewOrigin = ScalePosition(new Vector2(framePosition.X, -framePosition.Y));
        range = coverageRange;
        return true;
    }

    private void DrawMapSpaceBlips(DrawingHandleScreen handle)
    {
        if (MapUid == null ||
            !EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform))
        {
            return;
        }

        var worldToFrame = _freezeGridTransforms
            ? _frozenWorldToFrame
            : _transform.GetInvWorldMatrix(MapUid.Value);
        var mapId = _freezeGridTransforms ? _frozenMapId : frameXform.MapID;
        var lit = Timing.RealTime.TotalSeconds % 1f > 0.5f;
        foreach (var (entity, blip) in TrackedEntities)
        {
            if (blip.Blinks && !lit)
                continue;

            Vector2 worldPos;
            if (_freezeGridTransforms)
            {
                if (!_frozenBlipWorldPositions.TryGetValue(entity, out worldPos))
                    continue;
            }
            else
            {
                var mapPosition = _transform.ToMapCoordinates(blip.Coordinates);
                if (mapPosition.MapId != mapId)
                    continue;
                worldPos = mapPosition.Position;
            }

            var local =
                Vector2.Transform(worldPos, worldToFrame) -
                GetOffset();
            var position = ScalePosition(new Vector2(local.X, -local.Y));
            var scale = 0.075f * float.Sqrt(MinimapScale) * blip.Scale;
            var extent = new Vector2(
                scale * blip.Texture.Width,
                scale * blip.Texture.Height);
            handle.DrawTextureRect(
                blip.Texture,
                new UIBox2(position - extent, position + extent),
                blip.Color);

            if (entity == Focus)
                handle.DrawCircle(position, MathF.Max(6f, extent.Length()), Color.White, false);
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (MapUid == null ||
            EntManager.HasComponent<MapGridComponent>(MapUid.Value) ||
            args.Function != EngineKeyFunctions.UIClick ||
            !EntManager.TryGetComponent<TransformComponent>(MapUid.Value, out var frameXform))
        {
            return;
        }

        var local = args.PointerLocation.Position - GlobalPixelPosition;
        var unscaled = (local - MidPointVector) / MinimapScale;
        var framePosition =
            GetOffset() +
            new Vector2(unscaled.X, -unscaled.Y);

        Matrix3x2 frameToWorld;
        MapId mapId;
        if (_freezeGridTransforms)
        {
            if (!Matrix3x2.Invert(_frozenWorldToFrame, out frameToWorld))
                return;
            mapId = _frozenMapId;
        }
        else
        {
            frameToWorld = _transform.GetWorldMatrix(MapUid.Value);
            mapId = frameXform.MapID;
        }

        var worldPosition = Vector2.Transform(framePosition, frameToWorld);

        var closest = NetEntity.Invalid;
        var closestDistance = float.PositiveInfinity;
        foreach (var (entity, blip) in TrackedEntities)
        {
            if (!blip.Selectable)
                continue;

            Vector2 blipWorld;
            if (_freezeGridTransforms)
            {
                if (!_frozenBlipWorldPositions.TryGetValue(entity, out blipWorld))
                    continue;
            }
            else
            {
                var mapPosition = _transform.ToMapCoordinates(blip.Coordinates);
                if (mapPosition.MapId != mapId)
                    continue;
                blipWorld = mapPosition.Position;
            }

            var distance = Vector2.Distance(worldPosition, blipWorld);
            if (distance >= closestDistance || distance * MinimapScale > 10f)
                continue;

            closest = entity;
            closestDistance = distance;
        }

        if (closest.IsValid())
            SelectTrackedEntity(closest);
    }
    //ADT-Tweak End

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Focus == null || !TrackedEntities.TryGetValue(Focus.Value, out var blip))   //ADT-Tweak - New Monitor
        {
            _trackedEntityLabel.Text = string.Empty;
            _trackedEntityPanel.Visible = false;


            return;
        }

        if (!LocalizedNames.TryGetValue(Focus.Value, out var name))
        //ADT-Tweak Start - New Monitor: rewritten FrameUpdate
            name = Loc.GetString("navmap-unknown-entity");

        Vector2 position;
        if (_freezeGridTransforms &&
            _frozenBlipWorldPositions.TryGetValue(Focus.Value, out var frozenPos))
        {
            position = frozenPos;
        }
        else
        {
            position = _transform.ToMapCoordinates(blip.Coordinates).Position;
        }

        _trackedEntityLabel.Text =
            name + "\n" +
            Loc.GetString(
                "navmap-location",
                ("x", MathF.Round(position.X)),
                ("y", MathF.Round(position.Y)));
        _trackedEntityPanel.Visible = true;
        // #ADT-Tweak End
    }

    private readonly record struct FrozenGridSnapshot(EntityUid Uid, Matrix3x2 GridToWorld);
}
