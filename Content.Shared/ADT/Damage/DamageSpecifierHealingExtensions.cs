using Content.Shared.Damage;

namespace Content.Shared.ADT.Damage;

public static class DamageSpecifierHealingExtensions
{
    public static bool HasHealableDamage(this DamageSpecifier healing, DamageSpecifier current)
    {
        foreach (var (type, amount) in healing.DamageDict)
        {
            if (amount >= 0)
                continue;

            if (current.DamageDict.TryGetValue(type, out var value) && value > 0)
                return true;
        }

        return false;
    }
}
