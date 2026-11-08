using System.Numerics;
using Content.Shared.ADT.Segmented.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Segmented;

public sealed class ADTSegmentTetherOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly IEntityManager _entManager;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;
    private readonly ShaderInstance _unshaded;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public ADTSegmentTetherOverlay(IEntityManager entManager)
    {
        IoCManager.InjectDependencies(this);

        _entManager = entManager;
        _sprite = entManager.System<SpriteSystem>();
        _transform = entManager.System<TransformSystem>();
        _unshaded = _prototype.Index(UnshadedShader).Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var now = _timing.RealTime;
        var segmentQuery = _entManager.GetEntityQuery<ADTSegmentComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var unshaded = false;

        var query = _entManager.EntityQueryEnumerator<ADTSegmentTetherComponent, ADTSegmentComponent, TransformComponent>();
        while (query.MoveNext(out _, out var tether, out var segment, out var xform))
        {
            if (xform.MapID != args.MapId || segment.Leader is not { } leader)
                continue;

            if (!xformQuery.TryComp(leader, out var leaderXform) || leaderXform.MapID != args.MapId)
                continue;

            var leaderLift = 0f;

            if (segmentQuery.TryComp(leader, out var leaderSegment))
                leaderLift = leaderSegment.VisualLift;

            var start = _transform.GetWorldPosition(xform)
                + ToWorld(tether.SegmentOffset + new Vector2(0f, segment.VisualLift), eyeRotation);
            var end = _transform.GetWorldPosition(leaderXform)
                + ToWorld(tether.LeaderOffset + new Vector2(0f, leaderLift), eyeRotation);

            var texture = _sprite.GetFrame(tether.Sprite, now);
            var width = texture.Width / (float) EyeManager.PixelsPerMeter;
            var tile = texture.Height / (float) EyeManager.PixelsPerMeter;

            var box = new Box2(Vector2.Min(start, end), Vector2.Max(start, end)).Enlarged(width);

            if (!bounds.Intersects(box))
                continue;

            var direction = end - start;
            var length = direction.Length();

            if (length < 0.01f || tile <= 0f)
                continue;

            if (tether.Unshaded != unshaded)
            {
                handle.UseShader(tether.Unshaded ? _unshaded : null);
                unshaded = tether.Unshaded;
            }

            var normalized = direction / length;
            var angle = normalized.ToWorldAngle() + Angle.FromDegrees(180);
            var pieces = Math.Max(1, (int) MathF.Round(length / tile));
            var pieceLength = length / pieces;
            var size = new Vector2(width, pieceLength);

            for (var i = 0; i < pieces; i++)
            {
                var center = start + normalized * (pieceLength * (i + 0.5f));
                var quad = Box2.CenteredAround(center, size);
                handle.DrawTextureRect(texture, new Box2Rotated(quad, angle, center), tether.Color);
            }
        }

        if (unshaded)
            handle.UseShader(null);
    }

    private static Vector2 ToWorld(Vector2 screenOffset, Angle eyeRotation)
    {
        if (screenOffset == Vector2.Zero)
            return Vector2.Zero;

        return (-eyeRotation).RotateVec(screenOffset);
    }
}
