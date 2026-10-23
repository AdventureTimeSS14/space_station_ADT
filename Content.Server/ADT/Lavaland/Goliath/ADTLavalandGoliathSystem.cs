using System.Numerics;
using Content.Server.ADT.Mining;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Lavaland.Goliath;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.Goliath;

public sealed class ADTLavalandGoliathSystem : EntitySystem
{
    private const float ChargeArrivalRange = 0.3f;

    private static readonly string[] BruteTypes = { "Blunt", "Slash", "Piercing" };

    private static readonly Vector2i[] Cardinals =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
    };

    private static readonly Vector2i[] AllDirections =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1),
    };

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ADTMineralSystem _mineral = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private readonly HashSet<EntityUid> _tileEntities = new();
    private readonly List<Vector2i> _directions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandGoliathComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, ProjectileReflectAttemptEvent>(OnReflectAttempt);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, ThrowHitByEvent>(OnThrowHitBy);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTLavalandGoliathComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<ADTLavalandGoliathChargeComponent, AttemptMeleeEvent>(OnChargeAttemptMelee);
        SubscribeLocalEvent<ADTLavalandGoliathChargeComponent, StartCollideEvent>(OnChargeCollide);
        SubscribeLocalEvent<ADTLavalandGoliathChargeComponent, RefreshMovementSpeedModifiersEvent>(OnChargeRefreshSpeed);
        SubscribeLocalEvent<ADTLavalandGoliathChargeComponent, ComponentShutdown>(OnChargeShutdown);
    }

    private void OnMapInit(Entity<ADTLavalandGoliathComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThink = _timing.CurTime + ent.Comp.ThinkInterval;
        ent.Comp.NextRanged = _timing.CurTime;
        _appearance.SetData(ent, ADTLavalandGoliathVisuals.State, ADTLavalandGoliathState.Idle);
    }

    private void OnDamageChanged(Entity<ADTLavalandGoliathComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ent.Comp.NextRanged -= ent.Comp.DamageCooldownReduction;
        UpdateVisuals(ent, GetTarget(ent) != null);
    }

    private void OnReflectAttempt(Entity<ADTLavalandGoliathComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        ent.Comp.PendingHitSource = null;

        if (args.Cancelled || _mobState.IsDead(ent))
            return;

        if (HasComp<DamageMarkerOnCollideComponent>(args.ProjUid))
            return;

        if (_random.Prob(ent.Comp.ReflectChance))
        {
            if (Reflect(ent, args.ProjUid))
            {
                args.Cancelled = true;
                return;
            }
        }

        ent.Comp.PendingHitSource = args.ProjUid;
        ent.Comp.PendingHitTick = _timing.CurTick;
        ent.Comp.PendingHitIsThrow = false;
        ent.Comp.PendingHitTotal = args.Component.Damage.GetTotal().Float();
        ent.Comp.PendingHitHasBrute = HasBrute(args.Component.Damage);
    }

    private bool Reflect(Entity<ADTLavalandGoliathComponent> ent, EntityUid projectile)
    {
        if (!TryComp<PhysicsComponent>(projectile, out var physics))
            return false;

        var rotation = _random.NextAngle(-ent.Comp.ReflectSpread / 2, ent.Comp.ReflectSpread / 2).Opposite();
        var existingVelocity = _physics.GetMapLinearVelocity(projectile, component: physics);
        var relativeVelocity = existingVelocity - _physics.GetMapLinearVelocity(ent);
        var difference = rotation.RotateVec(relativeVelocity) - existingVelocity;

        _physics.SetLinearVelocity(projectile, physics.LinearVelocity + difference, body: physics);

        var localRotation = Transform(projectile).LocalRotation;
        _transform.SetLocalRotation(projectile, rotation.RotateVec(localRotation.ToVec()).ToAngle());

        _popup.PopupEntity(Loc.GetString("adt-goliath-reflect", ("projectile", projectile), ("goliath", ent.Owner)), ent, PopupType.SmallCaution);
        return true;
    }

    private void OnThrowHitBy(Entity<ADTLavalandGoliathComponent> ent, ref ThrowHitByEvent args)
    {
        ent.Comp.PendingHitSource = null;

        if (!TryComp<DamageOtherOnHitComponent>(args.Thrown, out var damage))
            return;

        ent.Comp.PendingHitSource = args.Thrown;
        ent.Comp.PendingHitTick = _timing.CurTick;
        ent.Comp.PendingHitIsThrow = true;
        ent.Comp.PendingHitTotal = damage.Damage.GetTotal().Float();
        ent.Comp.PendingHitHasBrute = HasBrute(damage.Damage);
    }

    private void OnDamageModify(Entity<ADTLavalandGoliathComponent> ent, ref DamageModifyEvent args)
    {
        if (ent.Comp.PendingHitSource is not { } source)
            return;

        ent.Comp.PendingHitSource = null;

        if (ent.Comp.PendingHitTick != _timing.CurTick)
            return;

        if (ent.Comp.PendingHitIsThrow)
        {
            if (ent.Comp.PendingHitTotal > ent.Comp.ThrowImmunityThreshold)
                return;

            args.Damage *= 0f;
            _popup.PopupEntity(Loc.GetString("adt-goliath-throw-immune", ("item", source), ("goliath", ent.Owner)), ent, PopupType.Small);
            return;
        }

        if (ent.Comp.PendingHitHasBrute || ent.Comp.PendingHitTotal >= ent.Comp.LaserResistThreshold)
            return;

        args.Damage *= ent.Comp.LaserResistMultiplier;
        _popup.PopupEntity(Loc.GetString("adt-goliath-laser-resist", ("projectile", source), ("goliath", ent.Owner)), ent, PopupType.Small);
    }

    private static bool HasBrute(DamageSpecifier damage)
    {
        foreach (var type in BruteTypes)
        {
            if (damage.DamageDict.TryGetValue(type, out var value) && value > 0)
                return true;
        }

        return false;
    }

    private void OnMeleeHit(Entity<ADTLavalandGoliathComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Weapon != ent.Owner || args.HitEntities.Count == 0)
            return;

        if (_timing.CurTime < ent.Comp.NextRanged || HasComp<ADTLavalandGoliathChargeComponent>(ent))
            return;

        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<MobStateComponent>(hit) || _mobState.IsDead(hit))
                continue;

            OpenFire(ent, hit);
            ent.Comp.NextRanged = _timing.CurTime + ent.Comp.RangedCooldown;
            UpdateVisuals(ent, true);
            return;
        }
    }

    private void OnMobStateChanged(Entity<ADTLavalandGoliathComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemCompDeferred<ADTLavalandGoliathChargeComponent>(ent);

        UpdateVisuals(ent, false);
    }

    private void OnChargeAttemptMelee(Entity<ADTLavalandGoliathChargeComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = true;
    }

    private void OnChargeRefreshSpeed(Entity<ADTLavalandGoliathChargeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Phase != ADTGoliathChargePhase.Running
            || !TryComp<ADTLavalandGoliathComponent>(ent, out var goliath)
            || !TryComp<MovementSpeedModifierComponent>(ent, out var speed)
            || speed.BaseSprintSpeed <= 0f
            || speed.BaseWalkSpeed <= 0f)
        {
            return;
        }

        args.ModifySpeed(goliath.ChargeSpeed / speed.BaseWalkSpeed, goliath.ChargeSpeed / speed.BaseSprintSpeed);
    }

    private void OnChargeCollide(Entity<ADTLavalandGoliathChargeComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.Phase != ADTGoliathChargePhase.Running || !args.OtherFixture.Hard)
            return;

        if (TryComp<ADTLavalandGoliathComponent>(ent, out var goliath)
            && TryComp<ADTMineralComponent>(args.OtherEntity, out var mineral)
            && mineral.OreAmount < goliath.MaxMineralAmount)
        {
            mineral.OreAmount++;
        }

        EndRun(ent);
    }

    private void OnChargeShutdown(Entity<ADTLavalandGoliathChargeComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        SetMoveInput(ent, Vector2.Zero);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        if (ent.Comp.WasAwake && !_mobState.IsDead(ent))
            _npc.WakeNPC(ent);

        if (TryComp<ADTLavalandGoliathComponent>(ent, out var goliath))
            UpdateVisuals((ent, goliath), GetTarget((ent, goliath)) != null);
    }

    private void EndRun(Entity<ADTLavalandGoliathChargeComponent> ent)
    {
        var recover = TryComp<ADTLavalandGoliathComponent>(ent, out var goliath)
            ? goliath.PostChargeDelay
            : TimeSpan.Zero;

        ent.Comp.Phase = ADTGoliathChargePhase.Recover;
        ent.Comp.PhaseEndsAt = _timing.CurTime + recover;
        SetMoveInput(ent, Vector2.Zero);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void SetMoveInput(EntityUid uid, Vector2 direction)
    {
        if (!TryComp<InputMoverComponent>(uid, out var mover))
            return;

        if (direction != Vector2.Zero)
            direction = (-_mover.GetParentGridAngle(mover)).RotateVec(direction);

        mover.CurTickSprintMovement = direction;
        mover.LastInputTick = _timing.CurTick;
        mover.LastInputSubTick = ushort.MaxValue;

        var ev = new SpriteMoveEvent(direction != Vector2.Zero);
        RaiseLocalEvent(uid, ref ev);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var charges = EntityQueryEnumerator<ADTLavalandGoliathChargeComponent>();
        while (charges.MoveNext(out var uid, out var charge))
        {
            UpdateCharge((uid, charge), now);
        }

        var tentacles = EntityQueryEnumerator<ADTLavalandGoliathTentacleComponent>();
        while (tentacles.MoveNext(out var uid, out var tentacle))
        {
            if (now >= tentacle.NextStage)
                AdvanceTentacle((uid, tentacle), now);
        }

        var goliaths = EntityQueryEnumerator<ADTLavalandGoliathComponent>();
        while (goliaths.MoveNext(out var uid, out var goliath))
        {
            if (now < goliath.NextThink)
                continue;

            goliath.NextThink = now + goliath.ThinkInterval;
            Think((uid, goliath), now);
        }
    }

    private void UpdateCharge(Entity<ADTLavalandGoliathChargeComponent> ent, TimeSpan now)
    {
        if (_mobState.IsDead(ent) || !TryComp<ADTLavalandGoliathComponent>(ent, out var goliath))
        {
            RemCompDeferred<ADTLavalandGoliathChargeComponent>(ent);
            return;
        }

        switch (ent.Comp.Phase)
        {
            case ADTGoliathChargePhase.WindUp:
                if (now < ent.Comp.PhaseEndsAt)
                    return;

                ent.Comp.Phase = ADTGoliathChargePhase.Running;
                ent.Comp.PhaseEndsAt = now + goliath.ChargeTimeout;
                _speed.RefreshMovementSpeedModifiers(ent.Owner);
                UpdateVisuals((ent, goliath), true);
                break;
            case ADTGoliathChargePhase.Running:
                var position = _transform.GetMapCoordinates(ent);
                var diff = ent.Comp.Destination.Position - position.Position;
                if (now >= ent.Comp.PhaseEndsAt
                    || position.MapId != ent.Comp.Destination.MapId
                    || diff.LengthSquared() < ChargeArrivalRange * ChargeArrivalRange)
                {
                    EndRun(ent);
                    return;
                }

                SetMoveInput(ent, Vector2.Normalize(diff));
                break;
            case ADTGoliathChargePhase.Recover:
                if (now >= ent.Comp.PhaseEndsAt)
                    RemCompDeferred<ADTLavalandGoliathChargeComponent>(ent);
                break;
        }
    }

    private void Think(Entity<ADTLavalandGoliathComponent> ent, TimeSpan now)
    {
        if (_mobState.IsDead(ent) || HasComp<ADTLavalandGoliathChargeComponent>(ent))
            return;

        var target = GetTarget(ent);
        UpdateVisuals(ent, target != null);

        if (target == null)
            return;

        if (ent.Comp.AncientTentacles)
            SpawnAncientTentacles(ent);

        if (now < ent.Comp.NextRanged)
            return;

        var distance = GetDistance(ent, target.Value);
        if (distance == null || distance <= ent.Comp.MeleeRange || distance > ent.Comp.RangedRange)
            return;

        OpenFire(ent, target.Value);
    }

    private void OpenFire(Entity<ADTLavalandGoliathComponent> ent, EntityUid target)
    {
        var distance = GetDistance(ent, target);
        if (distance == null)
            return;

        if (distance <= ent.Comp.MeleeRange)
        {
            MeleeTentacles(ent);
            return;
        }

        if (distance > ent.Comp.RangedRange)
            return;

        if (_random.Prob(ent.Comp.ChargeChance))
        {
            StartCharge(ent, target);
            return;
        }

        RangedTentacles(ent, target);
    }

    private void MeleeTentacles(Entity<ADTLavalandGoliathComponent> ent)
    {
        _popup.PopupEntity(Loc.GetString("adt-goliath-tentacles-around", ("goliath", ent.Owner)), ent, PopupType.MediumCaution);

        if (!TryGetTile(ent, out var grid, out var tile))
            return;

        foreach (var dir in AllDirections)
        {
            SpawnTentacle(ent, grid, tile + dir);
        }

        UpdateVisuals(ent, true);
    }

    private void RangedTentacles(Entity<ADTLavalandGoliathComponent> ent, EntityUid target)
    {
        _popup.PopupEntity(Loc.GetString("adt-goliath-tentacles-target", ("goliath", ent.Owner), ("target", target)), ent, PopupType.MediumCaution);
        ent.Comp.NextRanged = _timing.CurTime + ent.Comp.RangedCooldown;

        if (TryGetTile(target, out var grid, out var tile))
        {
            SpawnTentacle(ent, grid, tile);

            _directions.Clear();
            _directions.AddRange(Cardinals);
            for (var i = 0; i < 3; i++)
            {
                SpawnTentacle(ent, grid, tile + _random.PickAndTake(_directions));
            }
        }

        UpdateVisuals(ent, true);
    }

    private void SpawnAncientTentacles(Entity<ADTLavalandGoliathComponent> ent)
    {
        if (!TryGetTile(ent, out var grid, out var center))
            return;

        var radius = ent.Comp.AncientTentacleRadius;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                var tile = center + new Vector2i(x, y);
                if (!_map.TryGetTileRef(grid, grid.Comp, tile, out var tileRef)
                    || _turf.IsSpace(tileRef)
                    || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                {
                    continue;
                }

                if (!_random.Prob(ent.Comp.AncientTentacleChance))
                    continue;

                SpawnTentacle(ent, grid, tile);
            }
        }
    }

    private void SpawnTentacle(Entity<ADTLavalandGoliathComponent> ent, Entity<MapGridComponent> grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(grid, grid.Comp, tile, out var tileRef) || _turf.IsSpace(tileRef))
            return;

        _tileEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(grid, tile, _tileEntities, gridComp: grid.Comp);

        foreach (var uid in _tileEntities)
        {
            if (HasComp<ADTLavalandGoliathTentacleComponent>(uid))
                return;
        }

        foreach (var uid in _tileEntities)
        {
            if (HasComp<ADTMineralComponent>(uid))
                _mineral.Mine(new Entity<ADTMineralComponent?>(uid, null), 1, ent.Owner);
        }

        var tentacle = Spawn(ent.Comp.TentaclePrototype, _map.GridTileToLocal(grid, grid.Comp, tile));
        var comp = EnsureComp<ADTLavalandGoliathTentacleComponent>(tentacle);
        comp.Spawner = ent;
        comp.Stage = ADTGoliathTentacleStage.Spawning;
        comp.NextStage = _timing.CurTime + comp.SpawnTime;
        _appearance.SetData(tentacle, ADTGoliathTentacleVisuals.Stage, comp.Stage);
    }

    private void AdvanceTentacle(Entity<ADTLavalandGoliathTentacleComponent> ent, TimeSpan now)
    {
        switch (ent.Comp.Stage)
        {
            case ADTGoliathTentacleStage.Spawning:
                SetTentacleStage(ent, ADTGoliathTentacleStage.Wiggling, now + ent.Comp.WiggleTime);
                break;
            case ADTGoliathTentacleStage.Wiggling:
                if (Trip(ent))
                    SetTentacleStage(ent, ADTGoliathTentacleStage.Latched, now + ent.Comp.LatchTime);
                else
                    SetTentacleStage(ent, ADTGoliathTentacleStage.Retracting, now + ent.Comp.RetractTime);
                break;
            case ADTGoliathTentacleStage.Latched:
                SetTentacleStage(ent, ADTGoliathTentacleStage.Retracting, now + ent.Comp.RetractTime);
                break;
            case ADTGoliathTentacleStage.Retracting:
                RemComp<ADTLavalandGoliathTentacleComponent>(ent);
                QueueDel(ent);
                break;
        }
    }

    private void SetTentacleStage(Entity<ADTLavalandGoliathTentacleComponent> ent, ADTGoliathTentacleStage stage, TimeSpan next)
    {
        ent.Comp.Stage = stage;
        ent.Comp.NextStage = next;
        _appearance.SetData(ent, ADTGoliathTentacleVisuals.Stage, stage);
    }

    private bool Trip(Entity<ADTLavalandGoliathTentacleComponent> ent)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        _tileEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, tile, _tileEntities, gridComp: grid);

        var latched = false;
        foreach (var uid in _tileEntities)
        {
            if (!HasComp<MobStateComponent>(uid) || _mobState.IsDead(uid))
                continue;

            if (ent.Comp.Spawner is { } spawner
                && !TerminatingOrDeleted(spawner)
                && (uid == spawner || IsFriendly(spawner, uid)))
            {
                continue;
            }

            _popup.PopupEntity(Loc.GetString("adt-goliath-tentacle-grab", ("target", uid)), uid, PopupType.MediumCaution);

            int amount;
            if (!HasComp<StunnedComponent>(uid))
            {
                _stun.TryAddStunDuration(uid, ent.Comp.StunTime);
                amount = _random.Next(ent.Comp.MinDamage, ent.Comp.MaxDamage + 1);
            }
            else
            {
                amount = _random.Next(ent.Comp.MinStunnedDamage, ent.Comp.MaxStunnedDamage + 1);
            }

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", amount);
            _damageable.TryChangeDamage(uid, damage, origin: ent.Comp.Spawner);
            latched = true;
        }

        return latched;
    }

    private bool IsFriendly(EntityUid spawner, EntityUid target)
    {
        if (!TryComp<NpcFactionMemberComponent>(spawner, out var spawnerFaction)
            || !TryComp<NpcFactionMemberComponent>(target, out var targetFaction))
        {
            return false;
        }

        return _faction.IsEntityFriendly((spawner, spawnerFaction), (target, targetFaction));
    }

    private void StartCharge(Entity<ADTLavalandGoliathComponent> ent, EntityUid target)
    {
        var from = _transform.GetMapCoordinates(ent);
        var to = _transform.GetMapCoordinates(target);
        if (from.MapId != to.MapId)
            return;

        var diff = to.Position - from.Position;
        if (diff.LengthSquared() < 0.01f)
            return;

        var direction = Vector2.Normalize(diff);
        var charge = EnsureComp<ADTLavalandGoliathChargeComponent>(ent);
        charge.Destination = new MapCoordinates(to.Position + direction * ent.Comp.ChargePast, to.MapId);
        charge.Phase = ADTGoliathChargePhase.WindUp;
        charge.PhaseEndsAt = _timing.CurTime + ent.Comp.ChargeDelay;
        charge.WasAwake = HasComp<ActiveNPCComponent>(ent);

        _npc.SleepNPC(ent);
        SetMoveInput(ent, Vector2.Zero);
        _transform.SetWorldRotation(ent, direction.ToWorldAngle());
        _appearance.SetData(ent, ADTLavalandGoliathVisuals.State, ADTLavalandGoliathState.WindUp);

        var decoy = Spawn(ent.Comp.ChargeDecoyPrototype, from);
        _transform.SetWorldRotation(decoy, direction.ToWorldAngle());
        var fade = EnsureComp<HierophantFadeComponent>(decoy);
        fade.StartTime = _timing.CurTime;
        Dirty(decoy, fade);
    }

    private EntityUid? GetTarget(Entity<ADTLavalandGoliathComponent> ent)
    {
        if (!TryComp<HTNComponent>(ent, out var htn)
            || !htn.Blackboard.TryGetValue<EntityUid>("Target", out var target, EntityManager)
            || TerminatingOrDeleted(target)
            || _mobState.IsDead(target))
        {
            return null;
        }

        return target;
    }

    private float? GetDistance(EntityUid from, EntityUid to)
    {
        var a = _transform.GetMapCoordinates(from);
        var b = _transform.GetMapCoordinates(to);
        if (a.MapId != b.MapId)
            return null;

        return (a.Position - b.Position).Length();
    }

    private bool TryGetTile(EntityUid uid, out Entity<MapGridComponent> grid, out Vector2i tile)
    {
        grid = default;
        tile = default;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return false;

        grid = (gridUid, gridComp);
        tile = _map.TileIndicesFor(gridUid, gridComp, xform.Coordinates);
        return true;
    }

    private void UpdateVisuals(Entity<ADTLavalandGoliathComponent> ent, bool aggro)
    {
        if (TryComp<ADTLavalandGoliathChargeComponent>(ent, out var charge) && charge.Phase == ADTGoliathChargePhase.WindUp)
            return;

        var state = ADTLavalandGoliathState.Idle;
        if (aggro)
        {
            var preAttackAt = ent.Comp.NextRanged - ent.Comp.RangedCooldown * ent.Comp.PreAttackFraction;
            state = _timing.CurTime >= preAttackAt
                ? ADTLavalandGoliathState.PreAttack
                : ADTLavalandGoliathState.Aggro;
        }

        _appearance.SetData(ent, ADTLavalandGoliathVisuals.State, state);
    }
}
