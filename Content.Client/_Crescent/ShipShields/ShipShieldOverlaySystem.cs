using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._Crescent.ShipShields;

public sealed partial class ShipShieldOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new ShipShieldOverlay(EntityManager, _prototypeManager)); // LuaM: use shader-based shuttle shield visual
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<ShipShieldOverlay>();
    }
}
