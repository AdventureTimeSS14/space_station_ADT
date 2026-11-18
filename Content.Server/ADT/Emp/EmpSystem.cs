using Content.Shared.ADT.Emp;
using Content.Shared.Emp;

namespace Content.Server.Emp;

public sealed partial class EmpSystem
{
    private void InitializeADT()
    {
        SubscribeLocalEvent<EmpGlitchComponent, EmpPulseEvent>(OnGlitchEmp);
    }

    private void OnGlitchEmp(Entity<EmpGlitchComponent> ent, ref EmpPulseEvent args)
    {
        var ev = new DoEmpGlitchMessage((float)args.Duration.TotalSeconds * ent.Comp.DurationMultiplier, ent.Comp.MotionStrength, ent.Comp.Smear, ent.Comp.Glitch);
        RaiseNetworkEvent(ev, ent.Owner);
    }
}
