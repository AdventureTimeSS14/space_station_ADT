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
using Content.Shared.Gibbing;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.RetractableItemAction;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class ADTCursedKatanaSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMartialArtsSystem _martialArts = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTCursedKatanaComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTCursedKatanaComponent, GettingPickedUpAttemptEvent>(OnKatanaPickupAttempt);
        SubscribeLocalEvent<ADTCursedKatanaComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<ADTCursedKatanaComponent, EntGotInsertedIntoContainerMessage>(OnRetracted);

        SubscribeLocalEvent<ADTCursedKatanaComponent, KatanaTendonCutPerformedEvent>(OnTendonCut);
        SubscribeLocalEvent<ADTCursedKatanaComponent, KatanaHiltStrikePerformedEvent>(OnHiltStrike);
        SubscribeLocalEvent<ADTCursedKatanaComponent, KatanaDashPerformedEvent>(OnDash);
        SubscribeLocalEvent<ADTCursedKatanaComponent, KatanaDarkHealPerformedEvent>(OnDarkHeal);

        SubscribeLocalEvent<ADTCursedKatanaShardComponent, UseInHandEvent>(OnShardUsed);

        SubscribeLocalEvent<ADTCursedKatanaBearerComponent, UseAttemptEvent>(OnBearerUseAttempt);
        SubscribeLocalEvent<ADTCursedKatanaBearerComponent, IsEquippingAttemptEvent>(OnBearerEquipAttempt);
        SubscribeLocalEvent<ADTCursedKatanaBearerComponent, MobStateChangedEvent>(OnBearerMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var mendQuery = EntityQueryEnumerator<ADTShadowMendComponent>();
        while (mendQuery.MoveNext(out var uid, out var mend))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (_timing.CurTime >= mend.EndTime)
            {
                ApplyVoidPrice(uid, mend);
                RemCompDeferred<ADTShadowMendComponent>(uid);
                continue;
            }

            if (_timing.CurTime < mend.NextTick)
                continue;

            mend.NextTick = _timing.CurTime + mend.TickInterval;

            foreach (var group in mend.HealGroups)
            {
                _damageable.HealEvenly(uid, -mend.HealPerTick, group);
            }
        }

        var priceQuery = EntityQueryEnumerator<ADTVoidPriceComponent>();
        while (priceQuery.MoveNext(out var uid, out var price))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (_timing.CurTime >= price.EndTime)
            {
                RemCompDeferred<ADTVoidPriceComponent>(uid);
                continue;
            }

            if (_timing.CurTime < price.NextTick)
                continue;

            price.NextTick = _timing.CurTime + price.TickInterval;

            _damageable.TryChangeDamage(uid, price.Damage * price.Price, ignoreResistances: true);
            _audio.PlayPvs(price.TickSound, uid);
        }
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

    private void OnBearerMobStateChanged(Entity<ADTCursedKatanaBearerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coordinates = Transform(ent.Owner).Coordinates;

        _popup.PopupCoordinates(Loc.GetString("adt-cursed-katana-bearer-consumed"),
            coordinates, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.ReleaseSound, coordinates);

        RemCompDeferred<ADTCursedKatanaBearerComponent>(ent.Owner);
        _gibbing.Gib(ent.Owner);

        Spawn(ent.Comp.Remains, coordinates);
        Spawn(ent.Comp.Blade, coordinates);
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

    private void OnKatanaPickupAttempt(Entity<ADTCursedKatanaComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled || IsActionBlade(ent.Owner) || !KnowsMartialArt(args.User))
            return;

        args.Cancel();

        if (args.ShowPopup)
        {
            _popup.PopupEntity(Loc.GetString("adt-cursed-katana-rejects-martial-artist"),
                args.User, args.User, PopupType.MediumCaution);
        }
    }

    private void OnEquipped(Entity<ADTCursedKatanaComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.Holder = args.User;

        if (!IsActionBlade(ent.Owner))
            Implant(ent, args.User);
    }

    private void Implant(Entity<ADTCursedKatanaComponent> ent, EntityUid user)
    {
        if (!HasComp<ADTCursedKatanaBearerComponent>(user))
        {
            var actionEntity = (EntityUid?) null;
            if (!_actions.AddAction(user, ref actionEntity, ent.Comp.ImplantAction))
                return;

            EnsureComp<ADTCursedKatanaBearerComponent>(user);
        }

        _popup.PopupEntity(Loc.GetString("adt-cursed-katana-implanted"), user, user, PopupType.LargeCaution);
        _audio.PlayPvs(ent.Comp.ImplantSound, user);

        ent.Comp.Holder = null;
        QueueDel(ent.Owner);
    }

    private bool IsActionBlade(EntityUid katana)
    {
        return HasComp<ActionRetractableItemComponent>(katana);
    }

    private void OnRetracted(Entity<ADTCursedKatanaComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != RetractableItemActionComponent.ContainerId)
            return;

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
        _damageable.TryChangeDamage(user, ent.Comp.HungerDamage, ignoreResistances: true, origin: ent.Owner);
    }

    private void OnMeleeHit(Entity<ADTCursedKatanaComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        var target = args.HitEntities[0];

        if (target == args.User || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return;

        ent.Comp.DrewBlood = true;
    }

    private void OnTendonCut(Entity<ADTCursedKatanaComponent> ent, ref KatanaTendonCutPerformedEvent args)
    {
        if (!TryGetCombo(ent, out var user, out var target))
            return;

        _audio.PlayPvs(ent.Comp.CutSound, target);
        _damageable.TryChangeDamage(target, ent.Comp.CutDamage, ignoreResistances: true, origin: user);
        _bloodstream.TryModifyBleedAmount(target, ent.Comp.CutBleed);

        Finish(ent, user, target, ent.Comp.CutPopup);
    }

    private void OnHiltStrike(Entity<ADTCursedKatanaComponent> ent, ref KatanaHiltStrikePerformedEvent args)
    {
        if (!TryGetCombo(ent, out var user, out var target))
            return;

        _audio.PlayPvs(ent.Comp.StrikeSound, target);
        _damageable.TryChangeDamage(target, ent.Comp.StrikeDamage, ignoreResistances: true, origin: user);
        _stun.TryUpdateStunDuration(target, ent.Comp.StrikeStun);

        var direction = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(user).Position;

        if (direction.LengthSquared() >= 0.01f)
        {
            _martialArts.GrabThrow(target,
                user,
                direction.Normalized() * ent.Comp.StrikeThrowDistance,
                ent.Comp.StrikeThrowSpeed,
                ent.Comp.StrikeImpactDamage);
        }

        Finish(ent, user, target, ent.Comp.StrikePopup);
    }

    private void OnDash(Entity<ADTCursedKatanaComponent> ent, ref KatanaDashPerformedEvent args)
    {
        if (!TryGetCombo(ent, out var user, out var target))
            return;

        _audio.PlayPvs(ent.Comp.DashSound, user);

        if (TryComp<PullerComponent>(user, out var puller)
            && puller.Pulling is { } pulled
            && TryComp<PullableComponent>(pulled, out var pullable))
        {
            _pulling.TryStopPull(pulled, pullable, user, true);
        }

        var nearby = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(user), ent.Comp.DashRange, nearby);

        foreach (var mob in nearby)
        {
            if (mob.Owner == user || mob.Owner == target || _mobState.IsDead(mob))
                continue;

            _damageable.TryChangeDamage(mob.Owner, ent.Comp.DashSplashDamage, ignoreResistances: true, origin: user);
        }

        _damageable.TryChangeDamage(target, ent.Comp.DashDamage, ignoreResistances: true, origin: user);

        DashThrough(ent, user, target);

        Finish(ent, user, target, ent.Comp.DashPopup);
    }

    private void DashThrough(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target)
    {
        var targetXform = Transform(target);

        if (targetXform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var direction = Transform(user).LocalRotation.GetCardinalDir();
        var offset = direction.ToIntVec();

        var tile = _map.TileIndicesFor(gridUid, grid, targetXform.Coordinates);
        var destination = tile;

        for (var i = 0; i < ent.Comp.DashTiles; i++)
        {
            var next = destination + offset;
            var nextRef = _map.GetTileRef(gridUid, grid, next);

            if (_turf.IsTileBlocked(nextRef, CollisionGroup.Impassable))
                break;

            destination = next;

            var coords = _map.GridTileToLocal(gridUid, grid, next);
            var passed = new HashSet<Entity<MobStateComponent>>();
            _lookup.GetEntitiesInRange(_transform.ToMapCoordinates(coords), 0.4f, passed);

            foreach (var mob in passed)
            {
                if (mob.Owner == user || mob.Owner == target || _mobState.IsDead(mob))
                    continue;

                _damageable.TryChangeDamage(mob.Owner, ent.Comp.DashTrailDamage, ignoreResistances: true, origin: user);
            }
        }

        if (destination == tile)
            return;

        var from = _transform.GetMapCoordinates(user).Position;
        var to = _transform.ToMapCoordinates(_map.GridTileToLocal(gridUid, grid, destination)).Position;

        _throwing.TryThrow(user,
            to - from,
            ent.Comp.DashSpeed,
            compensateFriction: true,
            animated: false,
            playSound: false,
            doSpin: false);
    }

    private void OnDarkHeal(Entity<ADTCursedKatanaComponent> ent, ref KatanaDarkHealPerformedEvent args)
    {
        if (!TryGetCombo(ent, out var user, out var target))
            return;

        _audio.PlayPvs(ent.Comp.HealSound, user);
        _damageable.TryChangeDamage(target, ent.Comp.HealCost, ignoreResistances: true, origin: user);

        var mend = EnsureComp<ADTShadowMendComponent>(user);
        mend.NextTick = _timing.CurTime;
        mend.EndTime = _timing.CurTime + mend.Duration;

        Finish(ent, user, target, ent.Comp.HealPopup);
    }

    private void ApplyVoidPrice(EntityUid uid, ADTShadowMendComponent mend)
    {
        if (TerminatingOrDeleted(uid))
            return;

        var refresh = HasComp<ADTVoidPriceComponent>(uid);
        var price = EnsureComp<ADTVoidPriceComponent>(uid);

        if (refresh)
            price.Price += price.PriceIncrease;

        price.NextTick = _timing.CurTime + price.TickInterval;
        price.EndTime = _timing.CurTime + price.Duration;

        _popup.PopupEntity(Loc.GetString("adt-cursed-katana-void-price"), uid, uid, PopupType.MediumCaution);
        _audio.PlayPvs(mend.PriceSound, uid);
    }

    private bool TryGetCombo(Entity<ADTCursedKatanaComponent> ent, out EntityUid user, out EntityUid target)
    {
        user = EntityUid.Invalid;
        target = EntityUid.Invalid;

        return TryComp<WeaponMartialArtComponent>(ent.Owner, out var weapon)
            && _martialArts.TryUseWeaponMartialArt((ent.Owner, weapon), out _, out user, out target, out _);
    }

    private void Finish(Entity<ADTCursedKatanaComponent> ent, EntityUid user, EntityUid target, LocId popup)
    {
        ent.Comp.DrewBlood = true;

        _popup.PopupEntity(Loc.GetString(popup, ("user", user), ("target", target)),
            user,
            PopupType.MediumCaution);

        if (TryComp<WeaponMartialArtComponent>(ent.Owner, out var weapon))
            _martialArts.ResetWeaponCombo((ent.Owner, weapon), false);
    }
}
