using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class DeadlyShotTrophyEffect : TrophyEffect
{
    [DataField]
    public DamageSpecifier BonusDamage = new();

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-blaster-tubes", ("damage", BonusDamage.GetTotal())));
    }

    public override void OnMarkDetonation(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityUid user,
        EntityUid target,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        var timing = IoCManager.Resolve<IGameTiming>();
        var comp = entManager.EnsureComponent<DeadlyShotActiveEffectComponent>(holder.Owner);
        comp.ExpireTime = timing.CurTime + Duration;
    }

    public override void OnGunShot(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref GunShotEvent args)
    {
        if (!entManager.TryGetComponent<DeadlyShotActiveEffectComponent>(holder.Owner, out var active))
            return;

        entManager.RemoveComponentDeferred<DeadlyShotActiveEffectComponent>(holder.Owner);

        var timing = IoCManager.Resolve<IGameTiming>();

        if (timing.CurTime > active.ExpireTime)
            return;

        foreach (var (uid, _) in args.Ammo)
        {
            if (uid is not { } ammo || !entManager.TryGetComponent<ProjectileComponent>(ammo, out var projectile))
                continue;

            projectile.Damage = projectile.Damage + BonusDamage;
        }
    }
}
