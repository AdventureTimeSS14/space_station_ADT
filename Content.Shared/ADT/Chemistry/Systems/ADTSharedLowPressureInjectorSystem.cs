using Content.Shared.ADT.Chemistry.Components;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.ADT.Chemistry.Systems;

public abstract class ADTSharedLowPressureInjectorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLowPressureInjectorComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTLowPressureInjectorComponent> ent, ref MapInitEvent args)
    {
        ApplyMode(ent);
    }

    protected void SetLowPressure(Entity<ADTLowPressureInjectorComponent> ent, bool value)
    {
        if (ent.Comp.InLowPressure == value)
            return;

        ent.Comp.InLowPressure = value;

        ApplyMode(ent);
    }

    private void ApplyMode(Entity<ADTLowPressureInjectorComponent> ent)
    {
        if (!TryComp<InjectorComponent>(ent, out var injector))
            return;

        var mode = ent.Comp.InLowPressure ? ent.Comp.LowPressureMode : ent.Comp.NormalMode;

        if (injector.ActiveModeProtoId == mode)
            return;

        injector.ActiveModeProtoId = mode;
        Dirty(ent.Owner, injector);
    }
}
