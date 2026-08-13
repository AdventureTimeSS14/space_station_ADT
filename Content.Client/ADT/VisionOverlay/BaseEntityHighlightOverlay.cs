using System.Collections.Generic;
using Content.Shared.Body;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
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

    public override bool RequestScreenTexture => false;

    private readonly List<(SpriteComponent.Layer Layer, ShaderInstance? Shader, Color Color)> _clearedLayers = new();
    private Color _savedSpriteColor;

    protected BaseEntityHighlightOverlay(ShaderPrototype shader) : base(shader)
    {
        _containerSystem = _entityManager.System<ContainerSystem>();
        _transform = _entityManager.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        worldHandle.UseShader(_shader);
        var query = _entityManager.EntityQueryEnumerator<BodyComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta))
                continue;

            PrepareSprite(sprite);
            try
            {
                worldHandle.UseShader(_shader);
                var (position, rotation) = _transform.GetWorldPositionRotation(xform);
                sprite.Render(worldHandle, eyeRotation, rotation, null, position);
            }
            finally
            {
                RestoreSprite(sprite);
            }

            worldHandle.UseShader(_shader);
        }

        worldHandle.UseShader(null);
    }

    private void PrepareSprite(SpriteComponent sprite)
    {
        _clearedLayers.Clear();
        _savedSpriteColor = sprite.Color;
        sprite.Color = Color.White;

        foreach (var spriteLayer in sprite.AllLayers)
        {
            if (spriteLayer is not SpriteComponent.Layer layer)
                continue;

            if (layer.Shader == null && layer.Color.Equals(Color.White))
                continue;

            _clearedLayers.Add((layer, layer.Shader, layer.Color));
            layer.Shader = null;
            layer.Color = Color.White;
        }
    }

    private void RestoreSprite(SpriteComponent sprite)
    {
        sprite.Color = _savedSpriteColor;

        foreach (var (layer, shader, color) in _clearedLayers)
        {
            layer.Shader = shader;
            layer.Color = color;
        }

        _clearedLayers.Clear();
    }
}
