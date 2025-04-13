using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Projectiles;
using Content.Shared.ADT.ProjectileMirror;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.ADT.ProjectileMirror;

public sealed class ProjectileMirrorSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProjectileMirrorComponent, ProjectileReflectAttemptEvent>(OnReflectAttempt);
    }

    /// <summary>
    /// Handles a projectile reflection event. Performs reflection checks (direction, components, whitelist, etc.) and, 
    /// if all conditions are met, adjusts the projectile position and calls the fire method so that the projectile is launched in the desired direction.
    /// <summary>
    private void OnReflectAttempt(EntityUid uid, ProjectileMirrorComponent comp, ref ProjectileReflectAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var proj = args.ProjUid;

        if (!TryGetImpactDirection(uid, proj, out var dir)
            || !HasComp<GunComponent>(uid)
            || !TryComp<ReflectiveComponent>(proj, out var refl) || refl.Reflective == 0
            || _whitelist.IsWhitelistFail(comp.Whitelist, proj)
            || comp.ExitSide.Contains(dir.ToString())
            || !TryGetOffset(comp, dir, out var offset))
            return;

        args.Cancelled = true;

        var xform = Transform(uid);
        var newPos = xform.LocalPosition + xform.LocalRotation.RotateVec(offset);
        _xform.SetLocalPosition(proj, newPos);

        var gun = Comp<GunComponent>(uid);
        _gun.Shoot(uid, gun, proj, xform.Coordinates, new EntityCoordinates(uid, offset), out _);
    }

    /// <summary>
    /// Calculates the direction of a projectile's impact in local mirror coordinates. Used to determine from which side a projectile enters an object.
    /// </summary>
    private bool TryGetImpactDirection(EntityUid mirror, EntityUid proj, out Direction dir)
    {
        var local = Vector2.Transform(
            _xform.GetWorldPosition(proj),
            _xform.GetInvWorldMatrix(Transform(mirror)));

        dir = local.ToAngle().GetCardinalDir();
        return true;
    }

    /// <summary>
    /// Defines the offset for projectile reflection by reflection type:
    /// - TrinaryReflection: uses the predefined direction (if set).
    /// - BinaryReflection: uses the corresponding vector direction map.
    /// </summary>
    private bool TryGetOffset(ProjectileMirrorComponent comp, Direction dir, out Vector2 offset)
    {
        offset = comp.TrinaryReflection && comp.TrinaryMirrorDirection is { } vec
            ? vec.ToVec()
            : comp.BinaryReflection && ProjectileMirrorComponent.DirectionToVector.TryGetValue(dir, out var binVec)
                ? binVec
                : default;

        return offset != default;
    }
}