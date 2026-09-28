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

namespace Content.Server.ADT.SpeedBoostWake;

public sealed class SlippingWakeSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpeedBoostWakeComponent, PhysicsWakeEvent>(MobWakeCheck);
    }

    public async void MobWakeCheck(EntityUid uid, SpeedBoostWakeComponent comp, PhysicsWakeEvent args)
    {
        var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(uid);

        var boostedSprintSpeed = movementSpeed.BaseSprintSpeed * comp.SpeedModified;
        var boostedWalkSpeed = movementSpeed.BaseWalkSpeed * comp.SpeedModified;

        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, boostedWalkSpeed, boostedSprintSpeed, movementSpeed.Acceleration, movementSpeed);

        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(comp.ParalyzeTime));

        _movementSpeedModifierSystem?.ChangeBaseSpeed(uid, movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed, movementSpeed.Acceleration, movementSpeed);
    }

}
