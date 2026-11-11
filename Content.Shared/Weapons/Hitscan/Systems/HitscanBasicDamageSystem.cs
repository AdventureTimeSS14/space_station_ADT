using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed partial class HitscanBasicDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanBasicDamageComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanBasicDamageComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity == null)
            return;

// ADT-Tweak Start
//         var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier;
// ADT-Tweak End
        var dmg = ent.Comp.Damage * _damage.UniversalHitscanDamageModifier * GetFalloffMultiplier(ent, args.Data);

// ADT-Tweak Start
//         if(!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt, origin: args.Data.Gun))
// ADT-Tweak End
        if (!_damage.TryChangeDamage(args.Data.HitEntity.Value, dmg, out var damageDealt,
                ent.Comp.IgnoreResistances, origin: args.Data.Gun, armorPenetration: ent.Comp.ArmorPenetration))
            return;

        var damageEvent = new HitscanDamageDealtEvent
        {
            Target = args.Data.HitEntity.Value,
            DamageDealt = damageDealt,
            Data = args.Data, // ADT: blood spray / follow-ups
        };

        RaiseLocalEvent(ent, ref damageEvent);
    }
}
