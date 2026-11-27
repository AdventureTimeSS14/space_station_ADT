using Content.Server.ADT.Emp.Components;
using Content.Shared.Emp;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Emp;

public sealed class EmpStatusEffectsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;

    private static readonly EntProtoId SlowdownEffect = "ADTStatusEffectEmpSlowdown";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmpSpeechComponent, EmpPulseEvent>(OnSpeechEmp);
        SubscribeLocalEvent<EmpSlowdownComponent, EmpPulseEvent>(OnSlowdownEmp);
    }

    private void OnSpeechEmp(Entity<EmpSpeechComponent> ent, ref EmpPulseEvent args)
    {
        var duration = GetDuration(args.Duration, ent.Comp.DurationMultiplier, ent.Comp.MaxDuration);

        var accents = new List<EntProtoId>(ent.Comp.Accents);
        for (var i = 0; i < ent.Comp.AccentsAmount && accents.Count > 0; i++)
        {
            if (_statusEffect.TryUpdateStatusEffectDuration(ent, _random.PickAndTake(accents), duration))
                args.Affected = true;
        }
    }

    private void OnSlowdownEmp(Entity<EmpSlowdownComponent> ent, ref EmpPulseEvent args)
    {
        var duration = GetDuration(args.Duration, ent.Comp.DurationMultiplier, ent.Comp.MaxDuration);

        if (_statusEffect.TryUpdateStatusEffectDuration(ent, SlowdownEffect, duration))
            args.Affected = true;
    }

    private static TimeSpan GetDuration(TimeSpan duration, float multiplier, float max)
    {
        return TimeSpan.FromSeconds(MathF.Min((float)duration.TotalSeconds * multiplier, max));
    }
}
