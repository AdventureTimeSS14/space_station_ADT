using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Projectiles;
using Content.Shared.ADT.EmitterMirror;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.ADT.EmitterMirror;

public sealed class EmitterMirrorSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmitterMirrorComponent, ProjectileReflectAttemptEvent>(OnReflectAttempt);
    }

    private void OnReflectAttempt(EntityUid uid, EmitterMirrorComponent comp, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var proj = args.ProjUid;

        if (!TryGetImpactDirection(uid, proj, out var dir)
            || !HasComp<GunComponent>(uid)
            || !TryComp<ReflectiveComponent>(proj, out var refl) || refl.Reflective == 0x0
            || _whitelist.IsWhitelistFail(comp.Whitelist, proj)
            || comp.BlockedDirections.Contains(dir.ToString())
            || !TryGetOffset(comp, dir, out var offset))
            return;

        args.Cancelled = true;

        var xform = Transform(uid);
        var newPos = xform.LocalPosition + xform.LocalRotation.RotateVec(offset);

        _xform.SetLocalPosition(proj, newPos);

        _gun.Shoot(uid, Comp<GunComponent>(uid), proj, xform.Coordinates, new EntityCoordinates(uid, offset), out _);
    }

    private bool TryGetImpactDirection(EntityUid mirror, EntityUid proj, out Direction dir)
    {
        var local = Vector2.Transform(
            _xform.GetWorldPosition(proj),
            _xform.GetInvWorldMatrix(Transform(mirror)));

        dir = local.ToAngle().GetCardinalDir();
        return true;
    }

    private bool TryGetOffset(EmitterMirrorComponent comp, Direction dir, out Vector2 offset)
    {
        if (comp.TrinaryReflector && comp.TrinaryMirrorDirection is { } vec)
        {
            offset = vec.ToVec();
            return true;
        }

        if (comp.BinaryReflector && EmitterMirrorComponent.DirectionToVector.TryGetValue(dir, out offset))
            return true;

        offset = default;
        return false;
    }
}