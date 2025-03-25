using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.Grab;
using Content.Shared.Actions.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Humanoid;

namespace Content.Shared.ADT.Combat;

public abstract class SharedDropkickSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComboComponent, DisarmAttemptEvent>(OnDisarmUsed);
        SubscribeLocalEvent<ComboComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ComboComponent, GrabStageChangedEvent>(OnGrab);

        SubscribeLocalEvent<ComboComponent, ToggleCombatActionEvent>(OnCombatToggled);
    }

    private void OnDisarmUsed(EntityUid uid, ComboComponent comp, DisarmAttemptEvent args)
    {
        if (args.DisarmerUid != uid || args.DisarmerUid == args.TargetUid)
            return;

        comp.CurrestActions.Add(CombatAction.Disarm);

        if (comp.CurrestActions.Count >= 5)
        {
            comp.CurrestActions.RemoveAt(0);
        }
        TryDoCombo(args.DisarmerUid, args.TargetUid, comp);
    }

    private void OnMeleeHit(EntityUid uid, ComboComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;
        if (!HasComp<HumanoidAppearanceComponent>(args.HitEntities[0]))
            return;
        comp.CurrestActions.Add(CombatAction.Hit);

        if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
        {
            comp.CurrestActions.RemoveAt(0);
        }

        TryDoCombo(uid, args.HitEntities[0], comp);
    }

    private void OnGrab(EntityUid uid, ComboComponent comp, ref GrabStageChangedEvent args)
    {
        if (args.Puller.Owner != uid)
            return;
        if (args.NewStage <= args.OldStage)
            return;
        comp.CurrestActions.Add(CombatAction.Grab);

        if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
        {
            comp.CurrestActions.RemoveAt(0);
        }

        if (TryDoCombo(args.Puller.Owner, args.Pulling.Owner, comp))
            args.NewStage = 0;
    }
    private void UseEventOnTarget(EntityUid user, EntityUid target, CombatMove combo)
    {
        foreach (var comboEvent in combo.ComboEvent)
        {
            comboEvent.DoEffect(user, target, EntityManager);
        }
    }
    private bool TryDoCombo(EntityUid user, EntityUid target, ComboComponent comp)
    {
        var mainList = comp.CurrestActions;
        if (mainList == null)
            return false;
        var isComboCompleted = false;
        foreach (var combo in comp.AvailableMoves)
        {
            var subList = combo.ActionsNeeds;
            if (!ContainsSubsequence(mainList, subList))
                continue;
            UseEventOnTarget(user, target, combo);
            isComboCompleted = true;
        }
        if (isComboCompleted)
            comp.CurrestActions.Clear();
        if (TryComp<PullableComponent>(target, out var pulled) && isComboCompleted)
            _pullingSystem.TryStopPull(target, pulled, user);
        return true;
    }
    public static bool ContainsSubsequence<T>(List<T> mainList, List<T> subList)
    {
        if (subList.Count == 0)
            return true;

        for (int i = 0; i <= mainList.Count - subList.Count; i++)
        {
            if (mainList.Skip(i).Take(subList.Count).SequenceEqual(subList))
            {
                return true;
            }
        }

        return false;
    }
    private void OnCombatToggled(EntityUid uid, ComboComponent comp, ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combat))
            return;
        comp.CurrestActions.Clear();
    }
}
