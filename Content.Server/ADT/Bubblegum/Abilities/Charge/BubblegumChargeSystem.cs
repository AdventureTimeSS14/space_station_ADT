using System.Numerics;
using Content.Server.Destructible;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.ADT.Bubblegum.Abilities;
using Content.Shared.ADT.Trail;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumChargeSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly BubblegumCombatSystem _combat = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumActiveChargeComponent, ComponentStartup>(OnActiveStart);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, ThrowDoHitEvent>(OnChargeHit);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, LandEvent>(OnChargeLand);
        SubscribeLocalEvent<BubblegumActiveChargeComponent, PreventCollideEvent>(OnChargePreventCollide);
    }

    private void OnCanMove(Entity<BubblegumActiveChargeComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnActiveStart(Entity<BubblegumActiveChargeComponent> ent, ref ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent.Owner);

        if (HasComp<TrailComponent>(ent))
            return;

        var isHallucination = HasComp<BubblegumHallucinationComponent>(ent);
        var trail = new TrailComponent
        {
            UseOwnerAsRenderedEntity = true,
            RenderedEntity = ent.Owner,
            NoRenderIfRenderedEntityDeleted = true,
            Frequency = 0.04f,
            Lifetime = 0.35f,
            LerpTime = 0.04f,
            AlphaLerpAmount = 0.5f,
            AlphaLerpTarget = 0f,
            MaxParticleAmount = 12,
            Color = isHallucination
                ? Color.FromHex("#FFFFFF88")
                : Color.FromHex("#FF3030"),
        };
        AddComp(ent.Owner, trail);
    }

    private void OnActiveShutdown(Entity<BubblegumActiveChargeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<TrailComponent>(ent);
        _combat.RemoveBubblegumBusy(ent.Owner);
        _blocker.UpdateCanMove(ent.Owner);
    }

    public void BeginCharge(
        EntityUid user,
        MapCoordinates target,
        float delaySeconds,
        float speed,
        string telegraphProto,
        float trampleDamage,
        bool expireOnHit = false,
        float telegraphLeadSeconds = 0f,
        EntityUid? targetEntity = null)
    {
        if (TerminatingOrDeleted(user))
            return;

        if (_mobState.IsDead(user))
            return;

        var userMap = _transform.GetMapCoordinates(user);
        if (userMap.MapId != target.MapId)
            return;

        var pending = EnsureComp<BubblegumPendingChargeComponent>(user);
        var now = _timing.CurTime;
        pending.Queue.Add(new PendingCharge
        {
            TelegraphAt = now + TimeSpan.FromSeconds(telegraphLeadSeconds),
            ExecuteAt = now + TimeSpan.FromSeconds(delaySeconds),
            Target = target,
            TargetEntity = targetEntity,
            Speed = speed,
            TrampleDamage = trampleDamage,
            ExpireOnHit = expireOnHit,
            TelegraphProto = telegraphProto
        });
        Dirty(user, pending);
    }

    private bool DoCharge(EntityUid user, PendingCharge charge)
    {
        if (TerminatingOrDeleted(user))
            return false;

        if (_mobState.IsDead(user))
            return false;

        var userMap = _transform.GetMapCoordinates(user);
        if (userMap.MapId != charge.Target.MapId)
            return false;

        var diff = charge.Target.Position - userMap.Position;
        if (diff.LengthSquared() < 0.01f)
            return false;

        var direction = Vector2.Normalize(diff);
        var distance = diff.Length();

        var active = EnsureComp<BubblegumActiveChargeComponent>(user);
        active.TargetCoords = charge.Target;
        active.Direction = direction;
        active.TrampleDamage = charge.TrampleDamage;
        active.ExpireOnHit = charge.ExpireOnHit;
        active.AlreadySmashed.Clear();

        var ray = new CollisionRay(userMap.Position, direction, (int)CollisionGroup.Impassable);
        var rayResults = _physics.IntersectRay(userMap.MapId, ray, distance, user, false);
        foreach (var result in rayResults)
        {
            var hit = result.HitEntity;
            if (!IsSmashableStructure(hit))
                continue;

            if (active.AlreadySmashed.Add(hit))
                TrySmashStructure(user, active, hit);
        }

        active.EndsAt = _timing.CurTime + TimeSpan.FromSeconds(distance / MathF.Max(1f, charge.Speed) + 0.2f);
        Dirty(user, active);

        var userXform = Transform(user);
        var gridRot = _transform.GetWorldRotation(userXform.ParentUid);
        _transform.SetLocalRotation(user, direction.ToWorldAngle() - gridRot);

        _throwing.TryThrow(user, diff, charge.Speed, animated: false, playSound: false, doSpin: false, compensateFriction: true);
        return true;
    }

    private void RefreshTargetFromEntity(PendingCharge item, MapId userMap)
    {
        if (item.TargetEntity is not { } entity)
            return;

        if (TerminatingOrDeleted(entity))
            return;

        var coords = _transform.GetMapCoordinates(entity);
        if (coords.MapId != userMap)
            return;

        item.Target = coords;
    }

    private void OnChargeHit(Entity<BubblegumActiveChargeComponent> ent, ref ThrowDoHitEvent args)
    {
        var target = args.Target;
        if (target == ent.Owner)
            return;

        if (HasComp<BubblegumComponent>(target) || HasComp<BubblegumHallucinationComponent>(target))
            return;

        if (HasComp<MobStateComponent>(target))
        {
            if (_mobState.IsDead(target))
                return;

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", ent.Comp.TrampleDamage);
            _damageable.TryChangeDamage(target, damage, false, origin: ent.Owner);

            _recoil.KickCamera(target, ent.Comp.Direction * ent.Comp.CameraKickStrength);

            if (ent.Comp.ExpireOnHit)
            {
                if (HasComp<BubblegumHallucinationComponent>(ent))
                {
                    QueueDel(ent.Owner);
                    return;
                }

                RemCompDeferred<BubblegumActiveChargeComponent>(ent);
            }
            return;
        }

        if (IsSmashableStructure(target) && ent.Comp.AlreadySmashed.Add(target))
            TrySmashStructure(ent.Owner, ent.Comp, target);
    }

    private void OnChargePreventCollide(Entity<BubblegumActiveChargeComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (!IsSmashableStructure(args.OtherEntity))
            return;

        if (ent.Comp.AlreadySmashed.Add(args.OtherEntity))
            TrySmashStructure(ent.Owner, ent.Comp, args.OtherEntity);

        // Structures are handled by charge damage, not by physics collision. Letting the
        // hard collision resolve can stop the throw or rotate its velocity away from the
        // originally planned charge direction.
        args.Cancelled = true;
    }

    private bool TrySmashStructure(EntityUid user, BubblegumActiveChargeComponent component, EntityUid target)
    {
        var smash = new DamageSpecifier();
        smash.DamageDict.Add("Blunt", component.SmashBlunt);
        smash.DamageDict.Add("Structural", component.SmashStructural);
        _damageable.TryChangeDamage(target, smash, true, origin: user);

        return TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target);
    }

    private bool IsSmashableStructure(EntityUid uid)
    {
        if (HasComp<ItemComponent>(uid))
            return false;

        if (!HasComp<DamageableComponent>(uid))
            return false;

        if (!HasComp<DestructibleComponent>(uid))
            return false;

        return Transform(uid).Anchored;
    }

    private void OnChargeLand(Entity<BubblegumActiveChargeComponent> ent, ref LandEvent args)
    {
        RemCompDeferred<BubblegumActiveChargeComponent>(ent);

        if (HasComp<BubblegumHallucinationComponent>(ent))
        {
            QueueDel(ent);
            return;
        }

        if (TryComp<BubblegumBloodAttackComponent>(ent.Owner, out var blood))
            blood.NextAttemptAt = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var pendingQuery = EntityQueryEnumerator<BubblegumPendingChargeComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            var userMapId = _transform.GetMapCoordinates(uid).MapId;
            for (var i = pending.Queue.Count - 1; i >= 0; i--)
            {
                var item = pending.Queue[i];

                if (!item.TelegraphSpawned && now >= item.TelegraphAt)
                {
                    RefreshTargetFromEntity(item, userMapId);

                    var lifetime = (float)(item.ExecuteAt - now).TotalSeconds;
                    var telegraph = Spawn(item.TelegraphProto, item.Target, rotation: Angle.Zero);
                    EnsureComp<TimedDespawnComponent>(telegraph).Lifetime = MathF.Max(0.1f, lifetime);
                    item.TelegraphSpawned = true;
                }

                if (now < item.ExecuteAt)
                    continue;

                var charged = DoCharge(uid, item);
                pending.Queue.RemoveAt(i);

                if (!charged && HasComp<BubblegumHallucinationComponent>(uid))
                    QueueDel(uid);
            }

            if (pending.Queue.Count == 0)
                RemCompDeferred<BubblegumPendingChargeComponent>(uid);
        }

        var activeQuery = EntityQueryEnumerator<BubblegumActiveChargeComponent>();
        while (activeQuery.MoveNext(out var uid, out var comp))
        {
            if (now < comp.EndsAt)
                continue;

            RemCompDeferred<BubblegumActiveChargeComponent>(uid);

            if (HasComp<BubblegumHallucinationComponent>(uid))
                QueueDel(uid);
        }
    }
}
