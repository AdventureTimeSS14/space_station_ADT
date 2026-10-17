using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.ADT.Shields;

/// <summary>Общая база: классификация урона для щита.</summary>
public abstract partial class SharedShieldSystem : EntitySystem
{
    public const string HeatTypeId = "Heat";
    public const string ColdTypeId = "Cold";
    public const string CausticTypeId = "Caustic";
    public const string ShockTypeId = "Shock";
    public const string RadiationTypeId = "Radiation";

/// <summary>Переводит DamageSpecifier в одну из трёх категорий урона щита.</summary>
    public static ShieldDamType GetShieldDamType(DamageSpecifier damage)
    {
        var heat = false;
        var em = false;
        foreach (var (type, amount) in damage.DamageDict)
        {
            if (amount <= 0)
                continue;

            switch (type.Id)
            {
                case HeatTypeId:
                case ColdTypeId:
                case CausticTypeId:
                    heat = true;
                    break;
                case ShockTypeId:
                case RadiationTypeId:
                    em = true;
                    break;
            }
        }

        if (heat && !em)
            return ShieldDamType.Heat;
        if (em && !heat)
            return ShieldDamType.Em;
        return ShieldDamType.Physical;
    }

    public static float GetTotalDamage(DamageSpecifier damage)
        => damage.GetTotal().Float();
}