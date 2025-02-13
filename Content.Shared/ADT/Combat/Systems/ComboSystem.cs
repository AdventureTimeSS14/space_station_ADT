using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Damage.Events; // ADT-Changeling-Tweak
using Content.Shared.Alert;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Database;
using Content.Shared.Effects;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.ADT.Grab;
using Content.Shared.Damage;
using Content.Shared.Chat;

namespace Content.Shared.ADT.Combat;

public abstract class SharedComboSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComboComponent, DisarmedEvent>(OnDisarmUsed);
        SubscribeLocalEvent<ComboComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ComboComponent, GrabStageChangedEvent>(OnGrab);

        SubscribeLocalEvent<ComboTargetDamageEvent>(OnTargetDamage);
        SubscribeLocalEvent<ComboUserDamageEvent>(OnUserDamage);
    }

    private void OnDisarmUsed(EntityUid uid, ComboComponent comp, DisarmedEvent args)
    {
        if (args.Source != uid)
            return;

        comp.CurrestActions.Add(CombatAction.Disarm);

        if (comp.CurrestActions.Count >= 5)
        {
            comp.CurrestActions.RemoveAt(1);
        }
        TryDoCombo(uid, args.Target, comp);
    }

    private void OnMeleeHit(EntityUid uid, ComboComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
        {
            return;
        }

        comp.CurrestActions.Add(CombatAction.Hit);

        if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
        {
            comp.CurrestActions.RemoveAt(1);
        }

        TryDoCombo(uid, args.HitEntities[0], comp);
    }

    private void OnGrab(EntityUid uid, ComboComponent comp, GrabStageChangedEvent args)
    {
        if (args.Puller.Owner != uid)
            return;

        comp.CurrestActions.Add(CombatAction.Grab);

        if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
        {
            comp.CurrestActions.RemoveAt(1);
        }

        TryDoCombo(uid, args.Puller.Owner, comp);
    }

    private void UseEventOnTarget(EntityUid user, EntityUid target, CombatMove combo)
    {
        foreach (var comboEvent in combo.ComboEvent)
        {
            var eventArgs = comboEvent;
            eventArgs.User = user;
            eventArgs.Target = target;

            RaiseLocalEvent(target, eventArgs);
        }
    }
    private void TryDoCombo(EntityUid user, EntityUid hited, ComboComponent comp)
    {
        var mainList = comp.CurrestActions;
        if (mainList == null)
            return;
        var isComboCompleted = false;
        foreach (var combo in comp.AvailableMoves)
        {
            var subList = combo.ActionsNeeds;
            if (!ContainsSubsequence(mainList, subList))
                continue;
            UseEventOnTarget(user, hited, combo);
            isComboCompleted = true;
        }
        if (isComboCompleted)
            comp.CurrestActions.Clear();
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
    #region combo events

    private void OnTargetDamage(ComboTargetDamageEvent args)
    {
        _damageableSystem.TryChangeDamage(args.Target, args.Damage, args.IgnoreResistances);
    }
    private void OnUserDamage(ComboUserDamageEvent args)
    {
        _damageableSystem.TryChangeDamage(args.User, args.Damage, args.IgnoreResistances);
    }
    #endregion combo events
}
