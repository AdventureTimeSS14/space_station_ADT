using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Actions;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.ADT.Grab;
using Content.Shared.Actions.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Humanoid;
using Content.Shared.ADT.Crawling;
using Content.Shared.Coordinates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Content.Shared.IdentityManagement;
namespace Content.Shared.ADT.Combat;

public abstract class SharedComboSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

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
        comp.Target = args.DisarmerUid;
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
        comp.Target = args.HitEntities[0];

        var user = uid;
        var target = args.HitEntities[0];

        if ( comp.AllowNeckSnap && _standing.IsDown(target) &&
            !_mobState.IsDead(target) &&
            !HasComp<GodmodeComponent>(target) &&
            TryComp<PullerComponent>(user, out var puller) &&
            puller.Stage == GrabStage.Choke &&
            puller.Pulling == target &&
            _mobThreshold.TryGetDeadThreshold(target, out var damageToKill) &&
            damageToKill != null
            && TryComp(target, out StaminaComponent? stamina) && stamina.Critical) // проверка условий для перелома шеи
        {
            if (comp.CurrestActions != null)
            {
                comp.CurrestActions.RemoveAt(0); // мы очищаем комбо список чтобы не было конфликтов, прежде чем сделать попап.
            }
            var blunt = new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), damageToKill.Value);
            _damageable.TryChangeDamage(target, blunt, true);
            _audio.PlayPvs("/Audio/ADT/Effects/crack1.ogg", target);
            _popup.PopupPredicted(Loc.GetString("cqc-necksnap-popup", ("user", Identity.Entity(user, _entManager)), ("target", target)), target, target, PopupType.LargeCaution);
            if (TryComp<PullableComponent>(target, out var pulled))
            {
                _pullingSystem.TryStopPull(target, pulled, user); // освобождаем от граба гнилой труп
            }
        }

        TryDoCombo(uid, args.HitEntities[0], comp);
    }

    private void OnGrab(EntityUid uid, ComboComponent comp, ref GrabStageChangedEvent args)
    {
        if (args.Puller.Owner != uid || args.NewStage <= args.OldStage)
            return;

        comp.CurrestActions.Add(CombatAction.Grab);

        if (comp.CurrestActions.Count >= 5)
        {
            comp.CurrestActions.RemoveAt(0);
        }

        if (TryDoCombo(args.Puller.Owner, args.Pulling.Owner, comp))
        {
            // args.NewStage = comp.GrabStageAfterCombo;
        }
    }

    public void UseEventOnTarget(EntityUid user, EntityUid target, CombatMove combo)
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
        var stopGrab = false;
        foreach (var combo in comp.AvailableMoves)
        {
            var subList = combo.ActionsNeeds;
            if (!ContainsSubsequence(mainList, subList))
                continue;
            UseEventOnTarget(user, target, combo);
            isComboCompleted = true;
            if (combo.StopGrab)
                stopGrab = true;
        }
        if (isComboCompleted)
            comp.CurrestActions.Clear();
        if (TryComp<PullableComponent>(target, out var pulled) && isComboCompleted && stopGrab)
            _pullingSystem.TryStopPull(target, pulled, user);
        return true;
    }

    public static bool ContainsSubsequence<T>(List<T> mainList, List<T> subList)
    {
        if (subList.Count == 0)
            return true;

        for (int i = 0; i <= mainList.Count - subList.Count; i++)
        {
            bool match = true;
            for (int j = 0; j < subList.Count; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(mainList[i + j], subList[j]))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return true;
        }

        return false;
    }

    // private void ToggleCrawling(EntityUid uid, ComboComponent comp, CrawlingKeybindEvent args)
    // {
    //     var userCoords = uid.ToCoordinates();
    //     var targetCoords = comp.Target.ToCoordinates();
    //     var diff = userCoords.X - targetCoords.X + userCoords.Y - targetCoords.Y;

    //     if (diff >= 4 || diff <= -4)
    //         return;

    //     comp.CurrestActions.Add(CombatAction.Crawl);

    //     if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
    //     {
    //         comp.CurrestActions.RemoveAt(0);
    //     }

    //     TryDoCombo(uid, comp.Target, comp);
    // }

    private void OnCombatToggled(EntityUid uid, ComboComponent comp, ToggleCombatActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combat))
            return;
        comp.CurrestActions.Clear();
    }
}
