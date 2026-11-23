using Content.Client.ADT.Emp;
using Content.Shared.ADT.Emp;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Emp;

public sealed partial class EmpSystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IPlayerManager _player = default!;
    private EmpOverlay _overlay = new();

    private void InitializeADT()
    {
        _overlayMan.AddOverlay(_overlay);

        SubscribeLocalEvent<EmpGlitchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmpGlitchComponent, LocalPlayerDetachedEvent>(OnDetach);

        SubscribeNetworkEvent<DoEmpGlitchMessage>(OnEmpGlitch);
    }

    private void OnShutdown(Entity<EmpGlitchComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.Stop();
    }

    private void OnDetach(Entity<EmpGlitchComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlay.Stop();
    }

    private void OnEmpGlitch(DoEmpGlitchMessage args)
    {
        _overlay.Stop();
        _overlay.DoEffect(args.Duration, args.MotionStrength, args.Smear, args.Glitch);
    }
}
