using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Server.Atmos.Components;
using Content.Server.Stack;
using System.Linq;
using System.Numerics;

using Robust.Server.GameObjects;

namespace Content.Server.ADT.BlueSpaceCrystalTeleport;

public sealed class BsCrystalTeleportSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private float CountToRadius;


    public override void Initialize()
    {
        SubscribeLocalEvent<BsCrystalTeleportComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BsCrystalTeleportComponent, ThrowDoHitEvent>(OnThrowInMob);
        SubscribeLocalEvent<BsCrystalTeleportComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnUseInHand(EntityUid uid, BsCrystalTeleportComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryTeleport(args.User, component.TeleportRadius, component.TeleportSound))
        {
            if (TryComp<StackComponent>(uid, out var stackComp))
                _stacks.Use(uid, 1, stackComp);

            args.Handled = true;
        }
    }

    private void OnThrowInMob(Entity<BsCrystalTeleportComponent> uid, ref ThrowDoHitEvent args)
    {
        var radius = GetThrowRadius(uid, uid.Comp);
        if (TryTeleport(args.Target, radius, uid.Comp.TeleportSound))
        {
            var xform = Transform(args.Target);
            _xform.AnchorEntity(args.Target, xform);
            if (TryComp<StackComponent>(uid, out var stackComp))
                _stacks.Use(uid, stackComp.Count, stackComp);
        }
    }
    private void OnProjectileHit(Entity<BsCrystalTeleportComponent> uid, ref ProjectileHitEvent args)
    {
        TryTeleport(args.Target, uid.Comp.TeleportRadiusThrow, uid.Comp.TeleportSound);
    }

    private float GetThrowRadius(Entity<BsCrystalTeleportComponent> uid, BsCrystalTeleportComponent component)
    {
        if (TryComp<StackComponent>(uid, out var stack) && stack.Count > 1)
            return stack.Count + component.TeleportRadiusThrow;
        return component.TeleportRadiusThrow;
    }


    private bool TryTeleport(EntityUid entity, float radius, SoundSpecifier sound)
    {
        var targetCoords = SelectRandomTileInRange(entity, radius);
        if (targetCoords == null)
            return false;

        _audio.PlayPvs(sound, entity);
        return true;
    }
    private EntityCoordinates? SelectRandomTileInRange(EntityUid uid, float radius)
    {
        EntityCoordinates coords = Transform(uid).Coordinates;
        var newCoords = new EntityCoordinates(Transform(uid).ParentUid, coords.X + _random.NextFloat(-radius, +radius), coords.Y + _random.NextFloat(-radius, +radius));

        _transform.SetCoordinates(uid, newCoords);
        _transform.AttachToGridOrMap(uid, Transform(uid));
        return newCoords;
    }
}