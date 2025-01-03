using System.Numerics;
using Content.Server.Actions;
using Content.Server.ADT.Language;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Interaction;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Salvage;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Lathe;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

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

        _stun.TryStun(uid, TimeSpan.FromSeconds(0.5f), false);
        Timer.Spawn(TimeSpan.FromSeconds(0.5f), () => Swoop(uid));
    }

    private void OnMeteors(AshDrakeMeteoritesActionEvent args)
    {
        var uid = args.Performer;

        var randVector = _random.NextVector2(6);

        var pseudoGun = Spawn("WeaponDragonMeteorites", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), new(Transform(uid).ParentUid, randVector.X, randVector.Y));
        QueueDel(pseudoGun);
        _stun.TryStun(uid, TimeSpan.FromSeconds(0.5f), false);
    }

    private void OnFire(AshDrakeFireActionEvent args)
    {
        var uid = args.Performer;
        if (!args.Coords.HasValue)
            return;

        var coords = args.Coords.Value;

        var pseudoGun = Spawn("WeaponDragonFire", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), coords);
        QueueDel(pseudoGun);
        _stun.TryStun(uid, TimeSpan.FromSeconds(0.5f), false);
    }

    private void OnBreath(AshDrakeBreathActionEvent args)
    {
        var uid = args.Performer;
        if (!args.Coords.HasValue)
            return;

        var coords = args.Coords.Value;

        var pseudoGun = Spawn("WeaponDragonBreath", Transform(uid).Coordinates);
        _gun.AttemptShoot(uid, pseudoGun, Comp<GunComponent>(pseudoGun), coords);
        QueueDel(pseudoGun);
        _stun.TryStun(uid, TimeSpan.FromSeconds(0.5f), false);
    }

    private void Swoop(EntityUid uid)
    {
        _appearance.SetData(uid, AshdrakeVisuals.Swoop, false);
        _polymorph.PolymorphEntity(uid, "SwoopDrake");
    }
}
