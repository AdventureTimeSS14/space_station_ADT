using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Weapons.Melee;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.ADT.Chaplain.Systems;

public sealed class UnholyFervorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnholyFervorComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<UnholyFervorComponent, GetUserMeleeDamageEvent>(OnGetUserMeleeDamage);
    }

    private void OnDamageModify(Entity<UnholyFervorComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.IncomingModifiers);
    }

    private void OnGetUserMeleeDamage(Entity<UnholyFervorComponent> ent, ref GetUserMeleeDamageEvent args)
    {
        args.Modifiers.Add(ent.Comp.MeleeModifiers);
    }
}
