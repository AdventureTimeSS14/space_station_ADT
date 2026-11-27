using Content.Shared.Damage.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Server.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        /*
         * On server because client doesn't want to predict other's guns.
         */

        // Automatic firing without stopping if the AutoShootGunComponent component is exist and enabled
        var query = EntityQueryEnumerator<GunComponent>();

        while (query.MoveNext(out var uid, out var gun))
        {
            // ADT-Tweak-Start: Mono fire control keeps the gun shooting at its coordinates for a while
            TryComp(uid, out AutoShootGunComponent? autoShoot);
            var fireControlled = false;
            if (autoShoot != null && autoShoot.RemainingTime > TimeSpan.Zero)
            {
                autoShoot.RemainingTime -= TimeSpan.FromSeconds(frameTime);
                fireControlled = true;
            }
            // ADT-Tweak-End

            if (gun.NextFire > Timing.CurTime)
                continue;

            if (autoShoot != null) // ADT-Tweak: TryComp moved up
            {
                // ADT-Tweak-Start
                if (fireControlled && gun.ShootCoordinates is { } coords)
                {
                    AttemptShoot(uid, (uid, gun), coords);
                    continue;
                }
                // ADT-Tweak-End

                if (!autoShoot.Enabled)
                    continue;

                AttemptShoot((uid, gun));
            }
            else if (gun.BurstActivated)
            {
                var parent = TransformSystem.GetParentUid(uid);
                if (HasComp<DamageableComponent>(parent))
                    AttemptShoot(parent, (uid, gun), gun.ShootCoordinates ?? new EntityCoordinates(uid, gun.DefaultDirection));
                else
                    AttemptShoot((uid, gun));
            }
        }
    }
}
