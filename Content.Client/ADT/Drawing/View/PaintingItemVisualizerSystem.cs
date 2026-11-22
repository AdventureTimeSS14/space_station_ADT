using System.Numerics;
using Content.Shared.ADT.Drawing;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Drawing.View;

public sealed class PaintingItemVisualizerSystem : EntitySystem
{
    private const string PaintingLayer = "painting";
    private const string BaseLayer = "base";
    private const float PixelsPerMeter = 32f;

    [Dependency] private readonly CanvasViewSystem _view = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaintingComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<PaintingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PaintingItemVisualsComponent, ComponentStartup>(OnStartup);
        _view.ThumbnailChanged += OnThumbnailChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _view.ThumbnailChanged -= OnThumbnailChanged;
    }

    private void OnState(Entity<PaintingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Apply(ent.Owner, ent.Comp);
    }

    private void OnShutdown(Entity<PaintingComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner) || !TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        var target = (ent.Owner, sprite);
        if (_sprite.LayerMapTryGet(target, PaintingLayer, out var layer, false))
            _sprite.LayerSetVisible(target, layer, false);

        if (_sprite.LayerMapTryGet(target, BaseLayer, out var baseLayer, false))
            _sprite.LayerSetVisible(target, baseLayer, true);
    }

    private void OnStartup(Entity<PaintingItemVisualsComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PaintingComponent>(ent.Owner, out var painting))
            Apply(ent.Owner, painting);
    }

    private void OnThumbnailChanged(Guid painting)
    {
        var query = EntityQueryEnumerator<PaintingComponent, PaintingItemVisualsComponent>();
        while (query.MoveNext(out var uid, out var comp, out _))
        {
            if (comp.PaintingUid == painting)
                Apply(uid, comp);
        }
    }

    private void Apply(EntityUid uid, PaintingComponent painting)
    {
        if (!TryComp<PaintingItemVisualsComponent>(uid, out var visuals) || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var ent = (uid, sprite);
        var hasBase = _sprite.LayerMapTryGet(ent, BaseLayer, out var baseLayer, false);

        if (painting.PaintingUid is not { } paintingUid || painting.Hidden || !_view.TryGetThumbnail(paintingUid, out var texture))
        {
            if (_sprite.LayerMapTryGet(ent, PaintingLayer, out var hiddenLayer, false))
                _sprite.LayerSetVisible(ent, hiddenLayer, false);

            if (hasBase)
                _sprite.LayerSetVisible(ent, baseLayer, true);

            return;
        }

        if (hasBase)
            _sprite.LayerSetVisible(ent, baseLayer, false);

        var layer = _sprite.LayerMapReserve(ent, PaintingLayer);
        var scale = MathF.Min(visuals.InnerSize.X / texture.Width, visuals.InnerSize.Y / texture.Height);
        _sprite.LayerSetTexture(ent, layer, texture);
        _sprite.LayerSetScale(ent, layer, new Vector2(scale, scale));
        _sprite.LayerSetOffset(ent, layer, visuals.InnerOffset / PixelsPerMeter);
        _sprite.LayerSetVisible(ent, layer, true);
    }
}
