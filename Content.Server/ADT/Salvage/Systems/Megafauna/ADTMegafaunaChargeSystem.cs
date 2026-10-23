using System.Numerics;
using Content.Server.ADT.Mining;
using Content.Server.Destructible;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.ArmorPenetration;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Throwing;
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
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class ADTMegafaunaChargeSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private ArmorPenetrationSystem _penetration = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private ADTMineralSystem _mineral = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private const float MaxLeadDistance = 2.5f;

    private readonly HashSet<Entity<MobStateComponent>> _trampleBuffer = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMegafaunaPendingChargeComponent, ComponentStartup>(OnPendingStartup);
        SubscribeLocalEvent<ADTMegafaunaPendingChargeComponent, ComponentShutdown>(OnPendingShutdown);
        SubscribeLocalEvent<ADTMegafaunaPendingChargeComponent, ADTMegafaunaBusyCheckEvent>(OnPendingBusyCheck);

        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, ComponentStartup>(OnActiveStart);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, ADTMegafaunaBusyCheckEvent>(OnActiveBusyCheck);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, ThrowDoHitEvent>(OnChargeHit);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, LandEvent>(OnChargeLand);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, PreventCollideEvent>(OnChargePreventCollide);
        SubscribeLocalEvent<ADTMegafaunaActiveChargeComponent, AttemptMeleeEvent>(OnAttemptMelee);
    }

    public bool IsCharging(EntityUid uid)
    {
        return HasComp<ADTMegafaunaPendingChargeComponent>(uid) || HasComp<ADTMegafaunaActiveChargeComponent>(uid);
    }

    public void CancelCharges(EntityUid uid)
    {
        if (TryComp<ADTMegafaunaPendingChargeComponent>(uid, out var pending))
        {
            pending.Queue.Clear();
            RemCompDeferred<ADTMegafaunaPendingChargeComponent>(uid);
        }

        if (!HasComp<ADTMegafaunaActiveChargeComponent>(uid))
            return;

        RemCompDeferred<ADTMegafaunaActiveChargeComponent>(uid);
        _physics.SetLinearVelocity(uid, Vector2.Zero);
    }

    public void BeginCharge(
        EntityUid user,
        MapCoordinates target,
        float delaySeconds,
        float speed,
        EntProtoId telegraphProto,
        float trampleDamage,
        bool expireOnHit = false,
        float telegraphLeadSeconds = 0f,
        EntityUid? targetEntity = null,
        ADTMegafaunaChargeOptions? options = null)
    {
        if (TerminatingOrDeleted(user) || _mobState.IsDead(user))
            return;

        if (_transform.GetMapCoordinates(user).MapId != target.MapId)
            return;

        var pending = EnsureComp<ADTMegafaunaPendingChargeComponent>(user);
        var now = _timing.CurTime;

        pending.Queue.Add(new ADTMegafaunaPendingCharge
        {
            TelegraphAt = now + TimeSpan.FromSeconds(telegraphLeadSeconds),
            ExecuteAt = now + TimeSpan.FromSeconds(delaySeconds),
            Target = target,
            TargetEntity = targetEntity,
            Speed = speed,
            TrampleDamage = trampleDamage,
            ExpireOnHit = expireOnHit,
            Options = options ?? new ADTMegafaunaChargeOptions(),
            TelegraphProto = telegraphProto,
        });
    }

    public float BeginChargeSeries(
        EntityUid user,
        MapCoordinates fallback,
        EntityUid? targetEntity,
        IReadOnlyList<float> delays,
        float travelBuffer,
        float speed,
        EntProtoId telegraphProto,
        float trampleDamage,
        ADTMegafaunaChargeOptions? options = null,
        bool chained = false)
    {
        var cumulative = 0f;

        if (chained)
        {
            if (TerminatingOrDeleted(user) || _mobState.IsDead(user))
                return 0f;

            var pending = EnsureComp<ADTMegafaunaPendingChargeComponent>(user);

            foreach (var delay in delays)
            {
                pending.Queue.Add(new ADTMegafaunaPendingCharge
                {
                    Scheduled = false,
                    Delay = delay,
                    Target = fallback,
                    TargetEntity = targetEntity,
                    Speed = speed,
                    TrampleDamage = trampleDamage,
                    Options = options ?? new ADTMegafaunaChargeOptions(),
                    TelegraphProto = telegraphProto,
                });

                cumulative += delay;
            }

            return cumulative;
        }

        for (var i = 0; i < delays.Count; i++)
        {
            var delay = delays[i];

            BeginCharge(
                user,
                fallback,
                cumulative + delay,
                speed,
                telegraphProto,
                trampleDamage,
                telegraphLeadSeconds: cumulative,
                targetEntity: targetEntity,
                options: options);

            cumulative += delay + travelBuffer;
        }

        return cumulative;
    }

    private void OnPendingStartup(Entity<ADTMegafaunaPendingChargeComponent> ent, ref ComponentStartup args)
    {
        _megafauna.RefreshBusy(ent);
    }

    private void OnPendingShutdown(Entity<ADTMegafaunaPendingChargeComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _megafauna.RefreshBusy(ent);
    }

    private void OnPendingBusyCheck(Entity<ADTMegafaunaPendingChargeComponent> ent, ref ADTMegafaunaBusyCheckEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.Busy = true;
    }

    private void OnActiveBusyCheck(Entity<ADTMegafaunaActiveChargeComponent> ent, ref ADTMegafaunaBusyCheckEvent args)
    {
        if (ent.Comp.LifeStage < ComponentLifeStage.Stopping)
            args.Busy = true;
    }

    private void OnAttemptMelee(Entity<ADTMegafaunaActiveChargeComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCanMove(Entity<ADTMegafaunaActiveChargeComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnActiveStart(Entity<ADTMegafaunaActiveChargeComponent> ent, ref ComponentStartup args)
    {
        _blocker.UpdateCanMove(ent.Owner);
        _megafauna.RefreshBusy(ent);
    }

    private void OnActiveShutdown(Entity<ADTMegafaunaActiveChargeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        RemCompDeferred<TrailComponent>(ent);
        _megafauna.RefreshBusy(ent);
        _blocker.UpdateCanMove(ent.Owner);
    }

    private void AddTrail(EntityUid uid, Color color)
    {
        if (HasComp<TrailComponent>(uid))
            return;

        var trail = new TrailComponent
        {
            UseOwnerAsRenderedEntity = true,
            RenderedEntity = uid,
            NoRenderIfRenderedEntityDeleted = true,
            Frequency = 0.04f,
            Lifetime = 0.35f,
            LerpTime = 0.04f,
            AlphaLerpAmount = 0.5f,
            AlphaLerpTarget = 0f,
            MaxParticleAmount = 12,
            Color = color,
        };
        AddComp(uid, trail);
    }

    private bool DoCharge(EntityUid user, ADTMegafaunaPendingCharge charge)
    {
        if (TerminatingOrDeleted(user) || _mobState.IsDead(user))
            return false;

        var userMap = _transform.GetMapCoordinates(user);
        if (userMap.MapId != charge.Target.MapId)
            return false;

        var diff = charge.Target.Position - userMap.Position;

        Vector2 direction;
        if (diff.LengthSquared() < 0.01f)
            direction = _transform.GetWorldRotation(user).ToWorldVec();
        else
            direction = Vector2.Normalize(diff);

        if (direction.LengthSquared() < 0.01f)
            direction = new Vector2(1f, 0f);

        var options = charge.Options;
        var distance = MathF.Max(diff.Length(), options.MinDistance) + options.Overshoot;

        if (distance < 0.01f)
            return false;

        var travel = direction * distance;

        var active = EnsureComp<ADTMegafaunaActiveChargeComponent>(user);
        active.TargetCoords = charge.Target;
        active.Direction = direction;
        active.TrampleDamage = charge.TrampleDamage;
        active.ExpireOnHit = charge.ExpireOnHit;
        active.Options = options;
        active.AlreadySmashed.Clear();
        active.AlreadyHit.Clear();

        if (options.TrailColor is { } color)
            AddTrail(user, color);

        if (options.SmashPath)
        {
            var ray = new CollisionRay(userMap.Position, direction, (int) CollisionGroup.Impassable);
            var rayResults = _physics.IntersectRay(userMap.MapId, ray, distance, user, false);
            foreach (var result in rayResults)
            {
                var hit = result.HitEntity;
                if (!IsSmashableStructure(hit))
                    continue;

                if (active.AlreadySmashed.Add(hit))
                    TrySmashStructure(user, active, hit);
            }
        }

        active.EndsAt = _timing.CurTime + TimeSpan.FromSeconds(distance / MathF.Max(1f, charge.Speed) + 0.2f);

        var userXform = Transform(user);
        var gridRot = _transform.GetWorldRotation(userXform.ParentUid);
        _transform.SetLocalRotation(user, direction.ToWorldAngle() - gridRot);

        TrampleSweep((user, active));

        if (TerminatingOrDeleted(user) || EntityManager.IsQueuedForDeletion(user) || !HasComp<ADTMegafaunaActiveChargeComponent>(user))
            return true;

        TryComp<ADTThrowImmuneComponent>(user, out var immune);

        if (immune != null)
            immune.Bypass = true;

        _throwing.TryThrow(user, travel, charge.Speed, animated: false, playSound: false, doSpin: false, compensateFriction: true);

        if (immune != null)
            immune.Bypass = false;
        return true;
    }

    private void TrampleSweep(Entity<ADTMegafaunaActiveChargeComponent> ent)
    {
        _trampleBuffer.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(ent.Owner), ent.Comp.Options.TrampleRadius, _trampleBuffer);

        foreach (var mob in _trampleBuffer)
        {
            if (!TryTrample(ent, mob.Owner))
                continue;

            if (TerminatingOrDeleted(ent) || !HasComp<ADTMegafaunaActiveChargeComponent>(ent))
                return;
        }
    }

    private bool TryTrample(Entity<ADTMegafaunaActiveChargeComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
            return false;

        if (ent.Comp.AlreadyHit.Contains(target))
            return false;

        var attempt = new ADTMegafaunaChargeTrampleAttemptEvent(ent.Owner);
        RaiseLocalEvent(target, ref attempt);

        if (attempt.Cancelled)
            return false;

        ent.Comp.AlreadyHit.Add(target);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", ent.Comp.TrampleDamage);

        if (ent.Comp.Options.ArmorPenetration > 0f)
            damage = _penetration.ApplyPenetration(target, damage, ent.Comp.Options.ArmorPenetration);

        _damageable.TryChangeDamage(target, damage, false, origin: ent.Owner);

        _recoil.KickCamera(target, ent.Comp.Direction * ent.Comp.CameraKickStrength);

        var trampled = new ADTMegafaunaChargeTrampledEvent(target, ent.Comp.Direction);
        RaiseLocalEvent(ent.Owner, ref trampled);

        if (!ent.Comp.ExpireOnHit)
            return true;

        EndCharge(ent.Owner, ADTMegafaunaChargeEndReason.Hit);
        return true;
    }

    private void EndCharge(EntityUid uid, ADTMegafaunaChargeEndReason reason)
    {
        RemCompDeferred<ADTMegafaunaActiveChargeComponent>(uid);

        var ev = new ADTMegafaunaChargeEndedEvent(reason);
        RaiseLocalEvent(uid, ref ev);
    }

    private void RefreshTargetFromEntity(ADTMegafaunaPendingCharge item, MapId userMap, TimeSpan now)
    {
        if (item.TargetEntity is not { } entity || TerminatingOrDeleted(entity))
            return;

        var coords = _transform.GetMapCoordinates(entity);
        if (coords.MapId != userMap)
            return;

        var lead = (float) (item.ExecuteAt - now).TotalSeconds;
        if (lead <= 0f || !item.Options.LeadTarget)
        {
            item.Target = coords;
            return;
        }

        var velocity = _physics.GetMapLinearVelocity(entity);
        var offset = velocity * lead;
        if (offset.Length() > MaxLeadDistance)
            offset = Vector2.Normalize(offset) * MaxLeadDistance;

        item.Target = new MapCoordinates(coords.Position + offset, coords.MapId);
    }

    private void OnChargeHit(Entity<ADTMegafaunaActiveChargeComponent> ent, ref ThrowDoHitEvent args)
    {
        var target = args.Target;
        if (target == ent.Owner)
            return;

        if (HasComp<MobStateComponent>(target))
        {
            TryTrample(ent, target);
            return;
        }

        if (IsSmashableStructure(target) && ent.Comp.AlreadySmashed.Add(target))
            TrySmashStructure(ent.Owner, ent.Comp, target);
    }

    private void OnChargePreventCollide(Entity<ADTMegafaunaActiveChargeComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Options.PassThroughMobs && HasComp<MobStateComponent>(args.OtherEntity))
        {
            TryTrample(ent, args.OtherEntity);
            args.Cancelled = true;
            return;
        }

        if (!ent.Comp.Options.SmashPath || !IsSmashableStructure(args.OtherEntity))
            return;

        if (ent.Comp.AlreadySmashed.Add(args.OtherEntity))
            TrySmashStructure(ent.Owner, ent.Comp, args.OtherEntity);

        args.Cancelled = true;
    }

    private bool TrySmashStructure(EntityUid user, ADTMegafaunaActiveChargeComponent component, EntityUid target)
    {
        if (TryComp<ADTMineralComponent>(target, out var mineral))
        {
            _mineral.Mine((target, mineral), Math.Max(1, mineral.Hardness), user);
            return mineral.Breaking;
        }

        var smash = new DamageSpecifier();
        smash.DamageDict.Add("Blunt", component.SmashBlunt);
        smash.DamageDict.Add("Structural", component.SmashStructural);
        _damageable.TryChangeDamage(target, smash, true, origin: user);

        return TerminatingOrDeleted(target) || EntityManager.IsQueuedForDeletion(target);
    }

    private bool IsSmashableStructure(EntityUid uid)
    {
        if (HasComp<ADTMineralComponent>(uid))
            return true;

        return !HasComp<ItemComponent>(uid)
               && !HasComp<MobStateComponent>(uid)
               && HasComp<DamageableComponent>(uid)
               && HasComp<DestructibleComponent>(uid);
    }

    private void ScheduleNextChained(EntityUid uid, ADTMegafaunaPendingChargeComponent pending, TimeSpan now)
    {
        if (HasComp<ADTMegafaunaActiveChargeComponent>(uid))
            return;

        foreach (var item in pending.Queue)
        {
            if (item.Scheduled)
                return;
        }

        foreach (var item in pending.Queue)
        {
            item.Scheduled = true;
            item.TelegraphAt = now;
            item.ExecuteAt = now + TimeSpan.FromSeconds(item.Delay);
            return;
        }
    }

    private void OnChargeLand(Entity<ADTMegafaunaActiveChargeComponent> ent, ref LandEvent args)
    {
        EndCharge(ent, ADTMegafaunaChargeEndReason.Landed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var pendingQuery = EntityQueryEnumerator<ADTMegafaunaPendingChargeComponent>();
        while (pendingQuery.MoveNext(out var uid, out var pending))
        {
            var userMapId = _transform.GetMapCoordinates(uid).MapId;
            ScheduleNextChained(uid, pending, now);

            for (var i = pending.Queue.Count - 1; i >= 0; i--)
            {
                var item = pending.Queue[i];

                if (!item.Scheduled)
                    continue;

                if (!item.TelegraphSpawned && now >= item.TelegraphAt)
                {
                    var telegraphEv = new ADTMegafaunaChargeTelegraphEvent(item.Target, item.TargetEntity);
                    RaiseLocalEvent(uid, ref telegraphEv);

                    if (telegraphEv.Handled)
                        item.Target = telegraphEv.Target;
                    else
                        RefreshTargetFromEntity(item, userMapId, now);

                    var lifetime = item.Options.TelegraphLifetime ?? (float) (item.ExecuteAt - now).TotalSeconds;
                    var telegraph = Spawn(item.TelegraphProto, item.Target, rotation: Angle.Zero);
                    EnsureComp<TimedDespawnComponent>(telegraph).Lifetime = MathF.Max(0.1f, lifetime);
                    item.TelegraphSpawned = true;
                }

                if (now < item.ExecuteAt)
                    continue;

                var charged = DoCharge(uid, item);
                pending.Queue.RemoveAt(i);

                if (!charged)
                {
                    var failed = new ADTMegafaunaChargeEndedEvent(ADTMegafaunaChargeEndReason.Failed);
                    RaiseLocalEvent(uid, ref failed);
                }
            }

            if (pending.Queue.Count == 0)
                RemCompDeferred<ADTMegafaunaPendingChargeComponent>(uid);
        }

        var activeQuery = EntityQueryEnumerator<ADTMegafaunaActiveChargeComponent>();
        while (activeQuery.MoveNext(out var uid, out var comp))
        {
            TrampleSweep((uid, comp));

            if (TerminatingOrDeleted(uid) || !HasComp<ADTMegafaunaActiveChargeComponent>(uid))
                continue;

            if (now < comp.EndsAt)
                continue;

            EndCharge(uid, ADTMegafaunaChargeEndReason.Expired);
        }
    }
}
