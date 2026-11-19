using Content.Client.ADT.Chaplain.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.ADT.Chaplain;

public sealed class HolySuppressionOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;

    private HolySuppressionOverlay _suppressionOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _suppressionOverlay = new HolySuppressionOverlay();
        _overlay.AddOverlay(_suppressionOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_suppressionOverlay);
    }
}
