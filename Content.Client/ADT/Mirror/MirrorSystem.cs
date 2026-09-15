using Content.Shared.ADT.Mirror;
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

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<MirrorReflectionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var ev = new CanBeSeenInMirrorsEvent();
            RaiseLocalEvent(uid, ref ev);

            comp.Active = !ev.Cancelled;
        }
    }
}
