using Content.Shared.ADT.AnimatedTiles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.AnimatedTiles;

public sealed class AnimatedTileSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private AnimatedTileOverlay? _overlay;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new AnimatedTileOverlay(_sprite, _map, _transform);
        _overlayManager.AddOverlay(_overlay);

        _protoManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _protoManager.PrototypesReloaded -= OnPrototypesReloaded;

        if (_overlay != null)
            _overlayManager.RemoveOverlay(_overlay);

        _overlay = null;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<AnimatedTilePrototype>())
            return;

        _overlay?.BuildRegistry();
    }
}
