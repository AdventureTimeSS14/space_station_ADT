using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class FireRateBoostEffect : TrophyEffect
{
    [DataField]
    public float Coefficient = 1.3f;

    public override FormattedMessage GetDescription()
    {
        var bonusPercent = ((1f / Coefficient - 1f) * 100f).ToString("F0");
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-firerate-boost",
            ("percent", bonusPercent)));
    }

    public override void OnGunRefreshModifiers(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref GunRefreshModifiersEvent args)
    {
        if (Coefficient <= 0f)
            return;

        args.FireRate /= Coefficient;
    }
}
