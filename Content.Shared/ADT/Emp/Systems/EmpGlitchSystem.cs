using Content.Shared.Emp;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.EMP;

public sealed class EmpGlitchSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpGlitchComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(Entity<EmpGlitchComponent> ent, ref EmpPulseEvent args)
    {
        ent.Comp.GlitchStart = _timing.CurTime;
        ent.Comp.GlitchEnd = _timing.CurTime + args.Duration * ent.Comp.DurationMultiplier;
        Dirty(ent);
    }
}
