using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Crushers.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class CrusherDashTrophyEffect : TrophyEffect
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-empowered-legion-skull"));
    }

    public override void OnMarkDetonation(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityUid user,
        EntityUid target,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        entManager.System<CrusherDashSystem>().GrantDash(user, Duration);
    }
}
