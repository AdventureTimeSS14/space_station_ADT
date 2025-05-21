using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Humanoid;
using Content.Shared._RMC14.Weapons.Common;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Combat;

public sealed class SharedWeaponComboSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComboWeaponComponent, UniqueActionEvent>(OnUniqueAction);
        SubscribeLocalEvent<ComboWeaponComponent, MeleeHitEvent>(OnHeavyHit);
    }

    public static WeaponCombatAction GetWeaponAction(bool isWide, ComboWeaponStand stance)
    {
        //Я НЕ ЗНАЮ КАК ЭТО РАБОТАЕТ, МЕНЯ МОЖЕТЕ НЕ СПРАШИВАТЬ
        int index = ((isWide ? 1 : 0) << 1) | (int)stance;
        return (WeaponCombatAction)index;
    }

    private void OnHeavyHit(EntityUid uid, ComboWeaponComponent comp, MeleeHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (!args.IsHit || !args.HitEntities.Any())
            return;
        if (!HasComp<HumanoidAppearanceComponent>(args.HitEntities[0]))
            return;

        var move = GetWeaponAction(args.Iswide, comp.CurrentStand);
        comp.CurrestActions.Add(move);

        if (comp.CurrestActions.Count >= 5 && comp.CurrestActions != null)
        {
            comp.CurrestActions.RemoveAt(0);
        }
        comp.Target = args.HitEntities[0];
        TryDoCombo(args.User, args.HitEntities[0], comp);
    }

    private void OnUniqueAction(EntityUid uid, ComboWeaponComponent comp, UniqueActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        switch (comp.CurrentStand)
        {
            case ComboWeaponStand.Protective:
                comp.CurrentStand = ComboWeaponStand.Offensive;
                _appearance.SetData(uid, ComboWeaponState.State, true);
                break;
            case ComboWeaponStand.Offensive:
                comp.CurrentStand = ComboWeaponStand.Protective;
                _appearance.SetData(uid, ComboWeaponState.State, false);
                break;
        }
    }

    private bool TryDoCombo(EntityUid user, EntityUid target, ComboWeaponComponent comp)
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
            foreach (var comboEvent in combo.ComboEvent)
            {
                comboEvent.DoEffect(user, target, EntityManager);
            }
            isComboCompleted = true;
        }
        if (isComboCompleted)
            comp.CurrestActions.Clear();
        if (TryComp<PullableComponent>(target, out var pulled) && isComboCompleted)
            _pullingSystem.TryStopPull(target, pulled, user);
        return true;
    }

    //дубликат в связи с тем, что не дотнет не даёт использовать метод из combosystem
    public bool ContainsSubsequence<T>(List<T> mainList, List<T> subList)
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
}
