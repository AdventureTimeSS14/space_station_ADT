using Content.Shared.ADT.VentCrawling;
using Content.Shared.Atmos.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.ADT.VentCrawling;

public sealed partial class VentCrawPipeOverlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly TransformSystem _transformSystem;
    private readonly SharedContainerSystem _containerSystem;

    private static readonly Color PipeGlowColor = new(0.6f, 0.85f, 1.0f, 1.0f);
    private static readonly Color CurrentPipeGlowColor = new(1.0f, 0.5f, 0.5f, 1.0f);
    private static readonly TimeSpan LookupInterval = TimeSpan.FromSeconds(0.1);
    private TimeSpan _lastLookup;
    private readonly HashSet<Entity<SpriteComponent>> _pipes = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public VentCrawPipeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entityManager.System<SpriteSystem>();
        _lookup = _entityManager.System<EntityLookupSystem>();
        _transformSystem = _entityManager.System<TransformSystem>();
        _containerSystem = _entityManager.System<SharedContainerSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;
        return player != null
            && _entityManager.TryGetComponent<VentCrawlerComponent>(player.Value, out var crawler)
            && crawler.InTube;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _playerManager.LocalSession!.AttachedEntity!.Value;

        EntityUid? currentTube = null;
        if (_containerSystem.TryGetContainingContainer(player, out var holderContainer))
        {
            var holder = holderContainer.Owner;
            if (_containerSystem.TryGetContainingContainer(holder, out var tubeContainer))
                currentTube = tubeContainer.Owner;
        }

        var worldHandle = args.WorldHandle;
        worldHandle.UseShader(null);

        if (_gameTiming.CurTime - _lastLookup > LookupInterval)
        {
            _lastLookup = _gameTiming.CurTime;
            _pipes.Clear();
            _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds, _pipes, LookupFlags.Uncontained);
        }

        var eyeRot = _entityManager.GetComponent<EyeComponent>(player).Rotation;

        foreach (var ent in _pipes)
        {
            var uid = ent.Owner;
            if (_entityManager.Deleted(uid) || !_entityManager.HasComponent<PipeAppearanceComponent>(uid))
                continue;

            var sprite = ent.Comp;
            if (!sprite.Visible)
                continue;

            var xform = _entityManager.GetComponent<TransformComponent>(uid);
            var worldPos = _transformSystem.GetWorldPosition(xform);
            var worldRot = _transformSystem.GetWorldRotation(xform);

            var color = uid == currentTube ? CurrentPipeGlowColor : PipeGlowColor;

            var oldColor = sprite.Color;
            _spriteSystem.SetColor((uid, sprite), color);
            _spriteSystem.RenderSprite((uid, sprite), worldHandle, eyeRot, worldRot, worldPos);
            _spriteSystem.SetColor((uid, sprite), oldColor);
        }
    }
}