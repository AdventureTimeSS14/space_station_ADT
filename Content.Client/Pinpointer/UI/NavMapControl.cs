using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Input;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using System.Numerics;
using JetBrains.Annotations;
using Content.Shared.Atmos;
using System.Linq;
using Robust.Shared.Utility;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
[UsedImplicitly, Virtual]
public partial class NavMapControl : MapGridControl
{
    [Dependency] private IResourceCache _cache = default!;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedNavMapSystem _navMapSystem;

    public EntityUid? Owner;
    public EntityUid? MapUid;

    protected override bool Draggable => true;

    // Actions
    public event Action<NetEntity?>? TrackedEntitySelectedAction;
    public event Action<DrawingHandleScreen>? PostWallDrawingAction;

    // ADT-Tweak Start - New Monitor
    protected void SelectTrackedEntity(NetEntity? entity)
    {
        TrackedEntitySelectedAction?.Invoke(entity);
    }
    // ADT-Tweak End

    // Tracked data
    public Dictionary<EntityCoordinates, (bool Visible, Color Color)> TrackedCoordinates = new();
    public Dictionary<NetEntity, NavMapBlip> TrackedEntities = new();

    public List<(Vector2, Vector2)> TileLines = new();
    public List<(Vector2, Vector2)> TileRects = new();
    public List<(Vector2[], Color)> TilePolygons = new();
    public List<(Vector2, Vector2)> WindowLines = new();    // ADT-Tweak - New Monitor
    public List<NavMapRegionOverlay> RegionOverlays = new();

    // Default colors
    public Color WallColor = new(102, 217, 102);
    public Color TileColor = new(30, 67, 30);
    public Color WindowColor = new(102, 217, 102);  // ADT-Tweak - New Monitor

    // Constants
    protected float UpdateTime = 1.0f;
    protected float MaxSelectableDistance = 10f;
    protected float MinDragDistance = 5f;
    protected static float MinDisplayedRange = 8f;
    protected static float MaxDisplayedRange = 128f;
    protected static float DefaultDisplayedRange = 48f;
    protected float MinmapScaleModifier = 0.075f;
    protected float FullWallInstep = 0.165f;
    protected float ThinWallThickness = 0.165f;
    protected float ThinDoorThickness = 0.30f;

    // Local variables
    private float _updateTimer = 1.0f;
    // ADT-Tweak Start - New Monitor
    private int _appliedNavMapVersion = -1;
    private Vector2 _lastCullOffset;
    private float _lastCullRange = -1f;
    // ADT-Tweak End
    private Dictionary<Color, Color> _sRGBLookUp = new();
    protected Color BackgroundColor;
    protected float BackgroundOpacity = 0.9f;
    private int _targetFontsize = 8;

    private Dictionary<Vector2i, Vector2i> _horizLines = new();
    private Dictionary<Vector2i, Vector2i> _horizLinesReversed = new();
    private Dictionary<Vector2i, Vector2i> _vertLines = new();
    private Dictionary<Vector2i, Vector2i> _vertLinesReversed = new();

    // Components
    private NavMapComponent? _navMap;
    private MapGridComponent? _grid;
    private TransformComponent? _xform;
    private PhysicsComponent? _physics;
    private FixturesComponent? _fixtures;

    // TODO: https://github.com/space-wizards/RobustToolbox/issues/3818
    protected readonly Label ZoomLabel = new()    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
    {
        VerticalAlignment = VAlignment.Top,
        HorizontalExpand = true,
        Margin = new Thickness(8f, 8f),
    };

    protected readonly Button RecenterButton = new()    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
    {
        Text = Loc.GetString("navmap-recenter"),
        VerticalAlignment = VAlignment.Top,
        HorizontalAlignment = HAlignment.Right,
        HorizontalExpand = true,
        Margin = new Thickness(8f, 4f),
        Disabled = true,
    };

    protected readonly CheckBox BeaconsCheckbox = new()    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
    {
        Text = Loc.GetString("navmap-toggle-beacons"),
        VerticalAlignment = VAlignment.Center,
        HorizontalAlignment = HAlignment.Center,
        HorizontalExpand = true,
        Margin = new Thickness(4f, 0f),
        Pressed = true,
    };

    protected readonly PanelContainer NavMapTopPanel;   // ADT-Tweak - New Monitor

    public NavMapControl() : base(MinDisplayedRange, MaxDisplayedRange, DefaultDisplayedRange)
    {
        IoCManager.InjectDependencies(this);

        _transformSystem = EntManager.System<SharedTransformSystem>();
        _navMapSystem = EntManager.System<SharedNavMapSystem>();

        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        RectClipContent = true;
        HorizontalExpand = true;
        VerticalExpand = true;

        var topPanel = new PanelContainer()
        {
            StyleClasses = { StyleClass.PanelDark },
            VerticalExpand = false,
            HorizontalExpand = true,
            SetWidth = 650f,
            Children =
            {
                new BoxContainer()
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children =
                    {
                        // ADT-Tweak Start - New Monitor: private -> protected, avoding bug issue 3818
                        ZoomLabel,
                        BeaconsCheckbox,
                        RecenterButton
                        // ADT-Tweak End
                    }
                }
            }
        };
        NavMapTopPanel = topPanel;

        var topContainer = new BoxContainer()
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children =
            {
                topPanel,
                new Control()
                {
                    Name = "DrawingControl",
                    VerticalExpand = true,
                    Margin = new Thickness(5f, 5f)
                }
            }
        };

        AddChild(topContainer);
        topPanel.Measure(Vector2Helpers.Infinity);

        RecenterButton.OnPressed += args =>
        {
            Recentering = true;
        };

        ForceNavMapUpdate();
    }

    public void ForceNavMapUpdate()
    {
        EntManager.TryGetComponent(MapUid, out _navMap);
        EntManager.TryGetComponent(MapUid, out _grid);
        EntManager.TryGetComponent(MapUid, out _xform);
        EntManager.TryGetComponent(MapUid, out _physics);
        EntManager.TryGetComponent(MapUid, out _fixtures);
        UpdateNavMap();
        _appliedNavMapVersion = _navMap?.DataVersion ?? -1;
    }

    public void CenterToCoordinates(EntityCoordinates coordinates)
    {
        if (_physics != null)
            Offset = new Vector2(coordinates.X, coordinates.Y) - _physics.LocalCenter;  // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818

        RecenterButton.Disabled = false;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick)
        {
            if (TrackedEntitySelectedAction == null)
                return;

            if (_xform == null || _physics == null || TrackedEntities.Count == 0)
                return;

            // If the cursor has moved a significant distance, exit
            if ((StartDragPosition - args.PointerLocation.Position).Length() > MinDragDistance)
                return;

            // Get the clicked position
            var offset = Offset + _physics.LocalCenter;
            var localPosition = args.PointerLocation.Position - GlobalPixelPosition;

            // Convert to a world position
            var unscaledPosition = (localPosition - MidPointVector) / MinimapScale;
            var worldPosition = Vector2.Transform(new Vector2(unscaledPosition.X, -unscaledPosition.Y) + offset, _transformSystem.GetWorldMatrix(_xform));

            // Find closest tracked entity in range
            var closestEntity = NetEntity.Invalid;
            var closestDistance = float.PositiveInfinity;

            foreach ((var currentEntity, var blip) in TrackedEntities)
            {
                if (!blip.Selectable)
                    continue;

                var currentDistance = (_transformSystem.ToMapCoordinates(blip.Coordinates).Position - worldPosition).Length();

                if (closestDistance < currentDistance || currentDistance * MinimapScale > MaxSelectableDistance)
                    continue;

                closestEntity = currentEntity;
                closestDistance = currentDistance;
            }

            if (closestDistance > MaxSelectableDistance || !closestEntity.IsValid())
                return;

            TrackedEntitySelectedAction.Invoke(closestEntity);
        }

        else if (args.Function == EngineKeyFunctions.UIRightClick)
        {
            // Clear current selection with right click
            TrackedEntitySelectedAction?.Invoke(null);
        }

        else if (args.Function == ContentKeyFunctions.ExamineEntity)
        {
            // Toggle beacon labels
            BeaconsCheckbox.Pressed = !BeaconsCheckbox.Pressed; // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
        }
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (Offset != Vector2.Zero)
            RecenterButton.Disabled = false;    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
        else
            RecenterButton.Disabled = true;     // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // Get the components necessary for drawing the navmap
        EntManager.TryGetComponent(MapUid, out _navMap);
        EntManager.TryGetComponent(MapUid, out _grid);
        EntManager.TryGetComponent(MapUid, out _xform);
        EntManager.TryGetComponent(MapUid, out _physics);
        EntManager.TryGetComponent(MapUid, out _fixtures);

        if (_navMap == null || _grid == null || _xform == null)
            return;

        // Map re-centering
        RecenterButton.Disabled = DrawRecenter();   // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818

        // Update zoom text
        ZoomLabel.Text = Loc.GetString("navmap-zoom", ("value", $"{(DefaultDisplayedRange / WorldRange):0.0}"));    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818

        // Update offset with physics local center
        var offset = Offset;

        if (_physics != null)
            offset += _physics.LocalCenter;

        var offsetVec = new Vector2(offset.X, -offset.Y);

        // Wall sRGB
        if (!_sRGBLookUp.TryGetValue(WallColor, out var wallsRGB))
        {
            wallsRGB = Color.ToSrgb(WallColor);
            _sRGBLookUp[WallColor] = wallsRGB;
        }

        // Draw floor tiles
        if (TilePolygons.Any())
        {
            Span<Vector2> verts = new Vector2[8];

            foreach (var (polygonVerts, polygonColor) in TilePolygons)
            {
                for (var i = 0; i < polygonVerts.Length; i++)
                {
                    var vert = polygonVerts[i] - offset;
                    verts[i] = ScalePosition(new Vector2(vert.X, -vert.Y));
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts[..polygonVerts.Length], polygonColor);
            }
        }

        // Draw region overlays
        if (_grid != null)
        {
            foreach (var regionOverlay in RegionOverlays)
            {
                foreach (var gridCoords in regionOverlay.GridCoords)
                {
                    var positionTopLeft = ScalePosition(new Vector2(gridCoords.Item1.X, -gridCoords.Item1.Y) - new Vector2(offset.X, -offset.Y));
                    var positionBottomRight = ScalePosition(new Vector2(gridCoords.Item2.X + _grid.TileSize, -gridCoords.Item2.Y - _grid.TileSize) - new Vector2(offset.X, -offset.Y));

                    var box = new UIBox2(positionTopLeft, positionBottomRight);
                    handle.DrawRect(box, regionOverlay.Color);
                }
            }
        }

        // Draw map lines
        if (TileLines.Any())
        {
            var lines = new ValueList<Vector2>(TileLines.Count * 2);

            foreach (var (o, t) in TileLines)
            {
                var origin = ScalePosition(o - offsetVec);
                var terminus = ScalePosition(t - offsetVec);

                lines.Add(origin);
                lines.Add(terminus);
            }

            if (lines.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, lines.Span, wallsRGB);
        }

        // ADT-Tweak Start - New Monitor: Hollow window outlines (merged across adjacent glass tiles)
        if (WindowLines.Count > 0)
        {
            if (!_sRGBLookUp.TryGetValue(WindowColor, out var windowRGB))
            {
                windowRGB = Color.ToSrgb(WindowColor);
                _sRGBLookUp[WindowColor] = windowRGB;
            }

            var windowVerts = new ValueList<Vector2>(WindowLines.Count * 2);
            foreach (var (o, t) in WindowLines)
            {
                windowVerts.Add(ScalePosition(o - offsetVec));
                windowVerts.Add(ScalePosition(t - offsetVec));
            }

            if (windowVerts.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, windowVerts.Span, windowRGB);
        }
        // ADT-Tweak End

        // Draw map rects
        if (TileRects.Any())
        {
            var rects = new ValueList<Vector2>(TileRects.Count * 8);

            foreach (var (lt, rb) in TileRects)
            {
                var leftTop = ScalePosition(lt - offsetVec);
                var rightBottom = ScalePosition(rb - offsetVec);

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                rects.Add(leftTop);
                rects.Add(rightTop);
                rects.Add(rightTop);
                rects.Add(rightBottom);
                rects.Add(rightBottom);
                rects.Add(leftBottom);
                rects.Add(leftBottom);
                rects.Add(leftTop);
            }

            if (rects.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, rects.Span, wallsRGB);
        }

        // Invoke post wall drawing action
        if (PostWallDrawingAction != null)
            PostWallDrawingAction.Invoke(handle);

        var curTime = Timing.RealTime;
        var blinkFrequency = 1f / 1f;
        var lit = curTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;

        // Tracked coordinates (simple dot, legacy)
        foreach (var (coord, value) in TrackedCoordinates)
        {
            if (lit && value.Visible)
            {
                var mapPos = _transformSystem.ToMapCoordinates(coord);

                if (mapPos.MapId != MapId.Nullspace)
                {
                    var position = Vector2.Transform(mapPos.Position, _transformSystem.GetInvWorldMatrix(_xform)) - offset;
                    position = ScalePosition(new Vector2(position.X, -position.Y));

                    handle.DrawCircle(position, float.Sqrt(MinimapScale) * 2f, value.Color);
                }
            }
        }

        // Tracked entities (can use a supplied sprite as a marker instead; should probably just replace TrackedCoordinates with this eventually)
        foreach (var blip in TrackedEntities.Values)
        {
            if (blip.Blinks && !lit)
                continue;

            if (blip.Texture == null)
                continue;

            var mapPos = _transformSystem.ToMapCoordinates(blip.Coordinates);

            if (mapPos.MapId != MapId.Nullspace)
            {
                var position = Vector2.Transform(mapPos.Position, _transformSystem.GetInvWorldMatrix(_xform)) - offset;
                position = ScalePosition(new Vector2(position.X, -position.Y));

                var scalingCoefficient = MinmapScaleModifier * float.Sqrt(MinimapScale);
                var positionOffset = new Vector2(scalingCoefficient * blip.Scale * blip.Texture.Width, scalingCoefficient * blip.Scale * blip.Texture.Height);

                handle.DrawTextureRect(blip.Texture, new UIBox2(position - positionOffset, position + positionOffset), blip.Color);
            }
        }

        // Beacons
        if (BeaconsCheckbox.Pressed)    // ADT-Tweak - New Monitor: private -> protected, avoding bug issue 3818
        {
            var rectBuffer = new Vector2(5f, 3f);

            // Calculate font size for current zoom level
            var fontSize = (int)Math.Round(1 / WorldRange * DefaultDisplayedRange * UIScale * _targetFontsize, 0);
            var font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), fontSize);

            foreach (var beacon in _navMap.Beacons.Values)
            {
                var position = beacon.Position - offset;
                position = ScalePosition(position with { Y = -position.Y });

                var textDimensions = handle.GetDimensions(font, beacon.Text, 1f);
                handle.DrawRect(new UIBox2(position - textDimensions / 2 - rectBuffer, position + textDimensions / 2 + rectBuffer), BackgroundColor);
                handle.DrawString(font, position - textDimensions / 2, beacon.Text, beacon.Color);
            }
        }
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        //ADT-Tweak Start - New Monitor: Refresh component refs so DataVersion invalidation works even between Draws.
        EntManager.TryGetComponent(MapUid, out _navMap);
        EntManager.TryGetComponent(MapUid, out _physics);

        var needsRebuild = false;

        if (_navMap != null && _navMap.DataVersion != _appliedNavMapVersion)
            needsRebuild = true;

        // Viewport culling: rebuild when the camera moves/zooms enough that
        // previously skipped chunks may now be visible (or vice versa).
        var cullOffset = GetOffset();
        if (_lastCullRange < 0f ||
            Math.Abs(WorldRange - _lastCullRange) > 0.5f ||
            (cullOffset - _lastCullOffset).LengthSquared() > WorldRange * 0.15f * WorldRange * 0.15f)
        {
            needsRebuild = true;
        }

        if (needsRebuild)
        {
            EntManager.TryGetComponent(MapUid, out _grid);
            EntManager.TryGetComponent(MapUid, out _xform);
            EntManager.TryGetComponent(MapUid, out _fixtures);
            UpdateNavMap();
            _appliedNavMapVersion = _navMap?.DataVersion ?? -1;
            _lastCullOffset = cullOffset;
            _lastCullRange = WorldRange;
            _updateTimer = 0f;
            return;
        }
        // ADT-Tweak End

        // Update the timer
        _updateTimer += args.DeltaSeconds;

        if (_updateTimer >= UpdateTime)
        {
            _updateTimer -= UpdateTime;

            UpdateNavMap();
            // ADT-Tweak Start - New Monitor
            _appliedNavMapVersion = _navMap?.DataVersion ?? -1;
            _lastCullOffset = cullOffset;
            _lastCullRange = WorldRange;
            // ADT-Tweak End
        }
    }

    protected virtual void UpdateNavMap()
    {
        // Clear stale values
        TilePolygons.Clear();
        TileLines.Clear();
        TileRects.Clear();
        WindowLines.Clear();    // ADT-Tweak - New Monitor

        UpdateNavMapFloorTiles();
        UpdateNavMapWallLines();
        UpdateNavMapWindows();
        UpdateNavMapAirlocks();
    }

    // ADT-Tweak Start - New Monitor
    /// <summary>
    /// Local-space AABB of the current camera view, with a margin for chunk edges.
    /// </summary>
    protected Box2 GetViewBoundsLocal(float margin = 1.25f)
    {
        var offset = GetOffset();
        var range = WorldRange * margin;
        return new Box2(offset.X - range, offset.Y - range, offset.X + range, offset.Y + range);
    }

    protected static bool ChunkIntersectsView(Vector2i chunkOrigin, float tileSize, in Box2 view)
    {
        var size = SharedNavMapSystem.ChunkSize * tileSize;
        var minX = chunkOrigin.X * SharedNavMapSystem.ChunkSize * tileSize;
        var minY = chunkOrigin.Y * SharedNavMapSystem.ChunkSize * tileSize;
        return new Box2(minX, minY, minX + size, minY + size).Intersects(view);
    }
    // ADT-Tweak End


    private void UpdateNavMapFloorTiles()
    {
        if (_fixtures == null)
            return;

        var verts = new Vector2[8];

        foreach (var fixture in _fixtures.Fixtures.Values)
        {
            if (fixture.Shape is not PolygonShape poly)
                continue;

            for (var i = 0; i < poly.VertexCount; i++)
            {
                var vert = poly.Vertices[i];
                verts[i] = new Vector2(MathF.Round(vert.X), MathF.Round(vert.Y));
            }

            TilePolygons.Add((verts[..poly.VertexCount], TileColor));
        }
    }

    private void UpdateNavMapWallLines()
    {
        if (_navMap == null || _grid == null)
            return;

        // We'll use the following dictionaries to combine collinear wall lines
        _horizLines.Clear();
        _horizLinesReversed.Clear();
        _vertLines.Clear();
        _vertLinesReversed.Clear();

        const int southMask = (int) AtmosDirection.South << (int) NavMapChunkType.Wall;     // ADT-Tweak - New Monitor
        const int eastMask = (int) AtmosDirection.East << (int) NavMapChunkType.Wall;
        const int westMask = (int) AtmosDirection.West << (int) NavMapChunkType.Wall;
        const int northMask = (int) AtmosDirection.North << (int) NavMapChunkType.Wall;

        // ADT-Tweak Start - New Monitor
        var view = GetViewBoundsLocal();
        var tileSize = (float) _grid.TileSize;
        // ADT-Tweak End

        foreach (var (chunkOrigin, chunk) in _navMap.Chunks)
        {
            // ADT-Tweak Start - New Monitor: Intersection
            if (!ChunkIntersectsView(chunkOrigin, tileSize, view))
                continue;
            // ADT-Tweak End


            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & SharedNavMapSystem.WallMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) NavMapChunkType.Wall;

                var relativeTile = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * _grid.TileSize;

                if (tileData != SharedNavMapSystem.AllDirMask)
                {
                    AddRectForThinWall(tileData, tile);
                    continue;
                }

                tile = tile with { Y = -tile.Y };
                NavMapChunk? neighborChunk;

                // North edge
                var neighborData = 0;
                if (relativeTile.Y != SharedNavMapSystem.ChunkSize - 1)
                    neighborData = chunk.TileData[i+1];
                else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Up, out neighborChunk))
                    neighborData = neighborChunk.TileData[i + 1 - SharedNavMapSystem.ChunkSize];

                if ((neighborData & southMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize),
                        tile + new Vector2i(_grid.TileSize, -_grid.TileSize), _horizLines,
                        _horizLinesReversed);
                }

                // East edge
                neighborData = 0;
                if (relativeTile.X != SharedNavMapSystem.ChunkSize - 1)
                    neighborData = chunk.TileData[i + SharedNavMapSystem.ChunkSize];
                else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Right, out neighborChunk))
                    neighborData = neighborChunk.TileData[i + SharedNavMapSystem.ChunkSize - SharedNavMapSystem.ArraySize];

                if ((neighborData & westMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(_grid.TileSize, -_grid.TileSize),
                        tile + new Vector2i(_grid.TileSize, 0), _vertLines, _vertLinesReversed);
                }

                // South edge
                neighborData = 0;
                if (relativeTile.Y != 0)
                    neighborData = chunk.TileData[i - 1];
                else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Down, out neighborChunk))
                    neighborData = neighborChunk.TileData[i - 1 + SharedNavMapSystem.ChunkSize];

                if ((neighborData & northMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile, tile + new Vector2i(_grid.TileSize, 0), _horizLines,
                        _horizLinesReversed);
                }

                // West edge
                neighborData = 0;
                if (relativeTile.X != 0)
                    neighborData = chunk.TileData[i - SharedNavMapSystem.ChunkSize];
                else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Left, out neighborChunk))
                    neighborData = neighborChunk.TileData[i - SharedNavMapSystem.ChunkSize + SharedNavMapSystem.ArraySize];

                if ((neighborData & eastMask) == 0)
                {
                    AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize), tile, _vertLines,
                        _vertLinesReversed);
                }

                // Add a diagonal line for interiors. Unless there are a lot of double walls, there is no point combining these
                TileLines.Add((tile + new Vector2(0, -_grid.TileSize), tile + new Vector2(_grid.TileSize, 0)));
            }
        }

        // Record the combined lines
        foreach (var (origin, terminal) in _horizLines)
        {
            TileLines.Add((origin, terminal));
        }

        foreach (var (origin, terminal) in _vertLines)
        {
            TileLines.Add((origin, terminal));
        }
    }

    // ADT-Tweak Start - New Monitor
    /// <summary>
    /// Glass tiles: empty squares outlined only on exterior edges. Adjacent windows merge
    /// (shared edges disappear) into one continuous pane outline.
    /// </summary>
    private void UpdateNavMapWindows()
    {
        if (_navMap == null || _grid == null)
            return;

        _horizLines.Clear();
        _horizLinesReversed.Clear();
        _vertLines.Clear();
        _vertLinesReversed.Clear();

        const int southMask = (int) AtmosDirection.South << (int) NavMapChunkType.Window;
        const int eastMask = (int) AtmosDirection.East << (int) NavMapChunkType.Window;
        const int westMask = (int) AtmosDirection.West << (int) NavMapChunkType.Window;
        const int northMask = (int) AtmosDirection.North << (int) NavMapChunkType.Window;

        var view = GetViewBoundsLocal();
        var tileSize = (float) _grid.TileSize;

        foreach (var (chunkOrigin, chunk) in _navMap.Chunks)
        {
            if (!ChunkIntersectsView(chunkOrigin, tileSize, view))
                continue;

            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & SharedNavMapSystem.WindowMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) NavMapChunkType.Window;

                var relativeTile = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * _grid.TileSize;
                tile = tile with { Y = -tile.Y };
                NavMapChunk? neighborChunk;

                // Even partial windows contribute their blocked edges as outline segments.
                var drawNorth = (tileData & (int) AtmosDirection.North) != 0;
                var drawEast = (tileData & (int) AtmosDirection.East) != 0;
                var drawSouth = (tileData & (int) AtmosDirection.South) != 0;
                var drawWest = (tileData & (int) AtmosDirection.West) != 0;

                // North edge — omit when neighbor also has a north-facing / south glass edge
                if (drawNorth)
                {
                    var neighborData = 0;
                    if (relativeTile.Y != SharedNavMapSystem.ChunkSize - 1)
                        neighborData = chunk.TileData[i + 1];
                    else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Up, out neighborChunk))
                        neighborData = neighborChunk.TileData[i + 1 - SharedNavMapSystem.ChunkSize];

                    if ((neighborData & southMask) == 0)
                    {
                        AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize),
                            tile + new Vector2i(_grid.TileSize, -_grid.TileSize), _horizLines,
                            _horizLinesReversed);
                    }
                }

                if (drawEast)
                {
                    var neighborData = 0;
                    if (relativeTile.X != SharedNavMapSystem.ChunkSize - 1)
                        neighborData = chunk.TileData[i + SharedNavMapSystem.ChunkSize];
                    else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Right, out neighborChunk))
                        neighborData = neighborChunk.TileData[i + SharedNavMapSystem.ChunkSize - SharedNavMapSystem.ArraySize];

                    if ((neighborData & westMask) == 0)
                    {
                        AddOrUpdateNavMapLine(tile + new Vector2i(_grid.TileSize, -_grid.TileSize),
                            tile + new Vector2i(_grid.TileSize, 0), _vertLines, _vertLinesReversed);
                    }
                }

                if (drawSouth)
                {
                    var neighborData = 0;
                    if (relativeTile.Y != 0)
                        neighborData = chunk.TileData[i - 1];
                    else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Down, out neighborChunk))
                        neighborData = neighborChunk.TileData[i - 1 + SharedNavMapSystem.ChunkSize];

                    if ((neighborData & northMask) == 0)
                    {
                        AddOrUpdateNavMapLine(tile, tile + new Vector2i(_grid.TileSize, 0), _horizLines,
                            _horizLinesReversed);
                    }
                }

                if (drawWest)
                {
                    var neighborData = 0;
                    if (relativeTile.X != 0)
                        neighborData = chunk.TileData[i - SharedNavMapSystem.ChunkSize];
                    else if (_navMap.Chunks.TryGetValue(chunkOrigin + Vector2i.Left, out neighborChunk))
                        neighborData = neighborChunk.TileData[i - SharedNavMapSystem.ChunkSize + SharedNavMapSystem.ArraySize];

                    if ((neighborData & eastMask) == 0)
                    {
                        AddOrUpdateNavMapLine(tile + new Vector2i(0, -_grid.TileSize), tile, _vertLines,
                            _vertLinesReversed);
                    }
                }
            }
        }

        foreach (var (origin, terminal) in _horizLines)
            WindowLines.Add((origin, terminal));

        foreach (var (origin, terminal) in _vertLines)
            WindowLines.Add((origin, terminal));
    }
    // ADT-Tweak End

    private void UpdateNavMapAirlocks()
    {
        if (_navMap == null || _grid == null)
            return;

        // ADT-Tweak Start - New Monitor
        var view = GetViewBoundsLocal();
        var tileSize = (float) _grid.TileSize;
        // ADT-Tweak End

        foreach (var (chunkOrigin, chunk) in _navMap.Chunks) // ADT-Tweak - New Monitor
        {
            // ADT-Tweak Start - New Monitor: Intersection
            if (!ChunkIntersectsView(chunkOrigin, tileSize, view))
                continue;
            // ADT-Tweak End

            for (var i = 0; i < SharedNavMapSystem.ArraySize; i++)
            {
                var tileData = chunk.TileData[i] & SharedNavMapSystem.AirlockMask;
                if (tileData == 0)
                    continue;

                tileData >>= (int) NavMapChunkType.Airlock;

                var relative = SharedNavMapSystem.GetTileFromIndex(i);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relative) * _grid.TileSize;

                // If the edges of an airlock tile are not all occupied, draw a thin airlock for each edge
                if (tileData != SharedNavMapSystem.AllDirMask)
                {
                    AddRectForThinAirlock(tileData, tile);
                    continue;
                }

                // Otherwise add a single full tile airlock
                TileRects.Add((new Vector2(tile.X + FullWallInstep, -tile.Y - FullWallInstep),
                    new Vector2(tile.X - FullWallInstep + 1f, -tile.Y + FullWallInstep - 1)));

                TileLines.Add((new Vector2(tile.X + 0.5f, -tile.Y - FullWallInstep),
                    new Vector2(tile.X + 0.5f, -tile.Y + FullWallInstep - 1)));
            }
        }
    }

    private void AddRectForThinWall(int tileData, Vector2i tile)
    {
        var leftTop = new Vector2(-0.5f, 0.5f - ThinWallThickness);
        var rightBottom = new Vector2(0.5f, 0.5f);

        for (var i = 0; i < SharedNavMapSystem.Directions; i++)
        {
            var dirMask = 1 << i;
            if ((tileData & dirMask) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);

            // TODO NAVMAP
            // Consider using faster rotation operations, given that these are always 90 degree increments
            var angle = -((AtmosDirection) dirMask).ToAngle();
            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
        }
    }

    private void AddRectForThinAirlock(int tileData, Vector2i tile)
    {
        var leftTop = new Vector2(-0.5f + FullWallInstep, 0.5f - FullWallInstep - ThinDoorThickness);
        var rightBottom = new Vector2(0.5f - FullWallInstep, 0.5f - FullWallInstep);
        var centreTop = new Vector2(0f, 0.5f - FullWallInstep - ThinDoorThickness);
        var centreBottom = new Vector2(0f, 0.5f - FullWallInstep);

        for (var i = 0; i < SharedNavMapSystem.Directions; i++)
        {
            var dirMask = 1 << i;
            if ((tileData & dirMask) == 0)
                continue;

            var tilePosition = new Vector2(tile.X + 0.5f, -tile.Y - 0.5f);
            var angle = -((AtmosDirection) dirMask).ToAngle();
            TileRects.Add((angle.RotateVec(leftTop) + tilePosition, angle.RotateVec(rightBottom) + tilePosition));
            TileLines.Add((angle.RotateVec(centreTop) + tilePosition, angle.RotateVec(centreBottom) + tilePosition));
        }
    }

    protected void AddOrUpdateNavMapLine(
        Vector2i origin,
        Vector2i terminus,
        Dictionary<Vector2i, Vector2i> lookup,
        Dictionary<Vector2i, Vector2i> lookupReversed)
    {
        Vector2i foundTermius;
        Vector2i foundOrigin;

        if (origin == terminus)
            return;

        // Does our new line end at the beginning of an existing line?
        if (lookup.Remove(terminus, out foundTermius))
        {
            DebugTools.Assert(lookupReversed[foundTermius] == terminus);

            // Does our new line start at the end of an existing line?
            if (lookupReversed.Remove(origin, out foundOrigin))
            {
                // Our new line just connects two existing lines
                DebugTools.Assert(lookup[foundOrigin] == origin);
                lookup[foundOrigin] = foundTermius;
                lookupReversed[foundTermius] = foundOrigin;
            }
            else
            {
                // Our new line precedes an existing line, extending it further to the left
                lookup[origin] = foundTermius;
                lookupReversed[foundTermius] = origin;
            }
            return;
        }

        // Does our new line start at the end of an existing line?
        if (lookupReversed.Remove(origin, out foundOrigin))
        {
            // Our new line just extends an existing line further to the right
            DebugTools.Assert(lookup[foundOrigin] == origin);
            lookup[foundOrigin] = terminus;
            lookupReversed[terminus] = foundOrigin;
            return;
        }

        // Completely disconnected line segment.
        lookup.Add(origin, terminus);
        lookupReversed.Add(terminus, origin);
    }

    protected Vector2 GetOffset()
    {
        return Offset + (_physics?.LocalCenter ?? new Vector2());
    }
}

public struct NavMapBlip
{
    public EntityCoordinates Coordinates;
    public Texture Texture;
    public Color Color;
    public bool Blinks;
    public bool Selectable;
    public float Scale;

    public NavMapBlip(EntityCoordinates coordinates, Texture texture, Color color, bool blinks, bool selectable = true, float scale = 1f)
    {
        Coordinates = coordinates;
        Texture = texture;
        Color = color;
        Blinks = blinks;
        Selectable = selectable;
        Scale = scale;
    }
}
