using System.Linq;
using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Client._Mono.Radar;
using Content.Shared._Mono.FireControl;
using Content.Shared.Physics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._Mono.FireControl.UI;

public sealed class FireControlNavControl : ShuttleNavControl
{
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SharedPhysicsSystem _physics;
    private readonly RadarBlipsSystem _blips;

    private FireControllableEntry[]? _controllables;
    private HashSet<NetEntity> _selectedWeapons = new();

    private readonly Dictionary<NetEntity, Color> _blipColors = new();

    // ADT-Tweak-Start: Mono kept the mouse state in ShuttleNavControl, here only the gunnery radar needs it
    private bool _isMouseDown;
    private bool _isMouseInside;
    private Vector2 _lastMousePos;
    private float _lastFireTime;
    private const float FireRateLimit = 0.1f; // 100ms between shots
    // ADT-Tweak-End

    // Add a limit to how often we update the cursor position to prevent network spam
    private float _lastCursorUpdateTime = 0f;
    private const float CursorUpdateInterval = 0.1f; // 10 updates per second

    public FireControlNavControl() : base(64f, 512f, 512f)
    {
        IoCManager.InjectDependencies(this);
        _blips = EntManager.System<RadarBlipsSystem>();
        _physics = EntManager.System<SharedPhysicsSystem>();
        _transform = EntManager.System<SharedTransformSystem>();

        OnMouseEntered += _ => _isMouseInside = true;
        OnMouseExited += _ => _isMouseInside = false;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _isMouseDown = true;
        _lastMousePos = args.RelativePosition;
        OnRadarClick?.Invoke(GetMouseEntityCoordinates(args.RelativePosition));
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
            _isMouseDown = false;

        // the base control raises OnRadarClick on release, which here is a cursor update
        base.KeyBindUp(args);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (!_isMouseInside)
            return;

        _lastMousePos = args.RelativePosition;
        // Continuously update the cursor position for guided missiles
        TryUpdateCursorPosition(_lastMousePos);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_isMouseDown || !_isMouseInside)
            return;

        var currentTime = (float) Timing.CurTime.TotalSeconds;
        if (currentTime - _lastFireTime < FireRateLimit)
            return;

        _lastMousePos = _uiManager.MousePositionScaled.Position - GlobalPosition;
        OnRadarClick?.Invoke(GetMouseEntityCoordinates(_lastMousePos));
        _lastFireTime = currentTime;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_coordinates == null || _rotation == null || _controllables == null)
            return;

        if (!EntManager.TryGetComponent<TransformComponent>(_coordinates.Value.EntityId, out var xform)
            || xform.MapID == MapId.Nullspace)
        {
            return;
        }

        _blipColors.Clear();
        foreach (var blip in _blips.GetCurrentBlips())
            _blipColors[blip.NetUid] = blip.Config.Color;

        Matrix3x2.Invert(WorldToView, out var viewToWorld);
        var cursorWorldPos = Vector2.Transform(_lastMousePos, viewToWorld);

        foreach (var controllable in _controllables)
        {
            if (!_selectedWeapons.Contains(controllable.NetEntity))
                continue;

            var coords = EntManager.GetCoordinates(controllable.Coordinates);
            var worldPos = _transform.ToMapCoordinates(coords).Position;

            var direction = cursorWorldPos - worldPos;
            var ray = new CollisionRay(worldPos, direction.Normalized(), (int) CollisionGroup.Impassable);

            var results = _physics.IntersectRay(xform.MapID, ray, direction.Length(), ignoredEnt: _coordinates?.EntityId);

            if ((!results.Any() || controllable.IgnoresLos) && _blipColors.TryGetValue(controllable.NetEntity, out var color))
                handle.DrawLine(Vector2.Transform(worldPos, WorldToView), _lastMousePos, color.WithAlpha(0.3f));
        }
    }

    public void UpdateControllables(EntityUid console, FireControllableEntry[] controllables)
    {
        _controllables = controllables;
    }

    public void UpdateSelectedWeapons(HashSet<NetEntity> selectedWeapons)
    {
        _selectedWeapons = selectedWeapons;
    }

    private void TryUpdateCursorPosition(Vector2 relativePosition)
    {
        var currentTime = (float) Timing.CurTime.TotalSeconds;
        if (currentTime - _lastCursorUpdateTime < CursorUpdateInterval)
            return;

        _lastCursorUpdateTime = currentTime;

        // This will update the server of our cursor position without triggering actual firing
        OnRadarClick?.Invoke(GetMouseEntityCoordinates(relativePosition));
    }

    /// <summary>
    /// Gets the entity coordinates of the given position, relative to the control.
    /// </summary>
    private EntityCoordinates GetMouseEntityCoordinates(Vector2 relativePosition)
    {
        if (_coordinates == null || _rotation == null)
            return EntityCoordinates.Invalid;

        var a = InverseScalePosition(relativePosition);
        var relativeWorldPos = _rotation.Value.RotateVec(new Vector2(a.X, -a.Y));
        return _coordinates.Value.Offset(relativeWorldPos);
    }

    /// <summary>
    /// Returns true if the mouse button is currently pressed down
    /// </summary>
    public bool IsMouseDown() => _isMouseDown;
}
