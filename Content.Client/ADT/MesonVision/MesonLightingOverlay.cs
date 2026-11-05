using System.Numerics;
using Content.Client.Light;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.MesonVision;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.ADT.MesonVision;

public sealed class MesonLightingOverlay : Overlay
{
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.BeforeLighting;

    public MesonLightingOverlay()
    {
        IoCManager.InjectDependencies(this);
        ZIndex = AfterLightTargetOverlay.ContentZIndex + 1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entity.TryGetComponent(_player.LocalEntity, out MesonVisionComponent? meson) ||
            meson.State != MesonVisionState.Full ||
            meson.LightingAlpha >= 1f)
            return false;

        if (!_entity.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) || args.Viewport.Eye != eye.Eye)
            return false;

        if (!_entity.TryGetComponent(_player.LocalEntity, out TransformComponent? xform)
            || !_entity.HasComponent<ADTLavalandMapComponent>(xform.MapUid))
        {
            return false;
        }

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;
        if (viewport.Eye == null ||
            !_entity.TryGetComponent(_player.LocalEntity, out MesonVisionComponent? meson))
            return;

        var handle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var lightScale = viewport.LightRenderTarget.Size / (Vector2) viewport.Size;
        var scale = viewport.RenderScale / (Vector2.One / lightScale);
        var localMatrix = viewport.LightRenderTarget.GetWorldToLocalMatrix(viewport.Eye, scale);
        var color = Color.White.WithAlpha(1f - meson.LightingAlpha);

        handle.RenderInRenderTarget(viewport.LightRenderTarget,
            () =>
            {
                handle.SetTransform(localMatrix);
                handle.DrawRect(bounds, color);
            },
            null);

        handle.SetTransform(Matrix3x2.Identity);
    }
}
