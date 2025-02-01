using Content.Shared.Directions;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Abilities.Goliath;

public sealed class GoliathTentacleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GoliathSummonTentacleAction>(OnSummonAction);
        SubscribeLocalEvent<GoliathSummonRandomTentacleAction>(OnRandSummonAction);
    }

    private void OnSummonAction(GoliathSummonTentacleAction args)
    {
        if (args.Handled || args.Coords is not { } coords)
            return;

        // TODO: animation

        _popup.PopupPredicted(Loc.GetString("tentacle-ability-use-popup", ("entity", args.Performer)), args.Performer, args.Performer, type: PopupType.SmallCaution);
        _stun.TryStun(args.Performer, TimeSpan.FromSeconds(0.8f), false);

        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        for (var i = 0; i < 3; i++)
        {
            var dir = _random.PickAndTake(dirs);
            spawnPos.Add(coords.Offset(dir));
        }

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                tileRef.IsSpace() ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
        }

        args.Handled = true;
    }

    private void OnRandSummonAction(GoliathSummonRandomTentacleAction args)
    {
        if (args.Handled)
            return;

        var coords = Transform(args.Performer).Coordinates;

        // TODO: animation

        //_popup.PopupPredicted(Loc.GetString("tentacle-ability-use-popup", ("entity", args.Performer)), args.Performer, args.Performer, type: PopupType.SmallCaution);
        //_stun.TryStun(args.Performer, TimeSpan.FromSeconds(0.2f), false);

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        for (var i = 0; i < 25; i++)    // 25 попыток, не хочу засирать сервер каждым голиафом на сотни
        {
            var randVector = _random.NextVector2(6);
            coords = coords.Offset(randVector);
            if (_map.TryGetTileRef(grid, gridComp, coords, out var tileRef) &&
                !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable) &&
                _interaction.InRangeUnobstructed(args.Performer, coords, -1f))
            {
                break;
            }

        }

        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        for (var i = 0; i < 3; i++)
        {
            var dir = _random.PickAndTake(dirs);
            spawnPos.Add(coords.Offset(dir));
        }

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                tileRef.IsSpace() ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            {
                continue;
            }

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
        }

        args.Handled = true;
    }
}
