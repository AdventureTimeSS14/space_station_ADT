using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.VisionOverlay;

/// <summary>
/// Draws body sprites through a shader (e.g. through-walls thermal highlight).
/// Ported from Starlight.
/// </summary>
public abstract partial class BaseEntityHighlightOverlay : BaseVisionOverlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    private readonly ContainerSystem _containerSystem;
    private readonly TransformSystem _transform;

    // Highlight redraws sprites; screen capture is only for the thermal LUT overlay.
    public override bool RequestScreenTexture => false;

    protected BaseEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        _containerSystem = _entityManager.System<ContainerSystem>();
        _transform = _entityManager.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        // Bodies are redrawn with BrightnessShader (through walls); no SCREEN_TEXTURE needed.
        var worldHandle = args.WorldHandle;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        worldHandle.UseShader(_shader);
        var query = _entityManager.EntityQueryEnumerator<BodyComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta))
                continue;

            var (position, rotation) = _transform.GetWorldPositionRotation(xform);
            sprite.Render(worldHandle, eyeRotation, rotation, null, position);
        }

        worldHandle.UseShader(null);
    }
}
