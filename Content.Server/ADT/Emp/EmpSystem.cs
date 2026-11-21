using System.Linq;
using Content.Server.ADT.Emp.Components;
using Content.Shared.ADT.Emp;
using Content.Shared.Emp;
using Content.Shared.Speech.Muting;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;

namespace Content.Server.Emp;

public sealed partial class EmpSystem
{
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private IRobustRandom _random = default!;

    private void InitializeADT()
    {
        SubscribeLocalEvent<EmpSpeechComponent, EmpPulseEvent>(OnSpeechEmp);
        SubscribeLocalEvent<EmpSlowdownComponent, EmpPulseEvent>(OnSlowdownEmp);

        SubscribeLocalEvent<EmpGlitchComponent, EmpPulseEvent>(OnGlitchEmp);
    }

    private void OnSpeechEmp(Entity<EmpSpeechComponent> ent, ref EmpPulseEvent args)
    {
        var duration = MathF.Min((float)args.Duration.TotalSeconds * ent.Comp.DurationMultiplier, ent.Comp.MaxDuration);

        var list = EmpSpeechComponent.Accents.ToHashSet();
        for (var i = 0; i < ent.Comp.AccentsAmount; i++)
        {
            var picked = _random.Pick(list);
            list.Remove(picked);

            _statusEffect.TryAddStatusEffect(ent.Owner, picked, out _, TimeSpan.FromSeconds(duration));
        }
    }

    private void OnSlowdownEmp(Entity<EmpSlowdownComponent> ent, ref EmpPulseEvent args)
    {
        var duration = MathF.Min((float)args.Duration.TotalSeconds * ent.Comp.DurationMultiplier, ent.Comp.MaxDuration);

        _statusEffect.TryAddStatusEffect(ent.Owner, "ADTStatusEffectEmpSlowdown", out _, TimeSpan.FromSeconds(duration));
    }

    private void OnGlitchEmp(Entity<EmpGlitchComponent> ent, ref EmpPulseEvent args)
    {
        var ev = new DoEmpGlitchMessage((float)args.Duration.TotalSeconds * ent.Comp.DurationMultiplier, ent.Comp.MotionStrength, ent.Comp.Smear, ent.Comp.Glitch);
        RaiseNetworkEvent(ev, ent.Owner);
    }
}
