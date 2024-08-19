// Simple Station

using Content.Server.Emp;
using Content.Server.Speech.Muting;
using Content.Server.Stunnable;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.ADT.Silicon.Systems;

public sealed class SiliconEmpSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly SharedSlurredSystem _slurredSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(EntityUid uid, SiliconComponent component, ref EmpPulseEvent args)
    {
        args.EnergyConsumption *= 0.25f; // EMPs drain a lot of freakin power.

        if (!TryComp<StatusEffectsComponent>(uid, out var statusComp))
            return;

        args.Affected = true;
        args.Disabled = true;

        var duration = args.Duration / 1.5; // We divide the duration since EMPs are balanced for structures, not people.

        if (duration.TotalSeconds * 0.25 >= 3) // If the EMP blast is strong enough, we stun them.
        // This is mostly to prevent flickering in/out of being stunned. We also cap how long they can be stunned for.
        {
            _stun.TryParalyze(uid, TimeSpan.FromSeconds(Math.Min(duration.TotalSeconds * 0.25f, 15f)), true, statusComp);
        }

        _stun.TrySlowdown(uid, duration, true, _random.NextFloat(0.50f, 0.70f), _random.NextFloat(0.35f, 0.70f), statusComp);

        _status.TryAddStatusEffect<SeeingStaticComponent>(uid, SharedSeeingStaticSystem.StaticKey, duration, true, statusComp);

        if (_random.Prob(0.60f))
            _stuttering.DoStutter(uid, duration * 2, false, statusComp);
        else if (_random.Prob(0.80f))
            _slurredSystem.DoSlur(uid, duration * 2, statusComp);

        if (_random.Prob(0.02f))
            _status.TryAddStatusEffect<MutedComponent>(uid, "Muted", duration * 0.5, true, statusComp);

        if (_random.Prob(0.02f))
            _status.TryAddStatusEffect<TemporaryBlindnessComponent>(uid, TemporaryBlindnessSystem.BlindingStatusEffect, duration * 0.5, true, statusComp);

        if (_random.Prob(0.08f))
            _status.TryAddStatusEffect<PacifiedComponent>(uid, "Pacified", duration * 0.5, true, statusComp);

        args.EnergyConsumption = 0;
    }
}
