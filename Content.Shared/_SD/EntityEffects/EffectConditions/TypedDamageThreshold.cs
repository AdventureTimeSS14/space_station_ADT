using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.EffectConditions;

/// <summary>
/// Checking for at least this amount of damage, but only for specified types/groups
/// If we have less, this condition is false. Inverse flips the output boolean
/// </summary>
/// <remarks>
/// DamageSpecifier splits damage groups across types, we greedily revert that split to create
/// behaviour closer to what user expects; any damage in specified group contributes to that
/// group total. Use multiple conditions if you want to explicitly avoid that behaviour,
/// or don't use damage types within a group when specifying prototypes.
/// </remarks>
public sealed partial class TypedDamageThreshold : EntityEffectCondition
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool Inverse = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        if (args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damage))
        {
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var comparison = new DamageSpecifier(Damage);
            foreach (var group in protoManager.EnumeratePrototypes<DamageGroupPrototype>())
            {
                // Calculate requested threshold for this group as a sum of requested types within it
                var requestedGroup = FixedPoint2.Zero;
                foreach (var damageType in group.DamageTypes)
                {
                    if (comparison.DamageDict.TryGetValue(damageType, out var value) && value > FixedPoint2.Zero)
                        requestedGroup += value;
                }
                if (requestedGroup == FixedPoint2.Zero)
                    continue;

                if (damage.Damage.TryGetDamageInGroup(group, out var total) && total >= requestedGroup)
                    return !Inverse;

                // Remove this group's requested values from further consideration
                foreach (var damageType in group.DamageTypes)
                {
                    if (!comparison.DamageDict.TryGetValue(damageType, out var value) || value == FixedPoint2.Zero)
                        continue;

                    comparison.DamageDict[damageType] -= value;
                    if (MathF.Abs(comparison.DamageDict[damageType].Float() - MathF.Round(comparison.DamageDict[damageType].Float())) < 0.02)
                        comparison.DamageDict[damageType] = MathF.Round(comparison.DamageDict[damageType].Float());
                }
                comparison.ClampMin(0);
                comparison.TrimZeros();
            }
            comparison.ExclusiveAdd(-damage.Damage);
            comparison = -comparison;
            return comparison.AnyPositive() ^ Inverse;
        }
        return false;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        var damages = new List<string>();
        var comparison = new DamageSpecifier(Damage);
        foreach (var group in prototype.EnumeratePrototypes<DamageGroupPrototype>())
        {
            // Sum requested amounts for types within this group
            var requestedGroup = FixedPoint2.Zero;
            foreach (var damageType in group.DamageTypes)
            {
                if (comparison.DamageDict.TryGetValue(damageType, out var value) && value > FixedPoint2.Zero)
                    requestedGroup += value;
            }
            if (requestedGroup == FixedPoint2.Zero)
                continue;

            damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", group.LocalizedName),
                    ("amount", MathF.Abs(requestedGroup.Float())),
                    ("deltasign", 1))
            );

            foreach (var damageType in group.DamageTypes)
            {
                if (!comparison.DamageDict.TryGetValue(damageType, out var value) || value == FixedPoint2.Zero)
                    continue;

                comparison.DamageDict[damageType] -= value;
                if (MathF.Abs(comparison.DamageDict[damageType].Float() - MathF.Round(comparison.DamageDict[damageType].Float())) < 0.02)
                    comparison.DamageDict[damageType] = MathF.Round(comparison.DamageDict[damageType].Float());
            }
            comparison.ClampMin(0);
            comparison.TrimZeros();
        }

        foreach (var (kind, amount) in comparison.DamageDict)
        {
            damages.Add(
                Loc.GetString("health-change-display",
                    ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                    ("amount", MathF.Abs(amount.Float())),
                    ("deltasign", 1))
                );
        }

        return Loc.GetString("reagent-effect-condition-guidebook-typed-damage-threshold",
                ("inverse", Inverse),
                ("changes", ContentLocalizationManager.FormatList(damages))
                );
    }
}
