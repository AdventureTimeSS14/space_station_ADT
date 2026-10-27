using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.OrganHealth;

public sealed class OrganHealthSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    public bool TryGetOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> category, out Entity<OrganHealthComponent> organ)
    {
        foreach (var candidate in _body.EnumerateOrgans<OrganHealthComponent>(body))
        {
            if (candidate.Comp1.Category != category)
                continue;

            organ = (candidate.Owner, candidate.Comp2);
            return true;
        }

        organ = default;
        return false;
    }

    public bool ChangeDamage(Entity<OrganHealthComponent?> organ, float amount, OrganKind? kind = null)
    {
        if (!Resolve(organ, ref organ.Comp, false) || amount == 0f)
            return false;

        if (kind != null && organ.Comp.Kind != kind)
            return false;

        return SetDamage(organ, organ.Comp.Damage + amount);
    }

    public bool ChangeDamage(EntityUid body, ProtoId<OrganCategoryPrototype> category, float amount, OrganKind? kind = null)
    {
        return TryGetOrgan(body, category, out var organ) && ChangeDamage((organ.Owner, organ.Comp), amount, kind);
    }

    public bool SetDamage(Entity<OrganHealthComponent?> organ, float damage)
    {
        if (!Resolve(organ, ref organ.Comp, false))
            return false;

        damage = Math.Clamp(damage, 0f, organ.Comp.MaxDamage);
        if (MathHelper.CloseTo(damage, organ.Comp.Damage))
            return false;

        organ.Comp.Damage = damage;

        var oldStage = organ.Comp.Stage;
        organ.Comp.Stage = GetStage(organ.Comp);
        Dirty(organ);

        if (oldStage == organ.Comp.Stage)
            return true;

        var body = CompOrNull<OrganComponent>(organ)?.Body;
        var ev = new OrganStageChangedEvent(body, oldStage, organ.Comp.Stage);
        RaiseLocalEvent(organ, ref ev);
        return true;
    }

    public static OrganStage GetStage(OrganHealthComponent comp)
    {
        if (comp.Damage >= comp.MaxDamage)
            return OrganStage.Failing;

        if (comp.Damage > comp.MaxDamage * comp.HighThreshold)
            return OrganStage.Damaged;

        if (comp.Damage > comp.MaxDamage * comp.LowThreshold)
            return OrganStage.Bruised;

        return OrganStage.Healthy;
    }
}
