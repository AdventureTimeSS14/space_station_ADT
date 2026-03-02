using Content.Shared.ADT.Silicon;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Training;

public sealed class TrainingProgressSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    private const float Tier1Threshold = 0.40f;
    private const float Tier2Threshold = 0.70f;
    private const float Tier3Threshold = 1.00f;
    private const float StrengthPerAction = 0.02f;
    private const float StaminaBonusAmount = 25f;

    public void AddTrainingProgress(EntityUid performer)
    {
        if (HasComp<MobIpcComponent>(performer))
            return;

        if (!TryComp<PullerComponent>(performer, out var pullerComp))
            return;

        var training = EnsureComp<TrainingProgressComponent>(performer);
        var previousStrength = training.Strength;
        training.Strength += StrengthPerAction;

        float targetReduction;
        if (training.Strength >= Tier3Threshold)
            targetReduction = Tier3Threshold;
        else if (training.Strength >= Tier2Threshold)
            targetReduction = Tier2Threshold;
        else if (training.Strength >= Tier1Threshold)
            targetReduction = Tier1Threshold;
        else
            targetReduction = pullerComp.PulledDensityReduction;

        pullerComp.PulledDensityReduction = targetReduction;
        Dirty(performer, pullerComp);
        Dirty(performer, training);

        var crossedThreshold =
            (previousStrength < Tier1Threshold && training.Strength >= Tier1Threshold) ||
            (previousStrength < Tier2Threshold && training.Strength >= Tier2Threshold) ||
            (previousStrength < Tier3Threshold && training.Strength >= Tier3Threshold);

        if (crossedThreshold)
        {
            _popup.PopupEntity(Loc.GetString("barbell-bench-strength-increased"), performer, performer, PopupType.Medium);
        }

        if (training.Strength >= Tier3Threshold && !training.StaminaBonusApplied)
        {
            if (TryComp<StaminaComponent>(performer, out var staminaComp))
            {
                staminaComp.CritThreshold += StaminaBonusAmount;
                training.StaminaBonusApplied = true;
                Dirty(performer, staminaComp);
                Dirty(performer, training);
            }
        }
    }
}
