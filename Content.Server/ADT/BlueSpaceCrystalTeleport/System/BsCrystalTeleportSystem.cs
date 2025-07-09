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

namespace Content.Server.ADT.BlueSpaceCrystalTeleport;

public sealed class BsCrystalTeleportSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly StackSystem _stacks = default!;


    private EntityQuery<PhysicsComponent> _physicsQuery;
    private HashSet<Entity<MapGridComponent>> _targetGrids = [];


    public override void Initialize()
    {
        SubscribeLocalEvent<BsCrystalTeleportComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BsCrystalTeleportComponent, ThrowDoHitEvent>(OnThrowInMob);
    }
    private void OnUseInHand(EntityUid uid, BsCrystalTeleportComponent component, UseInHandEvent args)
    {
        var ent = args.User;
        if (!TryComp<BsCrystalTeleportComponent>(uid, out var crystal))
            return;
        var xform = Transform(ent);
        var targetCoords = SelectRandomTileInRange(xform, crystal.TeleportRadius);
        if (targetCoords != null)
        {
            _xform.SetCoordinates(ent, targetCoords.Value);
            _audio.PlayPvs(crystal.TeleportSound, ent);

        }
        if (TryComp<StackComponent>(uid, out var stackComp))
        {
            _stacks.Use(uid, 1, stackComp);
        }

        }


    private void OnThrowInMob(Entity<BsCrystalTeleportComponent> uid, ref ThrowDoHitEvent args)
    {


        var ent = args.Target;
        if (!TryComp<BsCrystalTeleportComponent>(uid, out var crystal))
            return;
        var xform = Transform(ent);
        var targetCoords = SelectRandomTileInRange(xform, crystal.TeleportRadiusThrow);
        if (targetCoords != null)
        {
            _xform.SetCoordinates(ent, targetCoords.Value);
            _audio.PlayPvs(crystal.TeleportSound, ent);

        }
        if (TryComp<StackComponent>(uid, out var stackComp))
        {
            _stacks.Use(uid, 1, stackComp);
        }
    }

    private EntityCoordinates? SelectRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = userXform.Coordinates.ToMap(EntityManager, _xform);
        _targetGrids.Clear();
        _lookupSystem.GetEntitiesInRange(userCoords, radius, _targetGrids);
        Entity<MapGridComponent>? targetGrid = null;

        if (_targetGrids.Count == 0)
            return null;

        // Give preference to the grid the entity is currently on.
        // This does not guarantee that if the probability fails that the owner's grid won't be picked.
        // In reality the probability is higher and depends on the number of grids.
        if (userXform.GridUid != null && TryComp<MapGridComponent>(userXform.GridUid, out var gridComp))
        {
            var userGrid = new Entity<MapGridComponent>(userXform.GridUid.Value, gridComp);
            if (_random.Prob(0.5f))
            {
                _targetGrids.Remove(userGrid);
                targetGrid = userGrid;
            }
        }

        if (targetGrid == null)
            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

        EntityCoordinates? targetCoords = null;

        do
        {
            var valid = false;

            var range = (float)Math.Sqrt(radius);
            var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
            var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Value.Owner, targetGrid.Value.Comp, box, false);
            var tileList = new ValueList<Vector2i>();

            while (tilesInRange.MoveNext(out var tile))
            {
                tileList.Add(tile.GridIndices);
            }

            while (tileList.Count != 0)
            {
                var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
                valid = true;
                foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Value.Owner, targetGrid.Value.Comp,
                             tile))
                {
                    valid = false;
                    break;
                }

                if (valid)
                {
                    targetCoords = new EntityCoordinates(targetGrid.Value.Owner,
                        _mapSystem.TileCenterToVector(targetGrid.Value, tile));
                    break;
                }
            }

            if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                break;

            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
        } while (true);

        return targetCoords;
    }
}