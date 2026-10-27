using System.Numerics;
using Content.Client.ADT.Drawing.View;
using Content.Shared.ADT.Drawing;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasEaselVisualizerSystem : VisualizerSystem<CanvasEaselComponent>
{
    private const string CanvasLayer = "canvas";
    private const string PaintingLayer = "painting";
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

    protected override void OnAppearanceChange(EntityUid uid, CanvasEaselComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        Apply(uid, args.Sprite, args.Component);
    }

    private void OnThumbnailChanged(Guid painting)
    {
        var key = painting.ToString();
        var query = EntityQueryEnumerator<CanvasEaselComponent, SpriteComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite, out var appearance))
        {
            if (AppearanceSystem.TryGetData<string>(uid, CanvasEaselVisuals.Painting, out var current, appearance) &&
                current == key)
            {
                Apply(uid, sprite, appearance);
            }
        }
    }

    private void Apply(EntityUid uid, SpriteComponent sprite, AppearanceComponent appearance)
    {
        var ent = (uid, sprite);
        if (!SpriteSystem.LayerMapTryGet(ent, CanvasLayer, out var layer, false))
            return;

        if (!AppearanceSystem.TryGetData<string>(uid, CanvasEaselVisuals.Canvas, out var state, appearance) ||
            string.IsNullOrEmpty(state))
        {
            SpriteSystem.LayerSetVisible(ent, layer, false);
            HidePainting(ent);
            return;
        }

        AppearanceSystem.TryGetData<Vector2>(uid, CanvasEaselVisuals.Offset, out var offset, appearance);
        SpriteSystem.LayerSetRsiState(ent, layer, state);
        SpriteSystem.LayerSetOffset(ent, layer, offset / PixelsPerMeter);

        if (AppearanceSystem.TryGetData<string>(uid, CanvasEaselVisuals.Painting, out var key, appearance) &&
            Guid.TryParse(key, out var painting) &&
            _view.TryGetThumbnail(painting, out var texture))
        {
            AppearanceSystem.TryGetData<Vector2>(uid, CanvasEaselVisuals.PaintingSize, out var size, appearance);
            AppearanceSystem.TryGetData<Vector2>(uid, CanvasEaselVisuals.PaintingOffset, out var paintingOffset, appearance);

            var paintingLayer = SpriteSystem.LayerMapReserve(ent, PaintingLayer);
            var scale = MathF.Min(size.X / texture.Width, size.Y / texture.Height);
            SpriteSystem.LayerSetTexture(ent, paintingLayer, texture);
            SpriteSystem.LayerSetScale(ent, paintingLayer, new Vector2(scale, scale));
            SpriteSystem.LayerSetOffset(ent, paintingLayer, paintingOffset / PixelsPerMeter);
            SpriteSystem.LayerSetVisible(ent, paintingLayer, true);
            SpriteSystem.LayerSetVisible(ent, layer, false);
            return;
        }

        HidePainting(ent);
        SpriteSystem.LayerSetVisible(ent, layer, true);
    }

    private void HidePainting(Entity<SpriteComponent?> ent)
    {
        if (SpriteSystem.LayerMapTryGet(ent, PaintingLayer, out var paintingLayer, false))
            SpriteSystem.LayerSetVisible(ent, paintingLayer, false);
    }
}
