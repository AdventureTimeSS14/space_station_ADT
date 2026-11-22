using Content.Shared.ADT.Silicon;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Training;

public sealed class TrainingProgressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrainingProgressComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<TrainingProgressComponent, RefreshStaminaCritThresholdEvent>(OnRefreshCritThreshold);
    }

    public void AddTrainingProgress(EntityUid performer, EntityUid equipment)
    {
        if (!performer.Valid || TerminatingOrDeleted(performer))
            return;

        if (HasComp<MobIpcComponent>(performer))
            return;

        if (!TryComp<TrainingEquipmentComponent>(equipment, out var settings))
            return;

        var now = _timing.CurTime;
        var training = EnsureComp<TrainingProgressComponent>(performer);

        var credit = training.LastTraining == TimeSpan.Zero
            ? settings.FirstActionCredit
            : now - training.LastTraining;

        if (credit > settings.MaxCreditPerAction)
            credit = settings.MaxCreditPerAction;

        if (credit < TimeSpan.Zero)
            credit = TimeSpan.Zero;

        training.LastTraining = now;
        training.TrainedTime += credit;

        var tier = 0;
        for (var i = 0; i < settings.Tiers.Count; i++)
        {
            if (training.TrainedTime >= settings.Tiers[i].Time)
                tier = i + 1;
        }

        if (tier > training.Tier)
        {
            var reached = settings.Tiers[tier - 1];
            training.Tier = tier;
            training.StaminaBonus = reached.StaminaBonus;
            training.UnarmedDamageBonus = reached.UnarmedDamageBonus;
            training.PullDensityReduction = reached.PullDensityReduction;
            Dirty(performer, training);

            _stamina.RefreshStaminaCritThreshold(performer);
            _popup.PopupEntity(Loc.GetString("barbell-bench-strength-increased"), performer, performer, PopupType.Medium);
            return;
        }

        Dirty(performer, training);
    }

    public float GetPullDensityReduction(EntityUid uid)
    {
        return TryComp<TrainingProgressComponent>(uid, out var training) ? training.PullDensityReduction : 0f;
    }

    private void OnRefreshCritThreshold(Entity<TrainingProgressComponent> ent, ref RefreshStaminaCritThresholdEvent args)
    {
        args.ThresholdValue *= 1f + ent.Comp.StaminaBonus;
    }

    private void OnMeleeHit(Entity<TrainingProgressComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.Weapon != args.User || args.User != ent.Owner)
            return;

        if (ent.Comp.UnarmedDamageBonus <= 0f)
            return;

        args.BonusDamage += args.BaseDamage * ent.Comp.UnarmedDamageBonus;
    }
}
