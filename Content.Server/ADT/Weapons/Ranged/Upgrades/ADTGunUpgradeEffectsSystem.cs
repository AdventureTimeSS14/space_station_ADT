using System.Linq;
using Content.Server.ADT.Mining;
using Content.Server.ADT.Mining.Resonator;
using Content.Server.ADT.PressureDamageModify;
using Content.Server.Gatherable;
using Content.Server.Gatherable.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Weapons.Ranged.Upgrades;
using Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
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

        var damage = ent.Comp.SplitAcrossProjectiles
            ? ent.Comp.Damage / bolts.Count
            : ent.Comp.Damage;

        foreach (var bolt in bolts)
        {
            if (TryComp<ProjectileComponent>(bolt, out var projectile))
                projectile.Damage += damage;
        }
    }

    private void OnRangeShot(Entity<ADTGunUpgradeRangeComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            if (TryComp<TimedDespawnComponent>(bolt, out var despawn))
                despawn.Lifetime *= ent.Comp.Coefficient;
        }
    }

    private void OnIndoorsShot(Entity<ADTGunUpgradeIndoorsComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            if (!TryComp<PressureDamageModifyComponent>(bolt, out var pressure))
                continue;

            pressure.ProjDamage = MathF.Min(pressure.ProjDamage * ent.Comp.Coefficient, 1f);
        }
    }

    private void OnVampirismShot(Entity<ADTGunUpgradeVampirismComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileVampirismComponent>(bolt);
            comp.DamageOnHit += ent.Comp.DamageOnHit;
        }
    }

    private void OnAoEShot(Entity<ADTGunUpgradeAoEComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileAoEComponent>(bolt);
            comp.MineTiles |= ent.Comp.MineTiles;
            comp.MobDamageModifier += ent.Comp.MobDamageModifier;
            comp.Radius = MathF.Max(comp.Radius, ent.Comp.Radius);
            comp.Effect ??= ent.Comp.Effect;
        }
    }

    private void OnRepeaterShot(Entity<ADTGunUpgradeRepeaterComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileRepeaterComponent>(bolt);
            comp.Gun = args.Gun;
            comp.HitCoefficient = ent.Comp.HitCoefficient;
        }
    }

    private void OnDeathSyphonShot(Entity<ADTGunUpgradeDeathSyphonComponent> ent, ref ADTGunUpgradeShotEvent args)
    {
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileDeathSyphonComponent>(bolt);
            comp.Upgrade = ent.Owner;
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
        foreach (var bolt in GetBolts(args))
        {
            var comp = EnsureComp<ADTProjectileResonatorComponent>(bolt);
            comp.FieldProto = ent.Comp.FieldProto;
            comp.BurstMultiplier = ent.Comp.BurstMultiplier;
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
        if (ent.Comp.Gun is not { } gun || !TryComp<RechargeBasicEntityAmmoComponent>(gun, out var recharge))
            return;

        if (!IsAliveMob(args.Target) && !HasComp<GatherableComponent>(args.Target))
            return;

        if (recharge.NextCharge is not { } next)
            return;

        var left = next - _timing.CurTime;
        if (left <= TimeSpan.Zero)
            return;

        recharge.NextCharge = _timing.CurTime + left * ent.Comp.HitCoefficient;
        Dirty(gun, recharge);
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

        var damage = new DamageSpecifier(_proto.Index(syphon.DamageType), FixedPoint2.New(bounty));
        _damageable.TryChangeDamage(args.Target, damage, origin: args.Shooter);
    }

    private void OnResonatorHit(Entity<ADTProjectileResonatorComponent> ent, ref ProjectileHitEvent args)
    {
        _resonator.BlastAt(ent.Comp.FieldProto, Transform(ent).Coordinates, args.Shooter, ent.Comp.BurstMultiplier);
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
