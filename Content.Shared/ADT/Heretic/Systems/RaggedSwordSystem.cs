using System.Linq;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class RaggedSwordSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaggedSwordComponent, MeleeHitEvent>(OnMeleeHit, after: new[] { typeof(MobStateSystem) });
    }

    private void OnMeleeHit(Entity<RaggedSwordComponent> ent, ref MeleeHitEvent args)
    {
        var owner = args.User;
        var amount = ent.Comp.MaxHealthPerKill;
        var victims = args.HitEntities.ToList();

        Timer.Spawn(50, () => GrantForKills(owner, amount, victims));
    }

    private void GrantForKills(EntityUid owner, float amount, List<EntityUid> victims)
    {
        if (TerminatingOrDeleted(owner))
            return;

        var killed = false;

        foreach (var victim in victims)
        {
            if (TerminatingOrDeleted(victim) || victim == owner)
                continue;

            if (!HasComp<HumanoidProfileComponent>(victim))
                continue;

            if (TryComp<MobStateComponent>(victim, out var state) && _mobState.IsDead(victim, state))
                killed = true;
        }

        if (killed)
            GrantMaxHealth(owner, amount);
    }

    private void GrantMaxHealth(EntityUid owner, float amount)
    {
        if (!TryComp<MobThresholdsComponent>(owner, out var thresholds))
            return;

        foreach (var (damage, state) in thresholds.Thresholds.ToList())
            _threshold.SetMobStateThreshold(owner, damage + amount, state, thresholds);

        if (!TryComp<DamageableComponent>(owner, out var damageable))
            return;

        var heal = new DamageSpecifier();
        foreach (var key in damageable.Damage.DamageDict.Keys.ToList())
            heal.DamageDict[key] = -amount;

        _damage.TryChangeDamage(owner, heal, true);
    }
}