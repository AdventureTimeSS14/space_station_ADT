using Content.Server.Polymorph.Systems;
using Content.Server.Stunnable;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.ADT.Salvage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeSwoopActionEvent>(OnSwoop);
        SubscribeLocalEvent<AshDrakeMeteoritesActionEvent>(OnMeteors);
        SubscribeLocalEvent<AshDrakeFireActionEvent>(OnFire);
        SubscribeLocalEvent<AshDrakeBreathActionEvent>(OnBreath);
    }

    private void OnSwoop(AshDrakeSwoopActionEvent args)
    {
        var uid = args.Performer;

        _appearance.SetData(uid, AshdrakeVisuals.Swoop, true);

        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(0.5f));
        Timer.Spawn(TimeSpan.FromSeconds(0.5f), () => Swoop(uid));
    }

    private void OnMeteors(AshDrakeMeteoritesActionEvent args)
    {
        var uid = args.Performer;
        var randVector = _random.NextVector2(6);

        var pseudoGun = Spawn("WeaponDragonMeteorites", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), new(Transform(uid).ParentUid, randVector.X, randVector.Y));
        QueueDel(pseudoGun);
        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(0.5f));
    }

    private void OnFire(AshDrakeFireActionEvent args)
    {
        var uid = args.Performer;
        var coords = args.Target;

        var pseudoGun = Spawn("WeaponDragonFire", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), coords);
        QueueDel(pseudoGun);
        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(0.5f));
    }

    private void OnBreath(AshDrakeBreathActionEvent args)
    {
        var uid = args.Performer;
        var coords = args.Target;

        var pseudoGun = Spawn("WeaponDragonBreath", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), coords);
        QueueDel(pseudoGun);
        _stun.TryUpdateStunDuration(uid, TimeSpan.FromSeconds(0.5f));
    }

    private void Swoop(EntityUid uid)
    {
        _appearance.SetData(uid, AshdrakeVisuals.Swoop, false);
        _polymorph.PolymorphEntity(uid, "SwoopDrake");
    }
}
