using Content.Shared.ADT.Grab;
using Content.Shared.ADT.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Weapons.Melee.Backstab;

public sealed class MeleeThrowOnHitDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MeleeThrowOnHitDamageComponent, MeleeThrowOnHitStartEvent>(ThrowStarted);
        SubscribeLocalEvent<MeleeThrownComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<MeleeThrownComponent, LandEvent>(OnLand);
    }

    private void ThrowStarted(Entity<MeleeThrowOnHitDamageComponent> ent, ref MeleeThrowOnHitStartEvent args)
    {
        var comp = EnsureComp<MeleeThrownComponent>(args.Target);
        comp.CollideDamage = ent.Comp.CollideDamage;
        comp.ToCollideDamage = ent.Comp.ToCollideDamage;
    }

    private void OnCollide(Entity<MeleeThrownComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard || !args.OurFixture.Hard)
            return;

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.CollideDamage);
        _damageable.TryChangeDamage(args.OtherEntity, ent.Comp.ToCollideDamage);
        RemComp<MeleeThrownComponent>(ent.Owner);
    }

    private void OnLand(Entity<MeleeThrownComponent> ent, ref LandEvent args)
    {
        RemComp<MeleeThrownComponent>(ent.Owner);
    }
}
