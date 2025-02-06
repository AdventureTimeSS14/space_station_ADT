using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Physics;

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/

namespace Content.Server.ADT.SlippingWake;

public sealed class SlippingWakeSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlippingWakeComponent, PhysicsWakeEvent>(MobWakeCheck);
    }

    public async void MobWakeCheck(EntityUid uid, SlippingWakeComponent comp, PhysicsWakeEvent args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);

        var boostedSprintSpeed = movementSpeed.BaseSprintSpeed * comp.SpeedModified;
        var boostedWalkSpeed = movementSpeed.BaseWalkSpeed * comp.SpeedModified;

        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, boostedWalkSpeed, boostedSprintSpeed, movementSpeed.Acceleration, movementSpeed);

        _stun.TryParalyze(uid, TimeSpan.FromSeconds(comp.ParalyzeTime), true);

        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed, movementSpeed.Acceleration, movementSpeed);
    }

}
