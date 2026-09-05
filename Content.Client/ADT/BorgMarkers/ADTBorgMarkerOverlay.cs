using System.Numerics;
using Content.Shared.ADT.BorgMarkers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Client.ADT.BorgMarkers;

public sealed class ADTBorgMarkerOverlay : Overlay
{
    private const float HalfWidth = 0.52f;
    private const float HalfHeight = 0.78f;
    private const float ArmThickness = 0.26f;
    private const float InnerTipDrop = 1.75f;

    private static readonly Color Outline = Color.Black.WithAlpha(0.55f);

    private readonly IEntityManager _entMan;
    private readonly ISharedPlayerManager _player;
    private readonly SharedTransformSystem _transform;

    private readonly Vector2[] _vertices = new Vector2[12];

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public ADTBorgMarkerOverlay(IEntityManager entMan, ISharedPlayerManager player, SharedTransformSystem transform)
    {
        _entMan = entMan;
        _player = player;
        _transform = transform;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return false;

        return _player.LocalEntity is { } local && _entMan.HasComponent<ADTBorgMarkerViewerComponent>(local);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl is not { } viewport)
            return;

        if (_player.LocalEntity is not { } local ||
            !_entMan.TryGetComponent(local, out ADTBorgMarkerViewerComponent? viewer))
            return;

        var uiScale = (viewport as Control)?.UIScale ?? 1f;
        var bounds = args.ViewportBounds;

        var padding = viewer.ScreenPadding * uiScale;
        var size = viewer.ArrowSize * uiScale;

        var left = bounds.Left + padding;
        var top = bounds.Top + padding;
        var right = bounds.Right - padding;
        var bottom = bounds.Bottom - padding;

        if (right <= left || bottom <= top)
            return;

        var center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
        var extents = new Vector2((right - left) * 0.5f, (bottom - top) * 0.5f);

        var handle = args.ScreenHandle;
        var query = _entMan.EntityQueryEnumerator<ADTBorgMarkerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var marker, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var screen = viewport.WorldToScreen(_transform.GetWorldPosition(uid));

            Vector2 position;
            Vector2 direction;

            if (screen.X >= left && screen.X <= right && screen.Y >= top && screen.Y <= bottom)
            {
                direction = new Vector2(0f, 1f);
                position = screen - new Vector2(0f, size * 1.6f);
            }
            else
            {
                var delta = screen - center;
                if (delta.LengthSquared() < 0.0001f)
                    continue;

                direction = delta.Normalized();
                position = center + delta * EdgeScale(delta, extents);
            }

            DrawChevron(handle, position, direction, size * 1.22f, ArmThickness * 1.75f, Outline);
            DrawChevron(handle, position, direction, size, ArmThickness, marker.MarkerColor);
        }
    }

    private static float EdgeScale(Vector2 delta, Vector2 extents)
    {
        var scaleX = MathF.Abs(delta.X) > 0.0001f ? extents.X / MathF.Abs(delta.X) : float.MaxValue;
        var scaleY = MathF.Abs(delta.Y) > 0.0001f ? extents.Y / MathF.Abs(delta.Y) : float.MaxValue;

        return MathF.Min(scaleX, scaleY);
    }

    private void DrawChevron(
        DrawingHandleScreen handle,
        Vector2 position,
        Vector2 direction,
        float size,
        float thickness,
        Color color)
    {
        var halfWidth = HalfWidth * size;
        var halfHeight = HalfHeight * size;
        var arm = thickness * size;

        var outerLeft = new Vector2(-halfWidth, -halfHeight);
        var outerRight = new Vector2(halfWidth, -halfHeight);
        var tip = new Vector2(0f, halfHeight);

        var innerLeft = new Vector2(-halfWidth + arm, -halfHeight);
        var innerRight = new Vector2(halfWidth - arm, -halfHeight);
        var innerTip = new Vector2(0f, halfHeight - arm * InnerTipDrop);

        _vertices[0] = Place(position, direction, outerLeft);
        _vertices[1] = Place(position, direction, tip);
        _vertices[2] = Place(position, direction, innerTip);
        _vertices[3] = _vertices[0];
        _vertices[4] = _vertices[2];
        _vertices[5] = Place(position, direction, innerLeft);

        _vertices[6] = _vertices[1];
        _vertices[7] = Place(position, direction, outerRight);
        _vertices[8] = Place(position, direction, innerRight);
        _vertices[9] = _vertices[1];
        _vertices[10] = _vertices[8];
        _vertices[11] = _vertices[2];

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, _vertices, color);
    }

    private static Vector2 Place(Vector2 position, Vector2 direction, Vector2 point)
    {
        var side = new Vector2(direction.Y, -direction.X);

        return position + side * point.X + direction * point.Y;
    }
}
