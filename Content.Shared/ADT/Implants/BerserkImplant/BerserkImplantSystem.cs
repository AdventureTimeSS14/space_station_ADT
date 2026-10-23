using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.ADT.Implants.BerserkImplant;

public sealed class BerserkImplantSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ImplantCritThresholdSystem _crit = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BerserkImplantActiveComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<BerserkImplantActiveComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<BerserkImplantActiveComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<BerserkImplantActiveComponent, BeforeStaminaDamageEvent>(OnStaminaDamageModify);
        SubscribeLocalEvent<BerserkImplantActiveComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    private void OnRemove(Entity<BerserkImplantActiveComponent> ent, ref ComponentRemove args)
    {
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.DelayedDamage * ent.Comp.DelayedDamageModifier, true);
    }

    private void OnDamageModify(Entity<BerserkImplantActiveComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.DamageModifier);
    }

    private void OnBeforeDamageChanged(Entity<BerserkImplantActiveComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!_mobState.IsAlive(ent.Owner))
            return;

        if (!_crit.TryGetFirstCritThreshold(ent.Owner, out var threshold))
            return;

        var currentDamage = _damageable.GetTotalDamage(ent.Owner);

        if (currentDamage + args.Damage.GetTotal() < threshold)
            return;

        args.Cancelled = true;

        if (ent.Comp.DelayedDamage.GetTotal() < 150)
            ent.Comp.DelayedDamage += args.Damage;
    }

    private void OnStaminaDamageModify(Entity<BerserkImplantActiveComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Value *= ent.Comp.StunModifier;
    }

    private void OnGetMeleeDamage(ref GetMeleeDamageEvent args)
    {
        if (!TryComp<BerserkImplantActiveComponent>(args.User, out var berserk))
            return;

        args.Damage *= berserk.SelfDamageModifier;
    }

    private void OnShotAttempted(Entity<BerserkImplantActiveComponent> ent, ref ShotAttemptedEvent args)
    {
        args.Cancel();
    }
}
