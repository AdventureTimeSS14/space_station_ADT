using Content.Shared.ADT.ModSuits;
using Content.Shared.Clothing.Components;
using Content.Shared.Emp;
using Content.Shared.Inventory;
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
        if (TryComp<ClothingComponent>(ent, out var clothing) && !CanClothingGlitch((ent, clothing)))
            return;

        var now = _timing.CurTime;
        var end = now + args.Duration * ent.Comp.DurationMultiplier;
        if (end <= now)
            return;

        args.Affected = true;

        if (end <= ent.Comp.GlitchEnd)
            return;

        if (ent.Comp.GlitchEnd <= now)
            ent.Comp.GlitchStart = now;

        ent.Comp.GlitchEnd = end;
        Dirty(ent);
    }

    private bool CanClothingGlitch(Entity<ClothingComponent> clothing)
    {
        if (clothing.Comp.InSlotFlag != SlotFlags.HEAD)
            return false;

        if (!TryComp<ModPartComponent>(clothing, out var part))
            return true;

        return !HasComp<EmpContainerProtectionComponent>(GetEntity(part.AttachedUid));
    }
}
