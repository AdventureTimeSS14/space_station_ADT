using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Chemistry.Components;
using Content.Shared.ADT.Chemistry.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.ADT.Chemistry.Systems;

public sealed class ADTLowPressureInjectorSystem : ADTSharedLowPressureInjectorSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLowPressureInjectorComponent, UseInHandEvent>(OnUseInHand,
            before: [typeof(InjectorSystem)]);
        SubscribeLocalEvent<ADTLowPressureInjectorComponent, AfterInteractEvent>(OnAfterInteract,
            before: [typeof(InjectorSystem)]);
        SubscribeLocalEvent<ADTLowPressureInjectorComponent, MeleeHitEvent>(OnMeleeHit,
            before: [typeof(InjectorSystem)]);
    }

    private void OnUseInHand(Entity<ADTLowPressureInjectorComponent> ent, ref UseInHandEvent args)
    {
        RefreshPressure(ent);
    }

    private void OnAfterInteract(Entity<ADTLowPressureInjectorComponent> ent, ref AfterInteractEvent args)
    {
        RefreshPressure(ent);
    }

    private void OnMeleeHit(Entity<ADTLowPressureInjectorComponent> ent, ref MeleeHitEvent args)
    {
        RefreshPressure(ent);
    }

    private void RefreshPressure(Entity<ADTLowPressureInjectorComponent> ent)
    {
        var pressure = _atmosphere.GetContainingMixture(ent.Owner)?.Pressure ?? 0f;

        SetLowPressure(ent, pressure > ent.Comp.MinPressure && pressure <= ent.Comp.MaxPressure);
    }
}
