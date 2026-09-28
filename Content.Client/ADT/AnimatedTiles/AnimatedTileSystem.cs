using Content.Shared.ADT.AnimatedTiles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.AnimatedTiles;

public sealed class AnimatedTileSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

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
