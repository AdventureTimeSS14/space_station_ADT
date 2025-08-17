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
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly StackSystem _stacks = default!;

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
        AddRadiusIfMore1(uid,uid.Comp);
        if (TryTeleport(args.Target, CountToRadius ,uid.Comp.TeleportSound))
        {
            var xform = Transform(args.Target);
            _xform.AnchorEntity(args.Target, xform);
            if (TryComp<StackComponent>(uid, out var stackComp))
                _stacks.Use(uid, stackComp.Count , stackComp);
        }
    }
    private void OnProjectileHit (Entity<BsCrystalTeleportComponent> uid,ref ProjectileHitEvent args)
    {
        TryTeleport(args.Target, uid.Comp.TeleportRadiusThrow, uid.Comp.TeleportSound);
    }

    private void AddRadiusIfMore1(Entity<BsCrystalTeleportComponent> uid ,BsCrystalTeleportComponent component)
    {
        if(TryComp<StackComponent>(uid, out var stack) && stack.Count>1) 
        {
            CountToRadius = stack.Count+component.TeleportRadiusThrow;
 
        }
        else
        {
            CountToRadius = component.TeleportRadiusThrow;
        }
    }


    private bool TryTeleport(EntityUid entity, float radius, SoundSpecifier sound)
    {
        if (!TryComp<TransformComponent>(entity, out var xform))
            return false;

        var targetCoords = SelectRandomTileInRange(xform, radius);
        if (targetCoords == null)
            return false;

        _audio.PlayPvs(sound, entity);
        _xform.SetCoordinates(entity, targetCoords.Value);
        return true;
    }

    private EntityCoordinates? TrySelectTileFromGrid(Entity<MapGridComponent> targetGrid, Vector2 userPosition, float radius)
    {
        var range = (float)Math.Sqrt(radius);
        var box = Box2.CenteredAround(userPosition, new Vector2(range, range));
        var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Owner, targetGrid.Comp, box, false);
        var tileList = new ValueList<Vector2i>();

        while (tilesInRange.MoveNext(out var tile))
        {
            tileList.Add(tile.GridIndices);
        }

        while (tileList.Count != 0)
        {
            var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
            var valid = true;

            foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Owner, targetGrid.Comp, tile))
            {
                valid = false;
                break;
            }

            if (valid)
            {
                return new EntityCoordinates(targetGrid.Owner, _mapSystem.TileCenterToVector(targetGrid, tile));
            }
        }

        return null;
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
            targetCoords = TrySelectTileFromGrid(targetGrid.Value, userCoords.Position, radius);
            if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                break;

            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
        } while (true);

        return targetCoords;
    }
}