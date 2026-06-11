using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Crushers.Effects;

public sealed partial class LegionSkullEffect : TrophyEffect
{
    [DataField]
    public float Coefficient = 1.3f;

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
