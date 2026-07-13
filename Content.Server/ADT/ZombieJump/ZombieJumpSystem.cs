using Content.Shared.ADT.ZombieJump;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server.ADT.ZombieJump;
public sealed partial class ZombieJumpSystem : SharedZombieJumpSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZombieJumpEvent>(OnZombieJumpServer);
    }
    public void ExecuteJump(EntityUid uid, ZombieJumpComponent jumpComp)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) ||
            !TryComp<PhysicsComponent>(uid, out var physics))
        {
            return;
        }

        if (xform.Anchored)
        {
            _transform.Unanchor(uid, xform);
        }

        _physics.SetAwake(uid, physics, true);

        var direction = xform.LocalRotation.ToWorldVec();
        var targetCoords = xform.Coordinates.Offset(direction * jumpComp.JumpDistance);
        _throwing.TryThrow(uid, targetCoords, jumpComp.JumpThrowSpeed);

        EnsureComp<ActiveZombieLeaperComponent>(uid, out var leaperComp);
        leaperComp.KnockdownDuration = jumpComp.CollideKnockdown;
    }

    private void OnZombieJumpServer(ZombieJumpEvent args)
    {
        if (args.Handled)
            return;

        var uid = args.Performer;

        if (!TryComp<ZombieJumpComponent>(uid, out var jumpComp))
        {
            return;
        }

        ExecuteJump(uid, jumpComp);

        args.Handled = true;
    }

    protected override void TryStunAndKnockdown(EntityUid uid, TimeSpan duration)
    {
        _stun.TryAddStunDuration(uid, TimeSpan.FromSeconds(1));

        if (TryComp(uid, out StunVisualsComponent? starsComp) &&
            TryComp(uid, out AppearanceComponent? appearance))
        {
            _appearance.SetData(uid, SharedStunSystem.StunVisuals.SeeingStars, true, appearance);
        }

        if (duration > TimeSpan.FromSeconds(1))
        {
            _stun.TryKnockdown(uid, duration, force: true);
        }
    }
}
