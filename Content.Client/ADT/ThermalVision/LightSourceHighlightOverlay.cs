using Content.Client.ADT.VisionOverlay;
using Content.Client.MapText;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

public sealed class LightSourceHighlightOverlay : BaseEntityHighlightOverlay
{
    public LightSourceHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.LightSourceHighlight;
    }

    protected override void DrawHighlights(in OverlayDrawArgs args, Angle eyeRotation)
    {
        var bounds = args.WorldAABB.Enlarged(1f);

        var query = _entityManager.EntityQueryEnumerator<PointLightComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var meta, out var sprite, out var xform))
        {
            if (_entityManager.HasComponent<MapTextComponent>(uid))
                continue;

            if (!light.Enabled || xform.MapID != args.MapId)
                continue;

            if (_containerSystem.IsEntityInContainer(uid, meta))
                continue;

            if (IsIgnored(uid))
                continue;

            if (!bounds.Contains(_transform.GetWorldPosition(xform)))
                continue;

            DrawEntity(args, eyeRotation, sprite, xform);
        }
    }
}
