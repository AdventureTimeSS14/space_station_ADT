using Content.Client.ADT.VisionOverlay;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.ThermalVision;

public sealed class ThermalVisionEntityHighlightOverlay : BaseEntityHighlightOverlay
{
    public ThermalVisionEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        ZIndex = (int) OverlayZIndexes.ThermalVisionEntityHighlight;
    }

    protected override void DrawHighlights(in OverlayDrawArgs args, Angle eyeRotation)
    {
        var query = _entityManager.EntityQueryEnumerator<BodyComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta))
                continue;

            if (IsIgnored(uid))
                continue;

            DrawEntity(args, eyeRotation, sprite, xform);
        }
    }
}
