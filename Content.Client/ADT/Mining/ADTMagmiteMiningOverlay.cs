using System.Numerics;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Mining.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Mining;

public sealed class ADTMagmiteMiningOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _xform;

    private readonly EntityQuery<SpriteComponent> _spriteQuery;
    private readonly EntityQuery<TransformComponent> _xformQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => false;

    private readonly HashSet<Entity<MiningScannerViewableComponent>> _viewableEnts = new();

    public ADTMagmiteMiningOverlay()
    {
        IoCManager.InjectDependencies(this);

        _lookup = _entity.System<EntityLookupSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _xform = _entity.System<TransformSystem>();

        _spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        _xformQuery = _entity.GetEntityQuery<TransformComponent>();

        ZIndex = 100;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        if (_player.LocalEntity is not { } player ||
            !_entity.TryGetComponent<ADTMagmiteScannerViewerComponent>(player, out var viewer))
            return;

        var origin = _xform.GetMapCoordinates(player);

        if (origin.MapId != args.MapId)
            return;

        var scaleMatrix = Matrix3Helpers.CreateScale(Vector2.One);

        _viewableEnts.Clear();
        _lookup.GetEntitiesInRange(origin, viewer.Range, _viewableEnts);

        foreach (var ore in _viewableEnts)
        {
            if (!_xformQuery.TryComp(ore, out var xform) ||
                !_spriteQuery.TryComp(ore, out var sprite))
                continue;

            if (xform.MapID != args.MapId || !sprite.Visible)
                continue;

            if (!_sprite.LayerMapTryGet((ore, sprite), MiningScannerVisualLayers.Overlay, out var idx, false))
                continue;

            var layer = sprite[idx];

            if (layer.ActualRsi?.Path == null || layer.RsiState.Name == null)
                continue;

            var gridRot = xform.GridUid == null ? 0 : _xformQuery.CompOrNull(xform.GridUid.Value)?.LocalRotation ?? 0;
            var rotationMatrix = Matrix3Helpers.CreateRotation(gridRot);

            var worldMatrix = Matrix3Helpers.CreateTranslation(_xform.GetWorldPosition(xform));
            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matty = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
            handle.SetTransform(matty);

            var spriteSpec = new SpriteSpecifier.Rsi(layer.ActualRsi.Path, layer.RsiState.Name);
            var texture = _sprite.GetFrame(spriteSpec, TimeSpan.FromSeconds(layer.AnimationTime));

            handle.DrawTexture(texture, -(Vector2)texture.Size / 2f / EyeManager.PixelsPerMeter, layer.Rotation);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
