using System.Linq;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Damage;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Heretic.Systems;

/// <summary>
///     ADT: from Goob. Separate system since one registrar can't mix
///     ordering constraints on the same event.
/// </summary>
public sealed class ActiveMultihitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveMultihitComponent, MeleeHitEvent>(OnHit,
            after: new[] { typeof(MultihitSystem) });
    }

    private void OnHit(Entity<ActiveMultihitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (Math.Abs(ent.Comp.DamageMultiplier - 1f) > 0.01f)
        {
            var modifierSet = new DamageModifierSet
            {
                Coefficients = args.BaseDamage.DamageDict
                    .Select(x => new KeyValuePair<string, float>(x.Key, ent.Comp.DamageMultiplier))
                    .ToDictionary(),
            };

            args.ModifiersList.Add(modifierSet);
        }

        RemComp(ent.Owner, ent.Comp);
    }
}
