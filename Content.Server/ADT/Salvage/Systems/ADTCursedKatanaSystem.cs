using System.Linq;
using Content.Shared.ADT.Clothing;
using Content.Shared.ADT.MartialArts;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Actions;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.RetractableItemAction;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;
// Сплошной щиткод. В будущем реализовать под нормальную систему БИ оружий
public sealed class ADTCursedKatanaSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTCursedKatanaComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTCursedKatanaComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<ADTCursedKatanaComponent, EntGotInsertedIntoContainerMessage>(OnRetracted);
        SubscribeLocalEvent<ADTCursedKatanaComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<ADTCursedKatanaShardComponent, UseInHandEvent>(OnShardUsed);

        SubscribeLocalEvent<ADTCursedKatanaBearerComponent, UseAttemptEvent>(OnBearerUseAttempt);
        SubscribeLocalEvent<ADTCursedKatanaBearerComponent, IsEquippingAttemptEvent>(OnBearerEquipAttempt);
    }

    private void OnBearerUseAttempt(Entity<ADTCursedKatanaBearerComponent> ent, ref UseAttemptEvent args)
    {
        if (args.Cancelled || !GrantsMartialArt(args.Used))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("adt-cursed-katana-forbids-martial-arts"),
            ent.Owner, ent.Owner, PopupType.MediumCaution);
    }

    private void OnBearerEquipAttempt(Entity<ADTCursedKatanaBearerComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (args.Cancelled || !GrantsMartialArt(args.Equipment))
            return;

        args.Reason = "adt-cursed-katana-forbids-martial-arts";
        args.Cancel();
    }

    private bool GrantsMartialArt(EntityUid item)
    {
        foreach (var component in EntityManager.GetComponents(item))
        {
            if (component is GrantMartialArtKnowledgeComponent)
                return true;

            if (component is ClothingGrantComponentComponent grant && GrantsKravMaga(grant))
                return true;
        }

        return false;
    }

    private static bool GrantsKravMaga(ClothingGrantComponentComponent grant)
    {
        foreach (var entry in grant.Components.Values)
        {
            if (entry.Component is KravMagaComponent)
                return true;
        }

        return false;
    }

    private void OnShardUsed(Entity<ADTCursedKatanaShardComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (KnowsMartialArt(args.User))
        {
            _popup.PopupEntity(Loc.GetString("adt-cursed-katana-rejects-martial-artist"),
                args.User, args.User, PopupType.MediumCaution);
            return;
        }

        var actionEntity = ent.Comp.ActionEntity;
        if (!_actions.AddAction(args.User, ref actionEntity, ent.Comp.Action))
            return;

        EnsureComp<ADTCursedKatanaBearerComponent>(args.User);

        _popup.PopupEntity(Loc.GetString("adt-cursed-katana-shard-use"), args.User, args.User, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.ConsumeSound, args.User);
        QueueDel(ent.Owner);
    }

    private void OnExamined(Entity<ADTCursedKatanaComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.DrewBlood
            ? "adt-cursed-katana-sated"
            : "adt-cursed-katana-hungry"));

        foreach (var combo in ent.Comp.Combos)
        {
            var steps = string.Join(", ", combo.Inputs.Select(input => Loc.GetString(input switch
            {
                ADTKatanaInput.Heavy => "adt-cursed-katana-input-heavy",
                _ => "adt-cursed-katana-input-light",
            })));

            args.PushMarkup(Loc.GetString("adt-cursed-katana-combo",
                ("move", Loc.GetString(combo.Name)),
                ("steps", steps)));
        }
    }

    private void OnEquipped(Entity<ADTCursedKatanaComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.Holder = args.User;
    }

    private void OnRetracted(Entity<ADTCursedKatanaComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != RetractableItemActionComponent.ContainerId)
            return;

        Reset(ent);

        var holder = ent.Comp.Holder;
        ent.Comp.Holder = null;

        if (ent.Comp.DrewBlood)
        {
            ent.Comp.DrewBlood = false;
            return;
        }

        if (holder is not { } user || TerminatingOrDeleted(user))
            return;

        _popup.PopupEntity(Loc.GetString("adt-cursed-katana-lashes-out", ("katana", ent.Owner)),
            user, user, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.HungerSound, user);
        _damageable.TryChangeDamage(user, ent.Comp.HungerDamage, origin: ent.Owner);
    }

    private void OnMeleeHit(Entity<ADTCursedKatanaComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities[0];

        if (target == args.User || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return;

        ent.Comp.DrewBlood = true;

        if (KnowsMartialArt(args.User))
        {
            Reset(ent);
            return;
        }

        if (ent.Comp.CurrentTarget != target || _timing.CurTime > ent.Comp.ResetAt)
            ent.Comp.Inputs.Clear();

        ent.Comp.CurrentTarget = target;
        ent.Comp.ResetAt = _timing.CurTime + ent.Comp.ComboWindow;
        ent.Comp.Inputs.Add(args.Iswide ? ADTKatanaInput.Heavy : ADTKatanaInput.Light);

        TrimInputs(ent);

        if (!TryMatch(ent, out var combo))
            return;

        Reset(ent);
        Perform(ent, combo, args.User, target);
    }

    private void TrimInputs(Entity<ADTCursedKatanaComponent> ent)
    {
        var longest = 0;
        foreach (var combo in ent.Comp.Combos)
        {
            longest = Math.Max(longest, combo.Inputs.Count);
        }

        var excess = ent.Comp.Inputs.Count - longest;
        if (excess > 0)
            ent.Comp.Inputs.RemoveRange(0, excess);
    }

    private bool TryMatch(Entity<ADTCursedKatanaComponent> ent, out ADTKatanaCombo combo)
    {
        foreach (var candidate in ent.Comp.Combos)
        {
            var offset = ent.Comp.Inputs.Count - candidate.Inputs.Count;
            if (offset < 0)
                continue;

            if (!ent.Comp.Inputs.GetRange(offset, candidate.Inputs.Count).SequenceEqual(candidate.Inputs))
                continue;

            combo = candidate;
            return true;
        }

        combo = default!;
        return false;
    }

    private void Reset(Entity<ADTCursedKatanaComponent> ent)
    {
        ent.Comp.Inputs.Clear();
        ent.Comp.CurrentTarget = null;
    }

    private bool KnowsMartialArt(EntityUid user)
    {
        if (HasComp<MartialArtsKnowledgeComponent>(user) || HasComp<KravMagaComponent>(user))
            return true;

        var slots = _inventory.GetSlotEnumerator(user);
        while (slots.NextItem(out var item))
        {
            foreach (var component in EntityManager.GetComponents(item))
            {
                if (component is GrantMartialArtKnowledgeComponent)
                    return true;
            }
        }

        return false;
    }

    private void Perform(Entity<ADTCursedKatanaComponent> ent, ADTKatanaCombo combo, EntityUid user, EntityUid target)
    {
        _popup.PopupEntity(
            Loc.GetString(combo.Popup, ("user", user), ("target", target)),
            user,
            PopupType.MediumCaution);

        switch (combo.Move)
        {
            case ADTKatanaMove.TendonCut:
                TendonCut(ent, user, target);
                break;
            case ADTKatanaMove.HiltStrike:
                HiltStrike(ent, user, target);
                break;
            case ADTKatanaMove.Dash:
                Dash(ent, user, target);
                break;
            case ADTKatanaMove.DarkHeal:
                DarkHeal(ent, user, target);
                break;
        }
    }

    private void TendonCut(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target)
    {
        _audio.PlayPvs(ent.Comp.CutSound, target);
        _damageable.TryChangeDamage(target, ent.Comp.CutDamage, ignoreResistances: true, origin: user);
        _bloodstream.TryModifyBleedAmount(target, ent.Comp.CutBleed);
    }

    private void HiltStrike(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target)
    {
        _audio.PlayPvs(ent.Comp.StrikeSound, target);
        _damageable.TryChangeDamage(target, ent.Comp.StrikeDamage, ignoreResistances: true, origin: user);
        _stun.TryUpdateStunDuration(target, ent.Comp.StrikeStun);

        var direction = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(user).Position;

        if (direction.LengthSquared() < 0.01f)
            return;

        _throwing.TryThrow(target,
            direction.Normalized() * ent.Comp.StrikeThrowDistance,
            ent.Comp.StrikeThrowSpeed,
            user);
    }

    private void Dash(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target)
    {
        _audio.PlayPvs(ent.Comp.DashSound, user);

        var nearby = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(target), ent.Comp.DashRange, nearby);

        foreach (var mob in nearby)
        {
            if (mob.Owner == user || _mobState.IsDead(mob))
                continue;

            _damageable.TryChangeDamage(mob.Owner, ent.Comp.DashDamage, ignoreResistances: true, origin: user);
        }

        var direction = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(user).Position;

        if (direction.LengthSquared() < 0.01f)
            return;

        _throwing.TryThrow(user,
            direction.Normalized() * ent.Comp.DashDistance,
            ent.Comp.DashSpeed,
            animated: false,
            playSound: false,
            doSpin: false,
            recoil: false);
    }

    private void DarkHeal(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target)
    {
        _audio.PlayPvs(ent.Comp.HealSound, user);
        _damageable.TryChangeDamage(target, ent.Comp.HealCost, ignoreResistances: true, origin: user);
        _damageable.TryChangeDamage(user, ent.Comp.HealAmount, origin: ent.Owner);
    }
}
