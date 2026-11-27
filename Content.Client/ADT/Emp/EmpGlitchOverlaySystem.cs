using Robust.Client.Graphics;

namespace Content.Client.ADT.Emp;

public sealed class EmpGlitchOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private EmpOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new EmpOverlay();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Dispose();
    }
}
