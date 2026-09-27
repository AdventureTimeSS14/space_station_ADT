using System.Numerics;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.ADT.Lavaland;

public sealed class ADTLavaBoatSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

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
