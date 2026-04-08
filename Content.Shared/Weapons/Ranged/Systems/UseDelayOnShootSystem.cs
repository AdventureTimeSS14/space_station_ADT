using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public sealed class UseDelayOnShootSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UseDelayOnShootComponent, AttemptShootEvent>(OnUseShoot);//ADT tweak
    }

<<<<<<< HEAD
    private void OnUseShoot(EntityUid uid, UseDelayOnShootComponent component, ref AttemptShootEvent args)//ADT tweak
    {
        //ADT-tweak-start
        if (_delay.IsDelayed(uid))
        {
            args.Cancelled = true;
            return;
        }
        //ADT-tweak-end
        if (TryComp(uid, out UseDelayComponent? useDelay))
            _delay.TryResetDelay((uid, useDelay));
=======
    private void OnUseShoot(Entity<UseDelayOnShootComponent> ent, ref GunShotEvent args)
    {
        if (TryComp(ent, out UseDelayComponent? useDelay))
            _delay.TryResetDelay((ent, useDelay));
>>>>>>> upstreamwiz/master
    }
}
