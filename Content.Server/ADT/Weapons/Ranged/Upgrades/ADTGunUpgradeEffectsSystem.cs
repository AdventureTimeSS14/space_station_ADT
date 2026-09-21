using System.Linq;
using Content.Server.ADT.Mining;
using Content.Server.ADT.Mining.Resonator;
using Content.Server.ADT.PressureDamageModify;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Weapons.KineticCooldown;
using Content.Shared.ADT.Weapons.Ranged.Upgrades;
using Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Weapons.Ranged.Upgrades;

public sealed class ADTGunUpgradeEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GatherableSystem _gatherable = default!;
    [Dependency] private readonly ADTHardRockSystem _hardRock = default!;
    [Dependency] private readonly ADTKineticCooldownSystem _cooldown = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ADTResonatorSystem _resonator = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTGunUpgradeDamageComponent, ADTGunUpgradeShotEvent>(OnDamageShot);
        SubscribeLocalEvent<ADTGunUpgradeRangeComponent, ADTGunUpgradeShotEvent>(OnRangeShot);
        SubscribeLocalEvent<ADTGunUpgradeIndoorsComponent, ADTGunUpgradeShotEvent>(OnIndoorsShot);
        SubscribeLocalEvent<ADTGunUpgradeVampirismComponent, ADTGunUpgradeShotEvent>(OnVampirismShot);
        SubscribeLocalEvent<ADTGunUpgradeAoEComponent, ADTGunUpgradeShotEvent>(OnAoEShot);
        SubscribeLocalEvent<ADTGunUpgradeRepeaterComponent, ADTGunUpgradeShotEvent>(OnRepeaterShot);
        SubscribeLocalEvent<ADTGunUpgradeDeathSyphonComponent, ADTGunUpgradeShotEvent>(OnDeathSyphonShot);
        SubscribeLocalEvent<ADTGunUpgradeTracerComponent, ADTGunUpgradeShotEvent>(OnTracerShot);
        SubscribeLocalEvent<ADTGunUpgradeResonatorComponent, ADTGunUpgradeShotEvent>(OnResonatorShot);

        SubscribeLocalEvent<ADTProjectileVampirismComponent, ProjectileHitEvent>(OnVampirismHit);
        SubscribeLocalEvent<ADTProjectileAoEComponent, ProjectileHitEvent>(OnAoEHit);
        SubscribeLocalEvent<ADTProjectileRepeaterComponent, ProjectileHitEvent>(OnRepeaterHit);
        SubscribeLocalEvent<ADTProjectileDeathSyphonComponent, ProjectileHitEvent>(OnDeathSyphonHit);
        SubscribeLocalEvent<ADTProjectileResonatorComponent, ProjectileHitEvent>(OnResonatorHit);

        SubscribeLocalEvent<ADTSyphonMarkComponent, MobStateChangedEvent>(OnMarkedMobStateChanged);
    }

    private void OnDamageShot(Entity<ADTGunUpgradeDamageComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();
        if (bolts.Count == 0)
            return;

        var weights = GetVolleyWeights(ent.Owner, bolts.Count, out var total);

        for (var i = 0; i < bolts.Count; i++)
        {
            if (!TryComp<ProjectileComponent>(bolts[i], out var projectile))
                continue;

            var share = ent.Comp.SplitAcrossProjectiles ? weights[i] / total : weights[i];
            projectile.Damage += ent.Comp.Damage * share;
        }
    }

    private void OnRangeShot(Entity<ADTGunUpgradeRangeComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();

        for (var i = 0; i < bolts.Count; i++)
        {
            if (!TryComp<TimedDespawnComponent>(bolts[i], out var despawn))
                continue;

            despawn.Lifetime *= GetVolleyCoefficient(ent.Owner, ent.Comp.Coefficient, i, bolts.Count);
        }
    }

    private void OnIndoorsShot(Entity<ADTGunUpgradeIndoorsComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();

        for (var i = 0; i < bolts.Count; i++)
        {
            if (!TryComp<PressureDamageModifyComponent>(bolts[i], out var pressure))
                continue;

            var coefficient = GetVolleyCoefficient(ent.Owner, ent.Comp.Coefficient, i, bolts.Count);
            pressure.ProjDamage = MathF.Min(pressure.ProjDamage * coefficient, 1f);
        }
    }

    private void OnVampirismShot(Entity<ADTGunUpgradeVampirismComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();
        if (bolts.Count == 0)
            return;

        var comp = EnsureComp<ADTProjectileVampirismComponent>(bolts[0]);
        comp.DamageOnHit += ent.Comp.DamageOnHit;
    }

    private void OnAoEShot(Entity<ADTGunUpgradeAoEComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var volley = new ADTVolleyTrigger();
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileAoEComponent>(bolt);
            comp.MineTiles |= ent.Comp.MineTiles;
            comp.MobDamageModifier += ent.Comp.MobDamageModifier;
            comp.Radius = MathF.Max(comp.Radius, ent.Comp.Radius);
            comp.Effect ??= ent.Comp.Effect;
            comp.Volley = volley;
        }
    }

    private void OnRepeaterShot(Entity<ADTGunUpgradeRepeaterComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();

        for (var i = 0; i < bolts.Count; i++)
        {
            var comp = EnsureComp<ADTProjectileRepeaterComponent>(bolts[i]);
            comp.Gun = args.Gun;

            var efficiency = GetVolleyEfficiency(ent.Owner, i, bolts.Count);
            comp.HitCoefficient = Math.Clamp(1f - (1f - ent.Comp.HitCoefficient) * efficiency, 0f, 1f);
        }
    }

    private void OnDeathSyphonShot(Entity<ADTGunUpgradeDeathSyphonComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var bolts = GetBolts(args).ToList();

        for (var i = 0; i < bolts.Count; i++)
        {
            var comp = EnsureComp<ADTProjectileDeathSyphonComponent>(bolts[i]);
            comp.Upgrade = ent.Owner;
            comp.Efficiency = GetVolleyEfficiency(ent.Owner, i, bolts.Count);
        }
    }

    private void OnTracerShot(Entity<ADTGunUpgradeTracerComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileTracerComponent>(bolt);
            comp.BoltColor = ent.Comp.BoltColor;
            Dirty(bolt, comp);
        }
    }

    private void OnResonatorShot(Entity<ADTGunUpgradeResonatorComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        var volley = new ADTVolleyTrigger();
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileResonatorComponent>(bolt);
            comp.FieldProto = ent.Comp.FieldProto;
            comp.BurstMultiplier = ent.Comp.BurstMultiplier;
            comp.Volley = volley;
        }
    }

    private void OnVampirismHit(Entity<ADTProjectileVampirismComponent> ent, ref ProjectileHitEvent args)
    {
        if (args.Shooter is not { } shooter || !IsAliveMob(args.Target))
            return;

        _damageable.TryChangeDamage(shooter, ent.Comp.DamageOnHit);
    }

    private void OnAoEHit(Entity<ADTProjectileAoEComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Volley is { } volley && !volley.TryUse())
            return;

        var coords = _transform.GetMapCoordinates(ent.Owner);

        if (ent.Comp.Effect is { } effect)
            Spawn(effect, coords);

        if (ent.Comp.MineTiles)
        {
            var gatherables = new HashSet<Entity<GatherableComponent>>();
            _lookup.GetEntitiesInRange(coords, ent.Comp.Radius, gatherables);

            foreach (var gatherable in gatherables)
            {
                if (gatherable.Owner == args.Target || TerminatingOrDeleted(gatherable) || _hardRock.IsHardRock(gatherable))
                    continue;

                _gatherable.Gather(gatherable, args.Shooter, gatherable.Comp);
            }
        }

        if (ent.Comp.MobDamageModifier <= 0f || !TryComp<ProjectileComponent>(ent, out var projectile))
            return;

        var splash = projectile.Damage * ent.Comp.MobDamageModifier;

        var mobs = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(coords, ent.Comp.Radius, mobs);

        foreach (var mob in mobs)
        {
            if (mob.Owner == args.Target || mob.Owner == args.Shooter)
                continue;

            _damageable.TryChangeDamage(mob.Owner, splash, origin: args.Shooter);
        }
    }

    private void OnRepeaterHit(Entity<ADTProjectileRepeaterComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Gun is not { } gun || !TryComp<ADTKineticCooldownComponent>(gun, out var cooldown))
            return;

        if (!IsAliveMob(args.Target) && !HasComp<GatherableComponent>(args.Target))
            return;

        var left = cooldown.NextUse - _timing.CurTime;
        if (left <= TimeSpan.Zero)
            return;

        _cooldown.ReduceCooldown((gun, cooldown), _timing.CurTime + left * ent.Comp.HitCoefficient);
    }

    private void OnDeathSyphonHit(Entity<ADTProjectileDeathSyphonComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Upgrade is not { } upgrade ||
            !TryComp<ADTGunUpgradeDeathSyphonComponent>(upgrade, out var syphon) ||
            !IsAliveMob(args.Target))
        {
            return;
        }

        var mark = EnsureComp<ADTSyphonMarkComponent>(args.Target);
        mark.Upgrades.Add(upgrade);

        var proto = MetaData(args.Target).EntityPrototype?.ID;
        if (proto == null || !syphon.Bounties.TryGetValue(proto, out var bounty) || bounty <= 0f)
            return;

        bounty *= MathF.Max(ent.Comp.Efficiency, 0f);
        if (bounty <= 0f)
            return;

        var damage = new DamageSpecifier(_proto.Index(syphon.DamageType), FixedPoint2.New(bounty));
        _damageable.TryChangeDamage(args.Target, damage, origin: args.Shooter);
    }

    private void OnResonatorHit(Entity<ADTProjectileResonatorComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Volley is { } volley && !volley.TryUse())
            return;

        var coords = TerminatingOrDeleted(args.Target)
            ? Transform(ent).Coordinates
            : Transform(args.Target).Coordinates;

        _resonator.BlastAt(ent.Comp.FieldProto, coords, args.Shooter, ent.Comp.BurstMultiplier);
    }

    private void OnMarkedMobStateChanged(Entity<ADTSyphonMarkComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var proto = MetaData(ent).EntityPrototype?.ID;
        if (proto == null)
            return;

        var megafauna = HasComp<MegafaunaComponent>(ent);

        foreach (var upgrade in ent.Comp.Upgrades)
        {
            if (!TryComp<ADTGunUpgradeDeathSyphonComponent>(upgrade, out var syphon))
                continue;

            var gain = syphon.Modifier * (megafauna ? syphon.MegafaunaModifier : 1f);
            var current = syphon.Bounties.GetValueOrDefault(proto);
            syphon.Bounties[proto] = MathF.Min(current + gain, syphon.MaximumBounty);
        }

        RemCompDeferred<ADTSyphonMarkComponent>(ent);
    }

    private float GetVolleyEfficiency(EntityUid upgrade, int index, int count)
    {
        if (count <= 1 || index <= 0)
            return 1f;

        var last = 0.2f;

        if (TryComp<ADTGunUpgradeComponent>(upgrade, out var comp))
            last = Math.Clamp(comp.VolleyFalloff, 0f, 1f);

        var step = index / (float)(count - 1);
        return 1f - (1f - last) * step;
    }

    private float GetVolleyCoefficient(EntityUid upgrade, float coefficient, int index, int count)
    {
        var scaled = 1f + (coefficient - 1f) * GetVolleyEfficiency(upgrade, index, count);
        return MathF.Max(scaled, 0f);
    }

    private float[] GetVolleyWeights(EntityUid upgrade, int count, out float total)
    {
        var weights = new float[count];
        total = 0f;

        for (var i = 0; i < count; i++)
        {
            weights[i] = GetVolleyEfficiency(upgrade, i, count);
            total += weights[i];
        }

        return weights;
    }

    private IEnumerable<EntityUid> GetBolts(ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in args.Projectiles)
        {
            if (HasComp<ProjectileComponent>(bolt))
                yield return bolt;
        }
    }

    private bool IsAliveMob(EntityUid target)
    {
        return TryComp<MobStateComponent>(target, out var state) && !_mobState.IsDead(target, state);
    }
}
