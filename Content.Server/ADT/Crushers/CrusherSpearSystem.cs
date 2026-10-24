using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Crushers;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Crushers.Systems;
using Content.Shared.ADT.Trail;
using Content.Shared.ADT.Weapons.Ranged.WearableGun;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Crushers;

public sealed class CrusherSpearSystem : SharedCrusherSpearSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private ADTKineticFocusSystem _focus = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDamageMarkerSystem _marker = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedWieldableSystem _wieldable = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private ThrownItemSystem _thrownItem = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTCrusherSpearThrowRequestEvent>(OnThrowRequest);

        SubscribeLocalEvent<ADTCrusherSpearThrowComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<ADTCrusherSpearThrowComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<ADTCrusherSpearThrowComponent, StopThrowEvent>(OnStopThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTCrusherSpearReturningComponent, ADTCrusherSpearThrowComponent, PhysicsComponent>();

        while (query.MoveNext(out var uid, out _, out var spear, out var physics))
        {
            Steer((uid, spear), physics);
        }
    }

    private void OnThrowRequest(ADTCrusherSpearThrowRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var weapon = GetEntity(msg.Weapon);

        if (!TryComp<ADTCrusherSpearThrowComponent>(weapon, out var spear))
            return;

        var ent = new Entity<ADTCrusherSpearThrowComponent>(weapon, spear);

        if (spear.Thrower != null || !_hands.IsHolding(user, weapon))
            return;

        if (TryComp<UseDelayComponent>(weapon, out var delay) && _useDelay.IsDelayed((weapon, delay), spear.DelayId))
            return;

        if (spear.RequireWielded && TryComp<WieldableComponent>(weapon, out var wieldable) && !wieldable.Wielded)
        {
            _popup.PopupEntity(Loc.GetString("adt-crusher-spear-need-wield"), user, user);
            return;
        }

        if (TryComp<ADTWearableGunUserComponent>(user, out var wearable) && HasComp<TrophyHolderComponent>(wearable.Gun))
        {
            _popup.PopupEntity(Loc.GetString("adt-crusher-spear-claws-worn"), user, user);
            return;
        }

        if (!_actionBlocker.CanAttack(user))
            return;

        var userPos = _transform.GetMapCoordinates(user);
        var targetPos = _transform.ToMapCoordinates(GetCoordinates(msg.Target));

        if (targetPos.MapId != userPos.MapId)
            return;

        var direction = targetPos.Position - userPos.Position;
        var length = direction.Length();

        if (length < 0.01f)
            return;

        if (length > spear.Range)
            direction *= spear.Range / length;

        if (!_hands.TryDrop(user, weapon))
            return;

        spear.Thrower = user;
        spear.Returning = false;
        spear.ThrowOrigin = Transform(user).Coordinates;
        Dirty(ent);

        if (!_throwing.TryThrow(weapon, direction, spear.ThrowSpeed, user, pushbackRatio: 0f, recoil: false, playSound: false))
        {
            Catch(ent);
            return;
        }

        SetTrail(ent.Owner, spear.TrailFrequency);
        ShootMark(ent, user, direction);
        _audio.PlayPvs(spear.ThrowSound, user);
    }

    private void ShootMark(Entity<ADTCrusherSpearThrowComponent> ent, EntityUid user, Vector2 direction)
    {
        if (ent.Comp.MarkProjectile is not { } proto)
            return;

        var bolt = Spawn(proto, Transform(user).Coordinates);
        _gun.ShootProjectile(bolt, direction, Vector2.Zero, ent.Owner, user, ent.Comp.MarkProjectileSpeed);
    }

    private void OnThrowHit(Entity<ADTCrusherSpearThrowComponent> ent, ref ThrowDoHitEvent args)
    {
        if (ent.Comp.Thrower is not { } user || ent.Comp.Returning || args.Target == user)
            return;

        if (HasComp<DamageableComponent>(args.Target) && _whitelist.IsWhitelistPassOrNull(ent.Comp.Whitelist, args.Target))
            Strike(ent, user, args.Target);

        StartReturn(ent);
    }

    private void OnLand(Entity<ADTCrusherSpearThrowComponent> ent, ref LandEvent args)
    {
        if (ent.Comp.Thrower == null)
            return;

        if (!ent.Comp.Returning)
        {
            StartReturn(ent);
            return;
        }

        Catch(ent);
    }

    private void OnStopThrow(Entity<ADTCrusherSpearThrowComponent> ent, ref StopThrowEvent args)
    {
        if (ent.Comp.Thrower == null)
            return;

        Catch(ent);
    }

    private void Strike(Entity<ADTCrusherSpearThrowComponent> ent, EntityUid user, EntityUid target)
    {
        _marker.ApplyMarker(target, ent.Owner, ent.Comp.Damage, ent.Comp.MarkDuration);

        var hitEvent = new MeleeHitEvent(new HashSet<EntityUid> { target }, user, ent.Owner, new DamageSpecifier(), null);
        RaiseLocalEvent(ent.Owner, hitEvent);

        if (hitEvent.Handled || TerminatingOrDeleted(target))
            return;

        var attackedEvent = new AttackedEvent(ent.Owner, user, Transform(target).Coordinates);
        RaiseLocalEvent(target, attackedEvent);

        var damage = DamageSpecifier.ApplyModifierSets(hitEvent.BonusDamage + attackedEvent.BonusDamage,
            hitEvent.ModifiersList.Concat(attackedEvent.ModifiersList));

        _damageable.TryChangeDamage(target, damage, origin: user);
        _audio.PlayPvs(ent.Comp.HitSound, target);

        if (ent.Comp.HitEffect is { } effect)
            Spawn(effect, Transform(target).Coordinates);
    }

    private void StartReturn(Entity<ADTCrusherSpearThrowComponent> ent)
    {
        if (!TryComp<ThrownItemComponent>(ent, out var thrown) || !TryComp<PhysicsComponent>(ent, out var physics))
        {
            Catch(ent);
            return;
        }

        ent.Comp.Returning = true;
        Dirty(ent);

        var returning = EnsureComp<ADTCrusherSpearReturningComponent>(ent);
        SoftenFixtures((ent.Owner, returning));

        _physics.SetAngularVelocity(ent.Owner, 0f, body: physics);
        _physics.SetBodyStatus(ent.Owner, physics, BodyStatus.InAir);

        thrown.Landed = false;
        thrown.LandTime = _timing.CurTime + ent.Comp.MaxReturnTime;
        Dirty(ent, thrown);

        Steer(ent, physics);
    }

    private void Steer(Entity<ADTCrusherSpearThrowComponent> ent, PhysicsComponent physics)
    {
        if (ent.Comp.Thrower is not { } user)
        {
            RemCompDeferred<ADTCrusherSpearReturningComponent>(ent);
            return;
        }

        if (TerminatingOrDeleted(user))
        {
            Catch(ent);
            return;
        }

        var spearPos = _transform.GetMapCoordinates(ent.Owner);
        var userPos = _transform.GetMapCoordinates(user);

        if (spearPos.MapId != userPos.MapId)
        {
            Catch(ent);
            return;
        }

        var direction = userPos.Position - spearPos.Position;
        var distance = direction.Length();

        if (distance <= ent.Comp.CatchDistance)
        {
            Catch(ent);
            return;
        }

        var heading = direction / distance;
        var offset = TryComp<ThrowingAngleComponent>(ent, out var throwingAngle) ? throwingAngle.Angle : Angle.Zero;
        _transform.SetWorldRotation(ent.Owner, heading.ToWorldAngle() + offset);

        var speed = MathF.Min(ent.Comp.ReturnSpeed, distance / (float) _timing.TickPeriod.TotalSeconds);
        var mapVelocity = _physics.GetMapLinearVelocity(ent.Owner, physics);
        _physics.SetLinearVelocity(ent.Owner, physics.LinearVelocity + heading * speed - mapVelocity, body: physics);
    }

    private void SoftenFixtures(Entity<ADTCrusherSpearReturningComponent> ent)
    {
        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (!fixture.Hard)
                continue;

            _physics.SetHard(ent.Owner, fixture, false, fixtures);
            ent.Comp.SoftenedFixtures.Add(id);
        }
    }

    private void RestoreFixtures(EntityUid uid)
    {
        if (!TryComp<ADTCrusherSpearReturningComponent>(uid, out var returning))
            return;

        if (TryComp<FixturesComponent>(uid, out var fixtures))
        {
            foreach (var id in returning.SoftenedFixtures)
            {
                if (fixtures.Fixtures.TryGetValue(id, out var fixture))
                    _physics.SetHard(uid, fixture, true, fixtures);
            }
        }

        RemCompDeferred<ADTCrusherSpearReturningComponent>(uid);
    }

    private void Catch(Entity<ADTCrusherSpearThrowComponent> ent)
    {
        var user = ent.Comp.Thrower;
        var origin = ent.Comp.ThrowOrigin;

        ent.Comp.Thrower = null;
        ent.Comp.Returning = false;
        ent.Comp.ThrowOrigin = null;
        Dirty(ent);

        RestoreFixtures(ent.Owner);
        SetTrail(ent.Owner, 0f);

        if (TryComp<PhysicsComponent>(ent, out var physics))
            _physics.SetLinearVelocity(ent.Owner, Vector2.Zero, body: physics);

        if (TryComp<ThrownItemComponent>(ent, out var thrown))
            _thrownItem.StopThrow(ent.Owner, thrown);

        var cooldown = user is { } thrower ? ent.Comp.Cooldown * _focus.GetCooldownMultiplier(thrower) : ent.Comp.Cooldown;
        _useDelay.SetLength(ent.Owner, cooldown, ent.Comp.DelayId);
        _useDelay.TryResetDelay(ent.Owner, id: ent.Comp.DelayId);

        if (user == null || TerminatingOrDeleted(user.Value) || Transform(user.Value).MapID != Transform(ent.Owner).MapID)
        {
            if (origin is { } coords && coords.IsValid(EntityManager))
                _transform.SetCoordinates(ent.Owner, coords);

            return;
        }

        _transform.SetCoordinates(ent.Owner, Transform(user.Value).Coordinates);

        if (_mobState.IsIncapacitated(user.Value))
            return;

        _hands.PickupOrDrop(user.Value, ent, animate: false);

        if (_hands.IsHolding(user.Value, ent.Owner)
            && TryComp<WieldableComponent>(ent, out var wieldable)
            && !wieldable.Wielded
            && _wieldable.CanWield((ent.Owner, wieldable), user.Value, true))
        {
            _wieldable.TryWield((ent.Owner, wieldable), user.Value);
        }

        _audio.PlayPvs(ent.Comp.CatchSound, user.Value);
    }

    private void SetTrail(EntityUid uid, float frequency)
    {
        if (!TryComp<TrailComponent>(uid, out var trail))
            return;

        trail.Frequency = frequency;
        Dirty(uid, trail);
    }
}
