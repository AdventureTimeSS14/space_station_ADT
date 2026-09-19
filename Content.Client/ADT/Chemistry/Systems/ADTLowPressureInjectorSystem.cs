using Content.Shared.ADT.Chemistry.Components;
using Content.Shared.ADT.Chemistry.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Client.ADT.Chemistry.Systems;

public sealed class ADTLowPressureInjectorSystem : ADTSharedLowPressureInjectorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLowPressureInjectorComponent, UseInHandEvent>(OnUseInHand,
            before: new[] { typeof(InjectorSystem) });
        SubscribeLocalEvent<ADTLowPressureInjectorComponent, AfterInteractEvent>(OnAfterInteract,
            before: new[] { typeof(InjectorSystem) });
        SubscribeLocalEvent<ADTLowPressureInjectorComponent, MeleeHitEvent>(OnMeleeHit,
            before: new[] { typeof(InjectorSystem) });
    }

    private void OnUseInHand(Entity<ADTLowPressureInjectorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.ApplyDelay = false;
    }

    private void OnAfterInteract(Entity<ADTLowPressureInjectorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true })
            return;

        args.Handled = true;
    }

    private void OnMeleeHit(Entity<ADTLowPressureInjectorComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        args.HitEntities = Array.Empty<EntityUid>();
    }
}
