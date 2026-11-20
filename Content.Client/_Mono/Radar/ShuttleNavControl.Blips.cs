using System.Numerics;
using Content.Client._Mono.Radar;
using Content.Shared._Crescent.ShipShields;
using Content.Shared._Mono.Radar;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Timing;

namespace Content.Client.Shuttles.UI;

// ADT-Tweak: Mono radar blips, missile/hitscan lines and ship shields on the stock radar.
public partial class ShuttleNavControl
{
    private RadarBlipsSystem _blips = default!;

    /// <summary>
    /// World to control space transform of the last drawn frame.
    /// </summary>
    protected Matrix3x2 WorldToView;

    private void InitializeBlips()
    {
        _blips = EntManager.System<RadarBlipsSystem>();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_consoleEntity != null)
            _blips.RequestBlips(_consoleEntity.Value);
    }

    private void DrawBlips(DrawingHandleScreen handle, TransformComponent xform, Angle viewRotation)
    {
        var viewBounds = new Box2(-3f, -3f, PixelSize.X + 3f, PixelSize.Y + 3f);
        var ourGrid = xform.GridUid;

        DrawShields(handle, xform);

        foreach (var blip in _blips.GetCurrentBlips())
        {
            // blips on grids hidden from radar are hidden with them
            if (blip.GridUid is { } grid && grid != ourGrid && !_shuttles.CanDraw(grid))
                continue;

            var position = Vector2.Transform(_transform.ToMapCoordinates(blip.Position).Position, WorldToView);
            if (!viewBounds.Contains(position))
                continue;

            var box = new Box2Rotated(blip.Config.Bounds, 0);
            if (blip.Config.RespectZoom)
                box.Box = new Box2(box.Box.BottomLeft * MinimapScale, box.Box.TopRight * MinimapScale);
            if (blip.Config.Rotate)
                box.Rotation = viewRotation - blip.Rotation;

            DrawBlipShape(handle, position, box, blip.Config.Color.WithAlpha(0.8f), blip.Config.Shape);
        }

        foreach (var line in _blips.GetMissileLines())
        {
            var start = Vector2.Transform(new Vector2(line.PositionStart.X, line.PositionStart.Y), WorldToView);
            var end = Vector2.Transform(new Vector2(line.PositionEnd.X, line.PositionEnd.Y), WorldToView);

            if (viewBounds.Contains(start) || viewBounds.Contains(end))
                handle.DrawLine(start, end, line.Color);
        }

        foreach (var line in _blips.GetHitscanLines())
        {
            var start = Vector2.Transform(line.Start, WorldToView);
            var end = Vector2.Transform(line.End, WorldToView);

            if (!viewBounds.Contains(start) && !viewBounds.Contains(end))
                continue;

            handle.DrawLine(start, end, line.Color);

            // For thicker lines, draw multiple lines side by side
            if (line.Thickness <= 1f)
                continue;

            var perpendicular = new Vector2(-(end - start).Normalized().Y, (end - start).Normalized().X) * 0.5f;
            for (var i = 1f; i <= line.Thickness; i += 1f)
            {
                var offset = perpendicular * i;
                handle.DrawLine(start + offset, end + offset, line.Color);
                handle.DrawLine(start - offset, end - offset, line.Color);
            }
        }
    }

    private void DrawShields(DrawingHandleScreen handle, TransformComponent consoleXform)
    {
        var shields = EntManager.AllEntityQueryEnumerator<ShipShieldVisualsComponent, FixturesComponent, TransformComponent>();
        while (shields.MoveNext(out var uid, out var visuals, out var fixtures, out var xform))
        {
            if (xform.MapID != consoleXform.MapID
                || xform.GridUid is not { } grid
                || EntManager.HasComponent<FTLComponent>(grid)
                || grid != consoleXform.GridUid && !_shuttles.CanDraw(grid))
                continue;

            if (!fixtures.Fixtures.TryGetValue("shield", out var fixture) || fixture.Shape is not ChainShape chain)
                continue;

            var shieldToView = _transform.GetWorldMatrix(uid) * WorldToView;
            for (var i = 1; i < chain.Count; i++)
            {
                handle.DrawLine(
                    Vector2.Transform(chain.Vertices[i - 1], shieldToView),
                    Vector2.Transform(chain.Vertices[i], shieldToView),
                    visuals.ShieldColor);
            }
        }
    }

    private void DrawBlipShape(DrawingHandleScreen handle, Vector2 position, Box2Rotated bounds, Color color, RadarBlipShape shape)
    {
        switch (shape)
        {
            case RadarBlipShape.Circle:
                DrawCircle(handle, position, bounds, color);
                break;
            case RadarBlipShape.Square:
                var boxPoints = new Vector2[]
                {
                    position + bounds.TopLeft,
                    position + bounds.TopRight,
                    position + bounds.BottomRight,
                    position + bounds.BottomLeft
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, boxPoints, color);
                break;
            case RadarBlipShape.Triangle:
                var points = new Vector2[]
                {
                    position + bounds.Origin + bounds.Rotation.RotateVec((bounds.TopLeft + bounds.TopRight) * 0.5f - bounds.Origin),
                    position + bounds.BottomLeft,
                    position + bounds.BottomRight
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, points, color);
                break;
            case RadarBlipShape.Star:
                DrawStar(handle, position, bounds, color);
                break;
            case RadarBlipShape.Diamond:
                var size = GetSize(bounds);
                var diamondPoints = new Vector2[]
                {
                    position + size.Offset + size.Top,
                    position + size.Offset + size.Left,
                    position + size.Offset - size.Top,
                    position + size.Offset - size.Left
                };
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, diamondPoints, color);
                break;
            case RadarBlipShape.Hexagon:
                DrawHexagon(handle, position, bounds, color);
                break;
            case RadarBlipShape.Arrow:
                DrawArrow(handle, position, bounds, color);
                break;
        }
    }

    private static (Vector2 Top, Vector2 Left, Vector2 Offset) GetSize(Box2Rotated bounds)
    {
        var top = (bounds.TopLeft + bounds.TopRight) * 0.5f;
        var left = (bounds.TopLeft + bounds.BottomLeft) * 0.5f;
        var offset = (bounds.TopRight + bounds.BottomLeft) * 0.5f;

        return (top - offset, left - offset, offset);
    }

    private static void DrawCircle(DrawingHandleScreen handle, Vector2 position, Box2Rotated bounds, Color color)
    {
        const int segments = 64;
        var buffer = new Vector2[segments + 1];
        var size = GetSize(bounds);
        var offsetPos = position + size.Offset;

        for (var i = 0; i <= segments; i++)
        {
            var angle = i * MathF.Tau / segments;
            buffer[i] = offsetPos + size.Left * MathF.Sin(angle) + size.Top * MathF.Cos(angle);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, buffer, color);
    }

    private static void DrawStar(DrawingHandleScreen handle, Vector2 position, Box2Rotated bounds, Color color)
    {
        const int points = 5;
        const float innerRatio = 0.4f;
        var vertices = new Vector2[points * 2];
        var size = GetSize(bounds);
        var offsetPos = position + size.Offset;

        for (var i = 0; i < points * 2; i++)
        {
            var angle = i * MathF.PI / points;
            var radius = i % 2 == 0 ? 1f : innerRatio;
            vertices[i] = offsetPos + radius * MathF.Sin(angle) * size.Left - radius * MathF.Cos(angle) * size.Top;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private static void DrawHexagon(DrawingHandleScreen handle, Vector2 position, Box2Rotated bounds, Color color)
    {
        var vertices = new Vector2[6];
        var size = GetSize(bounds);
        var offsetPos = position + size.Offset;

        for (var i = 0; i < 6; i++)
        {
            var angle = i * MathF.PI / 3;
            vertices[i] = offsetPos + MathF.Sin(angle) * size.Top - MathF.Cos(angle) * size.Left;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }

    private static void DrawArrow(DrawingHandleScreen handle, Vector2 position, Box2Rotated bounds, Color color)
    {
        var size = GetSize(bounds);
        var offsetPos = position + size.Offset;

        var vertices = new Vector2[]
        {
            offsetPos - size.Top,                    // Tip
            offsetPos + size.Left + size.Top * 0.5f, // Left wing
            offsetPos + size.Top,                    // Bottom
            offsetPos - size.Left + size.Top * 0.5f  // Right wing
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, vertices, color);
    }
}
