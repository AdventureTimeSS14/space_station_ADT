using Content.Shared.Interaction.Events;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Stacks;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Server.Audio;
using Content.Server.Stack;
using System.Numerics;
using System.Linq;
using Content.Server.Atmos.Components;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.BlueSpaceCrystalTeleport;

public sealed class BsCrystalTeleportSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly StackSystem _stacks = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private HashSet<Entity<MapGridComponent>> _targetGrids = new();
    private bool valid;
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
        AddRadiusIfMore1(uid, uid.Comp);
        if (TryTeleport(args.Target, CountToRadius, uid.Comp.TeleportSound))
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

    private void AddRadiusIfMore1(Entity<BsCrystalTeleportComponent> uid, BsCrystalTeleportComponent component)
    {
        if (TryComp<StackComponent>(uid, out var stack) && stack.Count > 1)
        {
            CountToRadius = stack.Count + component.TeleportRadiusThrow;

        }
        else
        {
            CountToRadius = component.TeleportRadiusThrow;
        }
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