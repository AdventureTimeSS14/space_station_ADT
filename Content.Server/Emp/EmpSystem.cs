using Content.Server.ADT.EMP;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.SurveillanceCamera;
using Content.Shared.Emp;
using Content.Shared.Projectiles;

namespace Content.Server.Emp;

public sealed class EmpSystem : SharedEmpSystem
{
    [Dependency] private readonly ChargerSystem _charger = default!;    // ADT-Tweak

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpDisabledComponent, RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
        SubscribeLocalEvent<EmpDisabledComponent, ApcToggleMainBreakerAttemptEvent>(OnApcToggleMainBreaker);
        SubscribeLocalEvent<EmpDisabledComponent, SurveillanceCameraSetActiveAttemptEvent>(OnCameraSetActive);

        SubscribeLocalEvent<EmpOnCollideComponent, ProjectileHitEvent>(OnProjectileHit); // ADT-Tweak
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

    // ADT-Tweak-Start
    private void OnProjectileHit(EntityUid uid, EmpOnCollideComponent component, ref ProjectileHitEvent args)
    {
        TryEmpEffects(args.Target, component.EnergyConsumption, TimeSpan.FromSeconds(component.DisableDuration));

        if (_charger.SearchForBattery(args.Target, out var batteryEnt, out _))
            TryEmpEffects(batteryEnt.Value, component.EnergyConsumption, TimeSpan.FromSeconds(component.DisableDuration));
    }
    // ADT-Tweak-End
}
