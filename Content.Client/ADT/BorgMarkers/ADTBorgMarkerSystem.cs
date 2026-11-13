using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.ADT.BorgMarkers;

public sealed class ADTBorgMarkerSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private ADTBorgMarkerOverlay? _instance;

    public override void Initialize()
    {
        base.Initialize();

        _instance = new ADTBorgMarkerOverlay(EntityManager, _player, _transform);
        _overlay.AddOverlay(_instance);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_instance == null)
            return;

        _overlay.RemoveOverlay(_instance);
        _instance = null;
    }
}
