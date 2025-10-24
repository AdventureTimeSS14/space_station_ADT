using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Abilities;
using Robust.Client.GameObjects;
using System.Numerics;

namespace Content.Client.ADT.Overlays;

public sealed partial class SandevistanVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] IEntityManager _entityManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _sandevistanVisionShader;
    private readonly TransformSystem _transformSystem = default!;

    public SandevistanVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _sandevistanVisionShader = _prototypeManager.Index<ShaderPrototype>("SandevistanVision").Instance().Duplicate();
        _transformSystem = _entityManager.System<TransformSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<SandevistanVisionComponent>(player))
        {
            return false;
        }

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var eye = args.Viewport.Eye;

        if (eye == null)
            return;

        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        _sandevistanVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_sandevistanVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);

        if (_entityManager.TryGetComponent<SpriteComponent>(player.Value, out var playerSprite))
        {
            var playerXform = _entityManager.GetComponent<TransformComponent>(player.Value);
            var playerPos = _transformSystem.GetWorldPosition(playerXform);
            var playerRot = _transformSystem.GetWorldRotation(playerXform);

            worldHandle.UseShader(null);
            playerSprite.Render(worldHandle, eye.Rotation, playerRot, null, playerPos);
        }
    }
}
