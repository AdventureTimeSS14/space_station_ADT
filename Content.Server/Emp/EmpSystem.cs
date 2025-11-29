using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.SurveillanceCamera;
using Content.Shared.Emp;

namespace Content.Server.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
        SubscribeLocalEvent<EmpDisabledComponent, SurveillanceCameraSetActiveAttemptEvent>(OnCameraSetActive);

        SubscribeLocalEvent<EmpOnCollideComponent, ProjectileHitEvent>(OnProjectileHit); ///ADT ion
    }

    private void OnRadioSendAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioSendAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(EntityUid uid, EmpDisabledComponent component, ref RadioReceiveAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnApcToggleMainBreaker(EntityUid uid, EmpDisabledComponent component, ref ApcToggleMainBreakerAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCameraSetActive(EntityUid uid, EmpDisabledComponent component, ref SurveillanceCameraSetActiveAttemptEvent args)
    {
        args.Cancelled = true;
    }

    ///ADT ion start
    private void OnProjectileHit(EntityUid uid, EmpOnCollideComponent component, ref ProjectileHitEvent args)
    {
        TryEmpEffects(args.Target, component.EnergyConsumption, component.DisableDuration);
        if (!TryComp<InventoryComponent>(args.Target, out var inventory))
            return;
        if (_charger.SearchForBattery(args.Target, out var batteryEnt, out var batteryComp))
            TryEmpEffects(batteryEnt.Value, component.EnergyConsumption, component.DisableDuration);
    }
    ///ADT ion end
}
