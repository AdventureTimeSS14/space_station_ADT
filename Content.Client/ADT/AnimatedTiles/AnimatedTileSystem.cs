using Content.Shared.ADT.AnimatedTiles;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.AnimatedTiles;

public sealed class AnimatedTileSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private AnimatedTileOverlay? _overlay;

    public readonly Dictionary<int, AnimatedTilePrototype> Animated = new();

    public readonly Dictionary<EntityUid, Dictionary<Vector2i, int>> GridTiles = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoval);

        _turf.RegisterTileDefinitions();
        BuildRegistry();

        _overlay = new AnimatedTileOverlay(this, _sprite, _map, _transform);
        _overlayManager.AddOverlay(_overlay);

        _protoManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _protoManager.PrototypesReloaded -= OnPrototypesReloaded;

        if (_overlay != null)
            _overlayManager.RemoveOverlay(_overlay);

        _overlay = null;
        GridTiles.Clear();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<AnimatedTilePrototype>())
            return;

        BuildRegistry();
        RescanGrids();
    }

    private void BuildRegistry()
    {
        Animated.Clear();

        foreach (var proto in _protoManager.EnumeratePrototypes<AnimatedTilePrototype>())
        {
            if (!_tileDefManager.TryGetDefinition(proto.ID, out var definition))
            {
                Log.Warning($"Animated tile '{proto.ID}' does not match any tile prototype, ignoring it.");
                continue;
            }

            Animated[definition.TileId] = proto;
        }
    }

    private void RescanGrids()
    {
        GridTiles.Clear();

        if (Animated.Count == 0)
            return;

        var query = AllEntityQuery<MapGridComponent>();
        while (query.MoveNext(out var uid, out var grid))
        {
            var enumerator = _map.GetAllTilesEnumerator(uid, grid);
            while (enumerator.MoveNext(out var tileRef))
            {
                SetTile(uid, tileRef.Value.GridIndices, tileRef.Value.Tile.TypeId);
            }
        }
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (Animated.Count == 0)
            return;

        var gridUid = args.Entity.Owner;

        foreach (var change in args.Changes)
        {
            SetTile(gridUid, change.GridIndices, change.NewTile.TypeId);
        }
    }

    private void OnGridRemoval(GridRemovalEvent args)
    {
        GridTiles.Remove(args.EntityUid);
    }

    private void SetTile(EntityUid gridUid, Vector2i indices, int typeId)
    {
        if (Animated.ContainsKey(typeId))
        {
            if (!GridTiles.TryGetValue(gridUid, out var tiles))
            {
                tiles = new Dictionary<Vector2i, int>();
                GridTiles[gridUid] = tiles;
            }

            tiles[indices] = typeId;
            return;
        }

        if (!GridTiles.TryGetValue(gridUid, out var existing))
            return;

        if (!existing.Remove(indices))
            return;

        if (existing.Count == 0)
            GridTiles.Remove(gridUid);
    }
}
