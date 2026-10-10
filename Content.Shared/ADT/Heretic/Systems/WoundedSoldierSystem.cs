using System.Linq;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class WoundedSoldierSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _dmg = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    private readonly TimeSpan _damageInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextDamage = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundedSoldierComponent, MeleeAttackEvent>(OnAttack);
        SubscribeLocalEvent<WoundedSoldierComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<WoundedSoldierComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<MeleeHitEvent>(OnHit);
    }

    private void OnExamine(Entity<WoundedSoldierComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.ExamineLoc, ("ent", Identity.Entity(ent, EntityManager))));
    }

    private void OnDamageModify(Entity<WoundedSoldierComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!_mobState.IsAlive(ent.Owner))
            return;

        var ratio = GetCritThresholdDamageRatio(ent.Owner);
        if (ratio == 0f)
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, GetResistances(ratio));
    }

    private void OnHit(MeleeHitEvent args)
    {
        var user = args.User;

        if (!TryComp(user, out WoundedSoldierComponent? soldier))
            return;

        var hitCount = args.HitEntities.Count(x => x != user && !_mobState.IsDead(x));

        if (hitCount == 0)
            return;

        var total = args.BaseDamage.GetTotal() * hitCount;

        LifeSteal(user, total * soldier.LifeStealMultiplier);
        _stamina.TryTakeStamina(user, -total.Float() * soldier.StaminaHealMultiplier);
    }

    private void OnAttack(Entity<WoundedSoldierComponent> ent, ref MeleeAttackEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon) || !TryComp(ent, out DamageableComponent? dmg))
            return;

        var ratio = GetCritThresholdDamageRatio((ent, dmg, null));

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate * MathF.Pow(ratio * 0.65f, 0.5f);
        Dirty(args.Weapon, weapon);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;

        if (now < _nextDamage)
            return;

        _nextDamage = now + _damageInterval;

        var query = EntityQueryEnumerator<WoundedSoldierComponent, MobStateComponent, MobThresholdsComponent,
            DamageableComponent>();
        while (query.MoveNext(out var uid, out var soldier, out var state, out var threshold, out var dmg))
        {
            if (state.CurrentState != MobState.Alive)
                continue;

            var ratio = 1f - GetCritThresholdDamageRatio((uid, dmg, threshold));

            if (ratio < soldier.OvertimeDamageThresholdRatio)
                continue;

            _dmg.ChangeDamage((uid, dmg),
                soldier.DamageOverTime * ratio,
                true,
                false);
        }
    }

    private void LifeSteal(EntityUid uid, FixedPoint2 amount)
    {
        if (!TryComp(uid, out DamageableComponent? damageable))
            return;

        var totalUserDamage = _dmg.GetTotalDamage((uid, damageable));
        if (totalUserDamage <= FixedPoint2.Zero)
            return;

        _dmg.HealEvenly((uid, damageable), -FixedPoint2.Min(amount, totalUserDamage));
    }

    private float GetCritThresholdDamageRatio(Entity<DamageableComponent?, MobThresholdsComponent?> ent)
    {
        if (!_threshold.TryGetThresholdForState(ent.Owner, MobState.Critical, out var threshold, ent.Comp2) &&
            !_threshold.TryGetThresholdForState(ent.Owner, MobState.Dead, out threshold, ent.Comp2) ||
            threshold == null || threshold <= 0f || !Resolve(ent, ref ent.Comp1, false))
            return 0f;

        var damage = _dmg.GetTotalDamage((ent.Owner, ent.Comp1));

        return Math.Clamp(damage.Float() / threshold.Value.Float(), 0f, 1f);
    }

    private DamageModifierSet GetResistances(float damageRatio)
    {
        var coef = 1f - 0.5f * damageRatio;
        return new()
        {
            Coefficients =
            {
                { "Blunt", coef },
                { "Slash", coef },
                { "Piercing", coef },
                { "Heat", coef },
            },
        };
    }
}
