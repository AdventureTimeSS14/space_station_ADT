using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeWeaponCombos()
    {
        SubscribeLocalEvent<WeaponMartialArtComponent, ComponentStartup>(OnWeaponComboStartup);
        SubscribeLocalEvent<WeaponMartialArtComponent, AfterInteractEvent>(OnWeaponComboAfterInteract);
        SubscribeLocalEvent<WeaponMartialArtComponent, DroppedEvent>(OnWeaponComboDropped);

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

    private void RegisterWeaponStep(Entity<WeaponMartialArtComponent> weapon,
        EntityUid user,
        EntityUid target,
        ComboAttackType type)
    {
        var comp = weapon.Comp;

        if (target == user
            || !HasComp<MobStateComponent>(target)
            || _mobState.IsDead(target))
            return;

        if (comp.BlockedByKnownMartialArt && KnowsMartialArt(user))
        {
            ResetWeaponCombo(weapon, false);
            return;
        }

        if (comp.LastAttacks.Count > 0 && comp.ResetTime - comp.ComboWindow == _timing.CurTime)
            return;

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

    public void GrabThrow(EntityUid target,
        EntityUid thrower,
        System.Numerics.Vector2 vector,
        float speed,
        DamageSpecifier? impactDamage = null,
        bool dropItems = false)
    {
        _grabThrown.Throw(target, thrower, vector, speed, impactDamage, dropItems);
    }

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

        if (comp.BeingPerformed is not { } beingPerformed
            || !_proto.TryIndex(beingPerformed, out proto)
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
}
