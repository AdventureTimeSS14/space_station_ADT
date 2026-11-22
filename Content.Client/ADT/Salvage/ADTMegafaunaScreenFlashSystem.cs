using System.Numerics;
using Content.Shared.ADT.Salvage;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Salvage;

public sealed class ADTMegafaunaScreenFlashSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlay = default!;

    private ADTMegafaunaScreenFlashOverlay _flash = default!;

    public override void Initialize()
    {
        base.Initialize();

        _flash = new ADTMegafaunaScreenFlashOverlay();
        _overlay.AddOverlay(_flash);

        SubscribeNetworkEvent<ADTMegafaunaScreenFlashEvent>(OnScreenFlash);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_flash);
    }

    private void OnScreenFlash(ADTMegafaunaScreenFlashEvent ev)
    {
        _flash.TintColor = new Vector3(ev.Color.R, ev.Color.G, ev.Color.B);
        _flash.StartTime = _timing.RealTime;
        _flash.Duration = TimeSpan.FromSeconds(ev.Duration);
    }
}
