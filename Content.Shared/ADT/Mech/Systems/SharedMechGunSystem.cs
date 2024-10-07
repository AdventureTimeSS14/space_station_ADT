using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;
using Content.Shared.ADT.Mech.Equipment.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Mech.Equipment.EntitySystems;
public sealed class SharedMechGunSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechGunComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnShotAttempt(EntityUid uid, MechGunComponent comp, ref ShotAttemptedEvent args)
    {
        if (!TryComp<MechComponent>(args.User, out var mech))
        {
            args.Cancel();
            return;
        }

        if (mech.Energy.Float() <= 0f)
            args.Cancel();
    }

}
