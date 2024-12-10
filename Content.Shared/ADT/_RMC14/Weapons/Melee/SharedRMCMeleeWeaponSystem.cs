using Content.Shared.Damage;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Melee;

public abstract class SharedRMCMeleeWeaponSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;

    private EntityQuery<MeleeWeaponComponent> _meleeWeaponQuery;

    public override void Initialize()
    {
        _meleeWeaponQuery = GetEntityQuery<MeleeWeaponComponent>();

        SubscribeLocalEvent<MeleeReceivedMultiplierComponent, DamageModifyEvent>(OnMeleeReceivedMultiplierDamageModify);

        SubscribeLocalEvent<MeleeDamageMultiplierComponent, MeleeHitEvent>(OnMultiplierOnHitMeleeHit);

        SubscribeAllEvent<LightAttackEvent>(OnLightAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });

        SubscribeAllEvent<HeavyAttackEvent>(OnHeavyAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });

        SubscribeAllEvent<DisarmAttackEvent>(OnDisarmAttack, before: new[] { typeof(SharedMeleeWeaponSystem) });
    }

    //Call this whenever you add MeleeResetComponent to anything
    public void MeleeResetInit(Entity<MeleeResetComponent> ent)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var weapon))
        {
            RemComp<MeleeResetComponent>(ent);
            return;
        }

        ent.Comp.OriginalTime = weapon.NextAttack;
        weapon.NextAttack = _timing.CurTime;
        Dirty(ent, weapon);
        Dirty(ent, ent.Comp);
    }

    private void OnMultiplierOnHitMeleeHit(Entity<MeleeDamageMultiplierComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var comp = ent.Comp;

        foreach (var hit in args.HitEntities)
        {
            var damage = args.BaseDamage * comp.Multiplier;
            args.BonusDamage += damage;
            break;
        }
    }

    private void OnMeleeReceivedMultiplierDamageModify(Entity<MeleeReceivedMultiplierComponent> ent, ref DamageModifyEvent args)
    {
        if (!_meleeWeaponQuery.HasComp(args.Tool))
            return;

        args.Damage = args.Damage * ent.Comp.OtherMultiplier;
    }

    private void OnLightAttack(LightAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, false);
    }

    private void OnHeavyAttack(HeavyAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon) ||
            weaponUid != GetEntity(msg.Weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, false);
    }

    private void OnDisarmAttack(DisarmAttackEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
        {
            return;
        }

        TryMeleeReset(weaponUid, weapon, true);
    }


    private void TryMeleeReset(EntityUid weaponUid, MeleeWeaponComponent weapon, bool disarm){
        if (!TryComp<MeleeResetComponent>(weaponUid, out var reset))
            return;

        if (disarm)
            weapon.NextAttack = reset.OriginalTime;

        RemComp<MeleeResetComponent>(weaponUid);
        Dirty(weaponUid, weapon);
    }
}
