using System.Numerics;
using Content.Client.UserInterface.Systems;
using Content.Shared.ADT.Fishing.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Fishing.Overlays;

public sealed class FishingOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly IPlayerManager _player;
    private readonly SharedTransformSystem _transform;
    private readonly ProgressColorSystem _progressColor;

    private readonly Texture _barTexture;

    private const float StartYFraction = 0.09375f;
    private const float EndYFraction = 0.90625f;
    private const float BarWidthFraction = 0.2f;
    private const float BarScale = 1f;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public FishingOverlay(IEntityManager entManager, IPlayerManager player)
    {
        _entManager = entManager;
        _player = player;
        _transform = _entManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        _progressColor = _entManager.System<ProgressColorSystem>();

        var sprite = new SpriteSpecifier.Rsi(new("/Textures/ADT/Interface/Misc/fish_bar.rsi"), "icon");
        _barTexture = _entManager.EntitySysManager.GetEntitySystem<SpriteSystem>().Frame0(sprite);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var rotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        const float scale = 1f;
        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(scale, scale));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-rotation);

        var bounds = args.WorldAABB.Enlarged(5f);
        var localEnt = _player.LocalSession?.AttachedEntity;

        var textureSize = new Vector2(_barTexture.Width, _barTexture.Height) / EyeManager.PixelsPerMeter;

        var scaledTextureSize = textureSize * BarScale;

        var barWidth = scaledTextureSize.X * BarWidthFraction;

        var enumerator = _entManager.AllEntityQueryEnumerator<ActiveFisherComponent, SpriteComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId ||
                comp.TotalProgress == null ||
                comp.TotalProgress < 0 ||
                uid != localEnt)
                continue;

            var worldPosition = _transform.GetWorldPosition(xform, xformQuery);
            if (!bounds.Contains(worldPosition))
                continue;

            var worldMatrix = Matrix3Helpers.CreateTranslation(worldPosition);
            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
            handle.SetTransform(matty);

            var position = new Vector2(
                sprite.Bounds.Width / 2f,
                -scaledTextureSize.Y / 2f
            );

            handle.DrawTextureRect(_barTexture, new Box2(position, position + scaledTextureSize));

            var progress = Math.Clamp(comp.TotalProgress.Value, 0f, 1f);

            var startYPixel = scaledTextureSize.Y * StartYFraction;
            var endYPixel = scaledTextureSize.Y * EndYFraction;
            var yProgress = (endYPixel - startYPixel) * progress + startYPixel;

            var box = new Box2(
                new Vector2((scaledTextureSize.X - barWidth) / 2f, startYPixel),
                new Vector2((scaledTextureSize.X + barWidth) / 2f, yProgress)
            );

            box = box.Translated(position);

            var color = GetProgressColor(progress);
            handle.DrawRect(box, color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }

    public Color GetProgressColor(float progress, float alpha = 1f)
    {
        return _progressColor.GetProgressColor(progress).WithAlpha(alpha);
    }
}
