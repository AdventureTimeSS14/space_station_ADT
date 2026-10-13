using System.Numerics;
using Content.Shared.ADT.ArmorPenetration;
using Content.Shared.ADT.Grab;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Resist;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class StarGazerGrabSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ArmorPenetrationSystem _armorPenetration = default!;

    private const float ThrowSpeed = 12f;
    private static readonly SoundSpecifier SmashSound = new SoundPathSpecifier("/Audio/Weapons/smash.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<MobStateComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<StarGazerComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<WallComponent, InteractUsingEvent>(OnWallInteractUsing);
        SubscribeLocalEvent<StarGazerHeldComponent, MeleeHitEvent>(OnHeldMeleeHit);

        SubscribeLocalEvent<StarGazerComponent, BeforeThrowEvent>(OnGazerBeforeThrow);
        SubscribeLocalEvent<StarGazerHeldComponent, DroppedEvent>(OnHeldDropped);
        SubscribeLocalEvent<StarGazerHeldComponent, ADTResistAttemptEvent>(OnHeldResist);

        SubscribeLocalEvent<StarGazerThrownComponent, StartCollideEvent>(OnThrownCollide);
        SubscribeLocalEvent<StarGazerThrownComponent, ThrowDoHitEvent>(OnThrownDoHit);
        SubscribeLocalEvent<StarGazerThrownComponent, StopThrowEvent>(OnThrownStopThrow);
    }

    public bool TryGrabMob(EntityUid gazer, EntityUid target)
    {
        if (!HasComp<StarGazerComponent>(gazer))
            return false;

        if (target == gazer)
            return false;

        if (!HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return false;

        if (!HasComp<HumanoidProfileComponent>(target))
            return false;

        if (_heretic.IsHereticOrGhoul(target) || HasComp<StarGazerComponent>(target))
            return false;

        if (!TryComp<HandsComponent>(gazer, out var hands) || hands.Count <= 0)
            return false;

        var addedItem = false;
        if (!HasComp<ItemComponent>(target))
        {
            EnsureComp<ItemComponent>(target);
            addedItem = true;
        }

        if (!_hands.TryForcePickupAnyHand(gazer, target, checkActionBlocker: false))
        {
            if (addedItem)
                RemComp<ItemComponent>(target);
            return false;
        }

        var held = EnsureComp<StarGazerHeldComponent>(target);
        held.Holder = gazer;
        held.AddedItemComponent = addedItem;
        held.EscapeChance = 0.10f;
        held.NextEscapeAttempt = _timing.CurTime + TimeSpan.FromSeconds(1);

        _popup.PopupEntity(Loc.GetString("stargazer-grab-held-user", ("target", target)), gazer, gazer, PopupType.Medium);
        _popup.PopupEntity(Loc.GetString("stargazer-grab-held-victim", ("gazer", gazer)), target, target, PopupType.LargeCaution);
        return true;
    }

    private void OnInteractHand(InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryGrabMob(args.User, args.Target))
            args.Handled = true;
    }

    private void OnGetVerbs(Entity<MobStateComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var gazer = args.User;
        var target = ent.Owner;

        if (!HasComp<StarGazerComponent>(gazer))
            return;

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (target == gazer || _mobState.IsDead(target) || _heretic.IsHereticOrGhoul(target) || HasComp<StarGazerComponent>(target))
            return;

        InteractionVerb verb = new()
        {
            Act = () => TryGrabMob(gazer, target),
            Text = Loc.GetString("stargazer-verb-grab"),
        };
        args.Verbs.Add(verb);
    }

    private void OnWallInteractUsing(Entity<WallComponent> wall, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<StarGazerHeldComponent>(args.Used, out var held) || held.Holder != args.User)
            return;

        args.Handled = true;
        SmashAgainstWall(args.User, args.Used, wall.Owner, 50f, 0.25f);
    }

    private void OnHeldMeleeHit(Entity<StarGazerHeldComponent> held, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (held.Comp.Holder != args.User)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (HasComp<WallComponent>(hit))
            {
                SmashAgainstWall(args.User, held.Owner, hit, 50f, 0.25f);
                break;
            }
        }
    }

    private void SmashAgainstWall(EntityUid gazer, EntityUid victim, EntityUid wall, float baseDamage, float penetration)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", baseDamage);

        if (_armorPenetration.TryGetArmorCompensator(victim, penetration, out var compensator))
            damage = DamageSpecifier.ApplyModifierSet(damage, compensator);

        _damageable.TryChangeDamage(victim, damage, origin: gazer);
        _stun.TryKnockdown((victim, null), TimeSpan.FromSeconds(2), true);
        _audio.PlayPvs(SmashSound, victim);

        var wallDamage = new DamageSpecifier();
        wallDamage.DamageDict.Add("Structural", baseDamage);
        _damageable.TryChangeDamage(wall, wallDamage, origin: gazer);

        _popup.PopupEntity(Loc.GetString("stargazer-smash-wall-user", ("victim", victim), ("wall", wall)), gazer, gazer, PopupType.MediumCaution);
        _popup.PopupEntity(Loc.GetString("stargazer-smash-wall-victim", ("gazer", gazer), ("wall", wall)), victim, victim, PopupType.LargeCaution);
    }

    private void OnGazerBeforeThrow(Entity<StarGazerComponent> gazer, ref BeforeThrowEvent args)
    {
        if (!TryComp<StarGazerHeldComponent>(args.ItemUid, out var held))
            return;

        var thrown = EnsureComp<StarGazerThrownComponent>(args.ItemUid);
        thrown.Thrower = gazer.Owner;
        thrown.WallDamage = 100f;
        thrown.ArmorPenetration = 0.50f;
        thrown.AddedItemComponent = held.AddedItemComponent;

        RemComp<StarGazerHeldComponent>(args.ItemUid);
    }

    private void OnHeldDropped(Entity<StarGazerHeldComponent> ent, ref DroppedEvent args)
    {
        if (ent.Comp.AddedItemComponent)
            RemComp<ItemComponent>(ent.Owner);

        RemComp<StarGazerHeldComponent>(ent.Owner);
    }

    private void OnHeldResist(Entity<StarGazerHeldComponent> ent, ref ADTResistAttemptEvent args)
    {
        args.Handled = true;

        if (_timing.CurTime < ent.Comp.NextEscapeAttempt)
        {
            _popup.PopupEntity(Loc.GetString("stargazer-escape-too-soon"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        ent.Comp.NextEscapeAttempt = _timing.CurTime + ent.Comp.EscapeCooldown;
        Dirty(ent.Owner, ent.Comp);

        if (_random.Prob(ent.Comp.EscapeChance))
        {
            _hands.TryDrop(ent.Comp.Holder, ent.Owner, checkActionBlocker: false);

            if (ent.Comp.AddedItemComponent)
                RemComp<ItemComponent>(ent.Owner);

            RemComp<StarGazerHeldComponent>(ent.Owner);

            _popup.PopupEntity(Loc.GetString("stargazer-escape-success"), ent.Owner, ent.Owner, PopupType.Medium);
            _popup.PopupEntity(Loc.GetString("stargazer-escape-success-holder", ("victim", ent.Owner)), ent.Comp.Holder, ent.Comp.Holder, PopupType.MediumCaution);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("stargazer-escape-fail"), ent.Owner, ent.Owner, PopupType.SmallCaution);
        }
    }

    private void OnThrownCollide(Entity<StarGazerThrownComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<WallComponent>(args.OtherEntity))
            return;

        HitWall(ent.Owner, ent.Comp, args.OtherEntity);
    }

    private void OnThrownDoHit(Entity<StarGazerThrownComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<WallComponent>(args.Target))
            return;

        HitWall(ent.Owner, ent.Comp, args.Target);
    }

    private void HitWall(EntityUid victim, StarGazerThrownComponent comp, EntityUid wall)
    {
        if (comp.HitWall)
            return;

        comp.HitWall = true;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", comp.WallDamage);

        if (_armorPenetration.TryGetArmorCompensator(victim, comp.ArmorPenetration, out var compensator))
            damage = DamageSpecifier.ApplyModifierSet(damage, compensator);

        _damageable.TryChangeDamage(victim, damage, origin: comp.Thrower);
        _stun.TryKnockdown((victim, null), TimeSpan.FromSeconds(3), true);
        _audio.PlayPvs(SmashSound, victim);

        var wallDamage = new DamageSpecifier();
        wallDamage.DamageDict.Add("Structural", comp.WallDamage);
        _damageable.TryChangeDamage(wall, wallDamage, origin: comp.Thrower);

        _popup.PopupEntity(Loc.GetString("stargazer-thrown-wall-victim", ("victim", victim), ("wall", wall)), victim, victim, PopupType.LargeCaution);
    }

    private void OnThrownStopThrow(Entity<StarGazerThrownComponent> ent, ref StopThrowEvent args)
    {
        if (ent.Comp.AddedItemComponent)
            RemComp<ItemComponent>(ent.Owner);

        RemComp<StarGazerThrownComponent>(ent.Owner);
    }

    private void OnMobStateChanged(Entity<StarGazerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        if (GetHeld(ent.Owner) is not { } held)
            return;

        var dir = _transform.GetWorldRotation(ent).ToWorldVec() * -1f;
        if (dir.LengthSquared() < 0.01f)
            dir = Vector2.UnitX;

        _hands.TryDrop(ent.Owner, held, checkActionBlocker: false);
        _throw.TryThrow(held, dir.Normalized(), ThrowSpeed, ent.Owner, recoil: false, animated: false, playSound: true);
    }

    private EntityUid? GetHeld(EntityUid gazer)
    {
        if (!TryComp<HandsComponent>(gazer, out var hands))
            return null;

        foreach (var handId in hands.SortedHands)
        {
            if (!_hands.TryGetHeldItem((gazer, hands), handId, out var item))
                continue;

            if (!HasComp<MobStateComponent>(item.Value))
                continue;

            return item.Value;
        }

        return null;
    }
}