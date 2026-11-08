using Robust.Client.Graphics;

namespace Content.Client.ADT.Mirror;

public sealed partial class MirrorSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new MirrorOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<MirrorOverlay>();
    }

    public bool CanBeSeenInMirrors(EntityUid uid)
    {
        var ev = new CanBeSeenInMirrorsEvent();
        RaiseLocalEvent(uid, ref ev);

        return !ev.Cancelled;
    }
}
