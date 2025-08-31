using System.Linq;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain;

public sealed partial class HolyVampirismSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MeleeHolyVampirismComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<MeleeHolyVampirismComponent> ent, ref MeleeHitEvent args)
    {
        var heal = ent.Comp.HealOnHit;
        if (heal == null || heal.GetTotal() == FixedPoint2.Zero)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (!(HasComp<PhantomPuppetComponent>(hit) ||
                  HasComp<PhantomHolderComponent>(hit) ||
                  HasComp<HereticComponent>(hit) ||
                  HasComp<VesselComponent>(hit)))
                continue;

            var thisHeal = new DamageSpecifier(heal);

            if (TryComp<HereticComponent>(hit, out var heretic))
            {
                var multiplier = (float) Math.Pow(1.5, heretic.PathStage);
                foreach (var key in thisHeal.DamageDict.Keys.ToArray())
                {
                    thisHeal.DamageDict[key] *= multiplier;
                }
            }

            // Invert the specifier to apply healing (negative damage)
            var healSpec = new DamageSpecifier();
            foreach (var (type, value) in thisHeal.DamageDict)
            {
                healSpec.DamageDict[type] = -value;
            }

            _damageable.TryChangeDamage(args.User, healSpec, origin: ent);
        }
    }
}