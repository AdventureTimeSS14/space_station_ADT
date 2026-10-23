using System.Numerics;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.ADT.Segmented;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Segmented;
using Content.Shared.ADT.Segmented.Components;
using Content.Shared.ADT.Vetus;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ADT.Vetus;

public sealed partial class ADTVetusSystem
{
    [Dependency] private ADTSegmentedSystem _segmented = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;

    private const float LegLungeLength = 1.3f;

    private readonly HashSet<Entity<MobStateComponent>> _legMobs = new();
    private readonly List<EntityUid> _legTargets = new();

    private void InitializeLegs()
    {
        SubscribeLocalEvent<ADTVetusComponent, ADTSegmentsSpawnedEvent>(OnSegmentsSpawned);
        SubscribeLocalEvent<ADTVetusComponent, MeleeHitEvent>(OnVetusMeleeHit);

        SubscribeLocalEvent<ADTVetusLegComponent, ADTSegmentDamageForwardEvent>(OnLegDamageForward);
        SubscribeLocalEvent<ADTVetusLegComponent, DamageChangedEvent>(OnLegDamageChanged);
        SubscribeLocalEvent<ADTVetusLegComponent, ADTSegmentLandedEvent>(OnLegLanded);
        SubscribeLocalEvent<ADTVetusLegComponent, PreventCollideEvent>(OnLegPreventCollide);
        SubscribeLocalEvent<ADTVetusLegComponent, BeforeExplodeEvent>(OnLegBeforeExplode);
        SubscribeLocalEvent<ADTVetusLegComponent, ElectrocutionAttemptEvent>(OnLegElectrocutionAttempt);
    }

    private void OnSegmentsSpawned(Entity<ADTVetusComponent> ent, ref ADTSegmentsSpawnedEvent args)
    {
        if (!TryComp<ADTSegmentedComponent>(ent, out var segmented))
            return;

        var now = _timing.CurTime;

        foreach (var segment in segmented.Spawned)
        {
            if (segment is not { } leg || !TryComp<ADTVetusLegComponent>(leg, out var comp))
                continue;

            comp.TurretEnabled = true;
            comp.NextShot = now;
            comp.NextRegen = now + comp.RegenInterval;
            comp.NextThink = now + GetLegThinkDelay(comp);
        }
    }

    private TimeSpan GetLegThinkDelay(ADTVetusLegComponent comp)
    {
        var jitter = comp.ThinkJitterMin;

        if (comp.ThinkJitterMax > comp.ThinkJitterMin)
            jitter += (comp.ThinkJitterMax - comp.ThinkJitterMin) * _random.NextDouble();

        return comp.ThinkInterval + jitter;
    }

    private float GetLegHealth(EntityUid leg, ADTVetusLegComponent comp)
    {
        var damage = (float) _damageable.GetTotalDamage(leg);
        return Math.Clamp(comp.MaxHealth - damage, 0f, comp.MaxHealth);
    }

    private float GetTransferRate(EntityUid leg, ADTVetusLegComponent comp)
    {
        if (comp.MaxHealth <= 0f)
            return 0f;

        return comp.BaseTransferRate * GetLegHealth(leg, comp) / comp.MaxHealth;
    }

    private void OnLegDamageForward(Entity<ADTVetusLegComponent> ent, ref ADTSegmentDamageForwardEvent args)
    {
        var rate = GetTransferRate(ent, ent.Comp);
        args.Modifier = rate;

        if (rate > 0f && rate <= ent.Comp.LowTransferWarning)
            _popup.PopupEntity(Loc.GetString("adt-vetus-leg-too-damaged", ("leg", ent.Owner)), ent, PopupType.Medium);

        if (args.Origin is not { } origin || !HasComp<MobStateComponent>(origin))
            return;

        var root = args.Root;

        if (TryComp<MegafaunaComponent>(root, out var megafauna) && !_megafauna.TryGetTarget(root, out _))
            _megafauna.TryAggro((root, megafauna), origin);

        if (ent.Comp.Target == null && CanLegAttack(root, origin))
            ent.Comp.Target = origin;
    }

    private void OnLegDamageChanged(Entity<ADTVetusLegComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var total = (float) _damageable.GetTotalDamage(ent.Owner);

        if (total > ent.Comp.MaxHealth)
            _damageable.HealEvenly(ent.Owner, ent.Comp.MaxHealth - total);

        if (!ent.Comp.TurretEnabled || GetLegHealth(ent, ent.Comp) > ent.Comp.TurretBreakHealth)
            return;

        ent.Comp.TurretEnabled = false;
        _popup.PopupEntity(Loc.GetString("adt-vetus-leg-turret-down", ("leg", ent.Owner)), ent, PopupType.MediumCaution);
    }

    private void UpdateLegs(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ADTVetusLegComponent, ADTSegmentComponent>();

        while (query.MoveNext(out var uid, out var leg, out var segment))
        {
            if (segment.Root is not { } root || TerminatingOrDeleted(root))
                continue;

            if (now >= leg.NextRegen)
            {
                leg.NextRegen = now + leg.RegenInterval;
                RegenLeg((uid, leg));
            }

            if (now < leg.NextThink)
                continue;

            leg.NextThink = now + GetLegThinkDelay(leg);

            if (!_mobState.IsDead(root))
                LegThink((uid, leg), root, now);
        }
    }

    private void RegenLeg(Entity<ADTVetusLegComponent> leg)
    {
        if (_damageable.GetTotalDamage(leg.Owner) > 0)
            _damageable.HealEvenly(leg.Owner, -leg.Comp.RegenAmount);

        if (leg.Comp.TurretEnabled || GetLegHealth(leg, leg.Comp) < leg.Comp.TurretRepairHealth)
            return;

        leg.Comp.TurretEnabled = true;
        _popup.PopupEntity(Loc.GetString("adt-vetus-leg-turret-up", ("leg", leg.Owner)), leg, PopupType.MediumCaution);
    }

    private void LegThink(Entity<ADTVetusLegComponent> leg, EntityUid root, TimeSpan now)
    {
        if (TryComp<ADTVetusComponent>(root, out var vetus) && vetus.Exploding)
            return;

        if (leg.Comp.Target is { } current && !CanLegAttack(root, current))
            leg.Comp.Target = null;

        GetLegVisibleTargets(leg, root);

        if (leg.Comp.Target == null && _legTargets.Count > 0)
            leg.Comp.Target = _random.Pick(_legTargets);

        if (leg.Comp.Target is not { } target)
            return;

        var distance = GetTileDistance(leg, target);

        if (!_legTargets.Contains(target))
        {
            if (distance > leg.Comp.VisionRange)
            {
                leg.Comp.Target = null;
                return;
            }

            if (leg.Comp.TurretEnabled && now >= leg.Comp.NextShot)
                LegOpenFire(leg, root, target, now);

            return;
        }

        if (leg.Comp.TurretEnabled && now >= leg.Comp.NextShot && distance > 1)
            LegOpenFire(leg, root, target, now);

        if (distance <= 1 && !_charge.IsCharging(root))
            LegMelee(leg, root, target);
    }

    private void GetLegVisibleTargets(Entity<ADTVetusLegComponent> leg, EntityUid root)
    {
        _legTargets.Clear();
        _legMobs.Clear();

        var range = leg.Comp.VisionRange;
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(leg), range * MathF.Sqrt(2f) + 0.5f, _legMobs);

        foreach (var mob in _legMobs)
        {
            if (!CanLegAttack(root, mob) || GetTileDistance(leg, mob) > range)
                continue;

            if (!_examine.InRangeUnOccluded(leg, mob, range * MathF.Sqrt(2f) + 0.5f))
                continue;

            _legTargets.Add(mob);
        }
    }

    private bool CanLegAttack(EntityUid root, EntityUid target)
    {
        if (target == root || TerminatingOrDeleted(target) || !HasComp<MobStateComponent>(target))
            return false;

        if (_mobState.IsDead(target))
            return false;

        return !_faction.IsEntityFriendly(root, target);
    }

    private void LegOpenFire(Entity<ADTVetusLegComponent> leg, EntityUid root, EntityUid target, TimeSpan now)
    {
        if (GetTileDistance(target, root) < leg.Comp.TurretCoreSafeRange)
            return;

        if (_random.Prob(leg.Comp.TurretSkipChance))
            return;

        var min = leg.Comp.TurretMinCooldown.TotalSeconds;
        var max = Math.Max(min, leg.Comp.TurretMaxCooldown.TotalSeconds);
        var cooldown = TimeSpan.FromSeconds(min + _random.NextDouble() * (max - min));

        if (IsFriendlyFire(leg, root, target))
            return;

        ShootProjectile(root, leg.Comp.TurretProjectile, _transform.GetMapCoordinates(leg), _transform.GetMapCoordinates(target), leg.Comp.TurretProjectileSpeed);
        _audio.PlayPvs(leg.Comp.TurretSound, leg);
        leg.Comp.NextShot = now + cooldown;
    }

    private bool IsFriendlyFire(EntityUid leg, EntityUid root, EntityUid target)
    {
        if (!TryGetTile(leg, out var gridUid, out var grid, out var from) || !TryGetTile(target, out var targetGrid, out _, out var to))
            return false;

        if (gridUid != targetGrid)
            return false;

        foreach (var tile in ADTMegafaunaGeometry.GetLine(from, to))
        {
            foreach (var entity in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid))
            {
                if (entity == leg || entity == target)
                    continue;

                if (entity == root || HasComp<ADTVetusLegComponent>(entity))
                    return true;

                if (HasComp<MobStateComponent>(entity) && _faction.IsEntityFriendly(root, entity))
                    return true;
            }
        }

        return false;
    }

    private void LegMelee(Entity<ADTVetusLegComponent> leg, EntityUid root, EntityUid target)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", leg.Comp.MeleeDamage);
        damage = _penetration.ApplyPenetration(target, damage, leg.Comp.ArmorPenetration);

        _damageable.TryChangeDamage(target, damage, origin: root);
        _popup.PopupEntity(Loc.GetString("adt-vetus-leg-stomp", ("leg", leg.Owner)), target, target, PopupType.MediumCaution);

        var legXform = Transform(leg);
        var targetPos = _transform.GetWorldPosition(target);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(legXform));

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = legXform.LocalRotation.RotateVec(localPos);

        if (localPos.Length() > LegLungeLength)
            localPos = Vector2.Normalize(localPos) * LegLungeLength;

        _melee.DoLunge(leg, leg, Angle.Zero, localPos, null, false);
    }

    private void OnLegLanded(Entity<ADTVetusLegComponent> ent, ref ADTSegmentLandedEvent args)
    {
        _audio.PlayPvs(ent.Comp.StepSound, ent);

        var root = args.Root;

        if (_mobState.IsDead(root) || !HasComp<ADTMegafaunaActiveChargeComponent>(root))
            return;

        var coords = _transform.GetMapCoordinates(ent);

        _legMobs.Clear();
        _lookup.GetEntitiesInRange(coords, ent.Comp.ChargeBumpRadius, _legMobs);

        foreach (var victim in _legMobs)
        {
            if (victim.Owner == root || _mobState.IsDead(victim))
                continue;

            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Blunt", ent.Comp.ChargeBumpDamage);
            damage = _penetration.ApplyPenetration(victim, damage, ent.Comp.ArmorPenetration);
            _damageable.TryChangeDamage(victim.Owner, damage, origin: root);

            _audio.PlayPvs(ent.Comp.ChargeBumpSound, victim);

            var kick = _transform.GetMapCoordinates(victim).Position - coords.Position;
            kick = kick.LengthSquared() < 0.01f ? _random.NextAngle().ToVec() : Vector2.Normalize(kick);
            _recoil.KickCamera(victim, kick * ent.Comp.ChargeBumpCameraKick);

            _popup.PopupEntity(Loc.GetString("adt-vetus-leg-charge-hit", ("leg", ent.Owner)), victim, victim, PopupType.LargeCaution);
        }
    }

    private void OnLegPreventCollide(Entity<ADTVetusLegComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !TryComp<ProjectileComponent>(args.OtherEntity, out var projectile))
            return;

        if (!TryComp<ADTSegmentComponent>(ent, out var segment) || segment.Root is not { } root)
            return;

        if (projectile.Shooter == root)
            args.Cancelled = true;
    }

    private void OnLegBeforeExplode(Entity<ADTVetusLegComponent> ent, ref BeforeExplodeEvent args)
    {
        var severity = GetExplosionSeverity(ent, ref args, ent.Comp.HeavyIntensity, ent.Comp.DevastationIntensity);
        ApplyExplosionDamage(ent, severity, ent.Comp.HeavyExplosionDamage, ent.Comp.DevastationExplosionDamage);
    }

    private void OnLegElectrocutionAttempt(Entity<ADTVetusLegComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnVetusMeleeHit(Entity<ADTVetusComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !HasComp<ActorComponent>(ent) || !TryComp<ADTSegmentedComponent>(ent, out var segmented))
            return;

        EntityUid? victim = null;

        foreach (var hit in args.HitEntities)
        {
            if (hit == ent.Owner || !HasComp<MobStateComponent>(hit))
                continue;

            victim = hit;
            break;
        }

        if (victim is not { } target)
            return;

        var diff = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(ent).Position;

        if (diff.LengthSquared() < 0.01f)
            return;

        var vertical = MathF.Abs(diff.Y) >= MathF.Abs(diff.X);
        var facingVec = vertical
            ? new Vector2(0f, MathF.Sign(diff.Y))
            : new Vector2(MathF.Sign(diff.X), 0f);

        for (var i = 0; i < segmented.Segments.Count && i < segmented.Spawned.Count; i++)
        {
            var entry = segmented.Segments[i];

            if (entry.Mode != ADTSegmentMode.Step || Vector2.Dot(entry.Offset, facingVec) <= 0f)
                continue;

            if (segmented.Spawned[i] is not { } leg || !TryComp<ADTVetusLegComponent>(leg, out var comp))
                continue;

            var offset = entry.Offset;
            var pull = comp.MeleePullInward;

            if (vertical)
                offset.X -= MathF.Sign(offset.X) * MathF.Min(pull, MathF.Abs(offset.X));
            else
                offset.Y -= MathF.Sign(offset.Y) * MathF.Min(pull, MathF.Abs(offset.Y));

            _segmented.SetAnchorOverride((ent.Owner, segmented), i, offset);
        }
    }
}
