using Content.Server.Emp;
using Content.Server.Stunnable;
using Content.Shared.Stunnable;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.ADT.Silicon.Systems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;

namespace Content.Server.ADT.Silicon.Systems;

public sealed class SiliconEmpSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private Content.Shared.StatusEffectNew.StatusEffectsSystem _statusNew = default!;

    private static readonly EntProtoId MuteEffect = "StatusEffectMuted";
    [Dependency] private StunSystem _stun = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StutteringSystem _stuttering = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnEmpPulse(EntityUid uid, SiliconComponent component, ref EmpPulseEvent args)
    {
        if (!TryComp<StatusEffectsComponent>(uid, out var statusComp))
            return;

        args.Affected = true;
        args.Disabled = true;

        var duration = args.Duration / 1.5; // We divide the duration since EMPs are balanced for structures, not people.

        if (duration.TotalSeconds * 0.25 >= 3) // If the EMP blast is strong enough, we stun them.
        // This is mostly to prevent flickering in/out of being stunned. We also cap how long they can be stunned for.
        {
            _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(Math.Min(duration.TotalSeconds * 0.25f, 15f)));
        }

        _status.TryAddStatusEffect<StunnedStatusEffectComponent>(uid, "SlowedDown", TimeSpan.FromSeconds(duration.TotalSeconds), false);

        _status.TryAddStatusEffect<SeeingStaticComponent>(uid, SharedSeeingStaticSystem.StaticKey, duration, true, statusComp);

        if (_random.Prob(0.8f))
            _statusNew.TryAddStatusEffectDuration(uid, SlurredSystem.Stutter, duration * 2);

        if (_random.Prob(0.6f))
            _stuttering.DoStutter(uid, duration * 2, false);

        if (_random.Prob(0.7f))
            _status.TryAddStatusEffect<PacifiedComponent>(uid, "Pacified", duration * 0.5, true, statusComp);

        if (_random.Prob(0.4f)) // Какие-то неадекватно низкие шансы тут, буквально 2-8 процентов. Ребят, это слишком мало для ЭМИ
            _statusNew.TryUpdateStatusEffectDuration(uid, MuteEffect, duration * 0.5);

        if (_random.Prob(0.3f))
            _statusNew.TryUpdateStatusEffectDuration(uid, BlindnessSystem.BlindingStatusEffect, duration * 0.5);

        _damage.TryChangeDamage(uid, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Shock"), _random.Next(20, 40)));

        args.EnergyConsumption = 0;
    }
}
