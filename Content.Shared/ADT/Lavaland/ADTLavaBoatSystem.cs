using System.Numerics;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.ADT.Lavaland;

public sealed class ADTLavaBoatSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private bool _reverting;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavaBoatComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<ADTLavaBoatComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<ADTLavaBoatComponent, MoveEvent>(OnMove);
    }

    private void OnStrapped(Entity<ADTLavaBoatComponent> ent, ref StrappedEvent args)
    {
        EnsureComp<ADTLavaBoatRiderComponent>(args.Buckle.Owner);
    }

    private void OnUnstrapped(Entity<ADTLavaBoatComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<ADTLavaBoatRiderComponent>(args.Buckle.Owner);
        TryMoveToShore(ent, args.Buckle.Owner);
    }

    private void TryMoveToShore(Entity<ADTLavaBoatComponent> ent, EntityUid rider)
    {
        if (TerminatingOrDeleted(rider) || TerminatingOrDeleted(ent))
            return;

        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var origin = _map.LocalToTile(gridUid, grid, xform.Coordinates);
        if (!IsSurface(ent, gridUid, grid, origin))
            return;

        var facing = (int) _transform.GetWorldRotation(rider).GetDir();
        for (var i = 0; i < 8; i++)
        {
            var offset = (i + 1) / 2 * (i % 2 == 0 ? -1 : 1);
            var dir = (Direction) ((facing + offset + 8) % 8);
            var tile = origin + dir.ToIntVec();

            if (!IsShore(ent, gridUid, grid, tile))
                continue;

            _transform.SetCoordinates(rider, _map.GridTileToLocal(gridUid, grid, tile));
            return;
        }
    }

    private bool IsShore(Entity<ADTLavaBoatComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return false;

        if (_turf.IsTileBlocked(gridUid, tile, CollisionGroup.Impassable, grid))
            return false;

        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<StepTriggerComponent>(uid.Value))
                return false;
        }

        return !IsSurface(ent, gridUid, grid, tile);
    }

    private void OnMove(Entity<ADTLavaBoatComponent> ent, ref MoveEvent args)
    {
        if (_reverting || !TryComp<VehicleComponent>(ent.Owner, out var vehicle) || vehicle.Rider == null)
            return;

        if (args.OldPosition.EntityId != args.NewPosition.EntityId)
            return;

        if (args.Component.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var oldTile = _map.LocalToTile(gridUid, grid, args.OldPosition);
        var newTile = _map.LocalToTile(gridUid, grid, args.NewPosition);
        if (oldTile == newTile || IsSurface(ent, gridUid, grid, newTile))
            return;

        _reverting = true;
        _transform.SetCoordinates(ent.Owner, args.OldPosition);
        _physics.SetLinearVelocity(ent.Owner, Vector2.Zero);
        _reverting = false;
    }

    private bool IsSurface(Entity<ADTLavaBoatComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        var anchored = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile);
        while (anchored.MoveNext(out var uid))
        {
            if (MetaData(uid.Value).EntityPrototype is { } proto && ent.Comp.Surfaces.Contains(proto.ID))
                return true;
        }

        return false;
    }
}
