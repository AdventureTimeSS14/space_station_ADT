using System.Numerics;
using Content.Shared.ADT.Drawing;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingFrameVisualizerSystem : VisualizerSystem<PaintingFrameComponent>
{
    private const string PaintingLayer = "painting";
    private const string CoverLayer = "cover";
    private const float PixelsPerMeter = 32f;

    [Dependency] private readonly CanvasViewSystem _view = default!;

    public override void Initialize()
    {
        base.Initialize();

        _view.ThumbnailChanged += OnThumbnailChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _view.ThumbnailChanged -= OnThumbnailChanged;
    }

    protected override void OnAppearanceChange(EntityUid uid, PaintingFrameComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        Apply(uid, component, args.Sprite, args.Component);
    }

    private void OnThumbnailChanged(Guid painting)
    {
        var key = painting.ToString();
        var query = EntityQueryEnumerator<PaintingFrameComponent, SpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var frame, out var sprite, out var appearance))
        {
            if (AppearanceSystem.TryGetData<string>(uid, PaintingFrameVisuals.Painting, out var current, appearance) &&
                current == key)
            {
                Apply(uid, frame, sprite, appearance);
            }
        }
    }

    private void Apply(EntityUid uid, PaintingFrameComponent frame, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var ent = (uid, sprite);
        if (!SpriteSystem.LayerMapTryGet(ent, PaintingLayer, out var paintingLayer, false) ||
            !SpriteSystem.LayerMapTryGet(ent, CoverLayer, out var coverLayer, false))
        {
            return;
        }

        AppearanceSystem.TryGetData<string>(uid, PaintingFrameVisuals.Painting, out var key, appearance);
        AppearanceSystem.TryGetData<bool>(uid, PaintingFrameVisuals.Hidden, out var hidden, appearance);

        if (string.IsNullOrEmpty(key) || !Guid.TryParse(key, out var painting))
        {
            SpriteSystem.LayerSetVisible(ent, paintingLayer, false);
            SpriteSystem.LayerSetVisible(ent, coverLayer, false);
            return;
        }

        if (hidden || !_view.TryGetThumbnail(painting, out var texture))
        {
            SpriteSystem.LayerSetVisible(ent, paintingLayer, false);
            SpriteSystem.LayerSetVisible(ent, coverLayer, true);
            return;
        }

        var scale = MathF.Min(frame.InnerSize.X / texture.Width, frame.InnerSize.Y / texture.Height);
        SpriteSystem.LayerSetTexture(ent, paintingLayer, texture);
        SpriteSystem.LayerSetScale(ent, paintingLayer, new Vector2(scale, scale));
        SpriteSystem.LayerSetOffset(ent, paintingLayer, frame.InnerOffset / PixelsPerMeter);
        SpriteSystem.LayerSetVisible(ent, paintingLayer, true);
        SpriteSystem.LayerSetVisible(ent, coverLayer, false);
    }
}
