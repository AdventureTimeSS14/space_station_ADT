using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.MartialArts;

/// <summary>
/// Боевые искусства оружия. Комбо копится на самом предмете, а не на бойце,
/// как в SS13, где стили из mind срабатывают только на безоружных атаках,
/// а оружейные приёмы каждый предмет считает сам.
/// </summary>
public partial class SharedMartialArtsSystem
{
    private void InitializeWeaponCombos()
    {
        // ComponentStartup, а не MapInit: у сущностей, приехавших с сервера, MapInit на клиенте
        // не поднимается, и список приёмов остался бы пустым.
        SubscribeLocalEvent<WeaponMartialArtComponent, ComponentStartup>(OnWeaponComboStartup);
        SubscribeLocalEvent<WeaponMartialArtComponent, AfterInteractEvent>(OnWeaponComboAfterInteract);
        SubscribeLocalEvent<WeaponMartialArtComponent, DroppedEvent>(OnWeaponComboDropped);

        // Событие поднимается на бойце, поэтому подписываемся на руки: оружие всё равно
        // ищется по активной руке, а не по полю Weapon - захват пишет туда самого бойца.
        SubscribeLocalEvent<HandsComponent, ComboAttackPerformedEvent>(OnWeaponComboAttackPerformed);
    }

    private void UpdateWeaponCombos()
    {
        var query = EntityQueryEnumerator<WeaponMartialArtComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CurrentTarget != null && TerminatingOrDeleted(comp.CurrentTarget.Value))
                comp.CurrentTarget = null;

            if (comp.LastAttacks.Count == 0 || _timing.CurTime < comp.ResetTime)
                continue;

            ResetWeaponCombo((uid, comp), comp.ResetPopup);
        }
    }

    #region Event Methods

    private void OnWeaponComboStartup(Entity<WeaponMartialArtComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.AllowedCombos.Clear();

        if (!_proto.TryIndex(ent.Comp.Combos, out var list))
            return;

        foreach (var combo in list.Combos)
        {
            ent.Comp.AllowedCombos.Add(_proto.Index(combo));
        }
    }

    /// <summary>
    /// Клик предметом по существу вне режима боя - интент помощи.
    /// Безоружный аналог сидит в OnInteract и даёт Hug.
    /// </summary>
    private void OnWeaponComboAfterInteract(Entity<WeaponMartialArtComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach
            || args.Target is not { } target
            || _combatMode.IsInCombatMode(args.User))
            return;

        RegisterWeaponStep(ent, args.User, target, ComboAttackType.Help);
    }

    private void OnWeaponComboDropped(Entity<WeaponMartialArtComponent> ent, ref DroppedEvent args)
    {
        ResetWeaponCombo(ent, ent.Comp.ResetPopup, args.User);
    }

    private void OnWeaponComboAttackPerformed(Entity<HandsComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (args.Cancelled
            || args.Performer != ent.Owner
            || !_hands.TryGetActiveItem(ent.Owner, out var held)
            || !TryComp<WeaponMartialArtComponent>(held, out var weapon))
            return;

        RegisterWeaponStep((held.Value, weapon), args.Performer, args.Target, args.Type);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Записывает шаг комбо в оружие и проверяет, не сложился ли приём.
    /// </summary>
    private void RegisterWeaponStep(Entity<WeaponMartialArtComponent> weapon,
        EntityUid user,
        EntityUid target,
        ComboAttackType type)
    {
        var comp = weapon.Comp;

        // Как в SS13: шаг пишется только по живой чужой цели.
        if (target == user
            || !HasComp<MobStateComponent>(target)
            || _mobState.IsDead(target))
            return;

        if (comp.BlockedByKnownMartialArt && KnowsMartialArt(user))
        {
            ResetWeaponCombo(weapon, false);
            return;
        }

        // Размашистый удар поднимает событие на каждую задетую цель - считаем только первую.
        if (comp.LastAttacks.Count > 0 && comp.ResetTime - comp.ComboWindow == _timing.CurTime)
            return;

        // Помощь и захват не ограничены скоростью оружия, поэтому им нужен свой кулдаун,
        // иначе комбо на этих шагах набирается простым закликиванием.
        if (comp.ThrottledSteps.Contains(type))
        {
            if (_timing.CurTime < comp.NextThrottledStep)
                return;

            comp.NextThrottledStep = _timing.CurTime + comp.StepCooldown;
        }

        if (_timing.CurTime >= comp.ResetTime
            || comp.RequireSameTarget && comp.CurrentTarget != null && comp.CurrentTarget != target)
            comp.LastAttacks.Clear();

        comp.CurrentTarget = target;
        comp.CurrentUser = user;
        comp.ResetTime = _timing.CurTime + comp.ComboWindow;
        comp.LastAttacks.Add(type);

        if (comp.LastAttacksLimit >= 0)
        {
            var excess = comp.LastAttacks.Count - comp.LastAttacksLimit;
            if (excess > 0)
                comp.LastAttacks.RemoveRange(0, excess);
        }

        CheckWeaponCombo(weapon);
        Dirty(weapon);
    }

    /// <summary>
    /// Ищет приём, чей набор шагов совпадает с хвостом буфера. Побеждает самый длинный,
    /// иначе короткое комбо срабатывало бы по дороге к длинному.
    /// </summary>
    private void CheckWeaponCombo(Entity<WeaponMartialArtComponent> weapon)
    {
        var comp = weapon.Comp;
        ComboPrototype? match = null;

        foreach (var proto in comp.AllowedCombos)
        {
            if (proto.ResultEvent == null)
                continue;

            var offset = comp.LastAttacks.Count - proto.AttackTypes.Count;
            if (offset < 0)
                continue;

            if (match != null && proto.AttackTypes.Count <= match.AttackTypes.Count)
                continue;

            if (!comp.LastAttacks.GetRange(offset, proto.AttackTypes.Count).SequenceEqual(proto.AttackTypes))
                continue;

            match = proto;
        }

        if (match?.ResultEvent is not { } ev)
            return;

        comp.BeingPerformed = match.ID;
        RaiseLocalEvent(weapon.Owner, ev);
    }

    public void ResetWeaponCombo(Entity<WeaponMartialArtComponent> weapon, bool popup, EntityUid? user = null)
    {
        var comp = weapon.Comp;

        if (comp.LastAttacks.Count == 0 && comp.CurrentTarget == null)
            return;

        comp.LastAttacks.Clear();
        comp.CurrentTarget = null;

        user ??= comp.CurrentUser;

        // Попап поднимаем только на сервере. Клиент чистит набор предсказанием, потом
        // получает состояние сервера, где сброса ещё не было, и на следующем тике сбрасывает
        // снова - и так до тех пор, пока сервер не догонит. PopupClient от этого не спасает:
        // он отсекает лишь повторы внутри одного тика, а тут сброс честно случается много раз.
        if (popup && user != null && !TerminatingOrDeleted(user.Value) && _netManager.IsServer)
        {
            _popupSystem.PopupEntity(Loc.GetString("weapon-martial-art-neutral-stance"), user.Value, user.Value);
        }

        comp.CurrentUser = null;
        Dirty(weapon);
    }

    private bool KnowsMartialArt(EntityUid user)
    {
        return HasComp<MartialArtsKnowledgeComponent>(user) || HasComp<KravMagaComponent>(user);
    }

    /// <summary>
    /// Общий хвост удавшегося приёма: звук, попап и возврат в нейтральную стойку.
    /// </summary>
    private void FinishWeaponCombo(Entity<WeaponMartialArtComponent> weapon,
        EntityUid user,
        EntityUid target,
        string comboName,
        SoundSpecifier? sound)
    {
        _audio.PlayPredicted(sound, target, user);
        ComboPopup(user, target, comboName);
        ResetWeaponCombo(weapon, false);
    }

    /// <summary>
    /// Строки с приёмами оружия для осмотра: название и последовательность интентов.
    /// </summary>
    public List<string> GetWeaponComboDescriptions(EntityUid weapon)
    {
        var result = new List<string>();

        if (!TryComp<WeaponMartialArtComponent>(weapon, out var comp))
            return result;

        foreach (var combo in comp.AllowedCombos)
        {
            var steps = string.Join(", ",
                combo.AttackTypes.Select(x => Loc.GetString($"combo-intent-{x.ToString().ToLower()}")));

            result.Add(Loc.GetString("weapon-martial-art-combo",
                ("move", Loc.GetString(combo.Name)),
                ("steps", steps)));
        }

        return result;
    }

    /// <summary>
    /// Бросок цели с уроном при столкновении. Обёртка, чтобы серверным приёмам
    /// оружия не тянуть за собой GrabThrownSystem.
    /// </summary>
    public void GrabThrow(EntityUid target,
        EntityUid thrower,
        System.Numerics.Vector2 vector,
        float speed,
        DamageSpecifier? impactDamage = null,
        bool dropItems = false)
    {
        _grabThrown.Throw(target, thrower, vector, speed, impactDamage, dropItems);
    }

    /// <summary>
    /// Оружейный аналог <see cref="TryUseMartialArt"/>. Вызывается из обработчиков приёмов.
    /// </summary>
    public bool TryUseWeaponMartialArt(Entity<WeaponMartialArtComponent> weapon,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ComboPrototype? proto,
        out EntityUid user,
        out EntityUid target,
        out bool downed)
    {
        proto = null;
        user = EntityUid.Invalid;
        target = EntityUid.Invalid;
        downed = false;

        var comp = weapon.Comp;

        if (!_proto.TryIndex(comp.BeingPerformed, out proto)
            || proto.MartialArtsForm != comp.MartialArtsForm)
            return false;

        if (comp.CurrentUser is not { } currentUser
            || comp.CurrentTarget is not { } currentTarget
            || TerminatingOrDeleted(currentUser)
            || TerminatingOrDeleted(currentTarget))
            return false;

        if (!proto.CanDoWhileProne && IsDown(currentUser))
        {
            _popupSystem.PopupClient(Loc.GetString("martial-arts-fail-prone"), currentUser, currentUser);
            return false;
        }

        user = currentUser;
        target = currentTarget;
        downed = IsDown(currentTarget);
        return true;

        bool IsDown(EntityUid uid)
        {
            return TryComp<StandingStateComponent>(uid, out var standing) && !standing.Standing;
        }
    }

    #endregion
}
