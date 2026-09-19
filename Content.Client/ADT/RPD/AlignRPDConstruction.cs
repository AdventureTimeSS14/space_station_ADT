using System.Numerics;
using Content.Client.Gameplay;
using Content.Client.Hands.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Coordinates;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.ADT.RPD;
using Content.Shared.ADT.RPD.Components;
using Content.Shared.ADT.RPD.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.Utility;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.ADT.RPD;

public sealed class AlignRPDConstruction : PlacementMode
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IEntityNetworkManager _entityNetwork = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly RPDSystem _rpdSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SharedAtmosPipeLayersSystem _pipeLayersSystem;
    private readonly SpriteSystem _spriteSystem;
    private readonly HandsSystem _hands = default!;

    private const float SearchBoxSize = 2f;
    private const float PlaceColorBaseAlpha = 0.5f;
    private const float MouseDeadzoneRadius = 0.25f;
    private const float GuideRadius = 0.1f;
    private const float GuideOffset = 0.21875f;

    private readonly Color _guideColor = new(0, 0, 0.5785f);
    private EntityCoordinates _unalignedMouseCoords = default;
    private AtmosPipeLayer _lastLayer = AtmosPipeLayer.Primary;
    private EntityUid? _lastHeldEntity;

    public AtmosPipeLayer Layer { get; private set; } = AtmosPipeLayer.Primary;

    /// <summary>
    /// This placement mode is not on the engine because it is content specific (i.e., for the RPD)
    /// </summary>
    public AlignRPDConstruction(PlacementManager pMan) : base(pMan)
    {
        IoCManager.InjectDependencies(this);
        _mapSystem = _entityManager.System<SharedMapSystem>();
        _rpdSystem = _entityManager.System<RPDSystem>();
        _transformSystem = _entityManager.System<SharedTransformSystem>();
        _pipeLayersSystem = _entityManager.System<SharedAtmosPipeLayersSystem>();
        _spriteSystem = _entityManager.System<SpriteSystem>();
        _hands = _entityManager.System<HandsSystem>();

        ValidPlaceColor = ValidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
    }

    public override void Render(in OverlayDrawArgs args)
    {
        if (pManager.CurrentPermission?.EntityType is { } entityType &&
            _protoManager.TryIndex<EntityPrototype>(entityType, out var proto) &&
            proto.TryGetComponent<AtmosPipeLayersComponent>(out _, _entityManager.ComponentFactory))
        {
            var gridUid = _transformSystem.GetGrid(MouseCoords);

            if (gridUid != null && _entityManager.TryGetComponent<MapGridComponent>(gridUid, out var mapGrid))
            {
                var gridRotation = _transformSystem.GetWorldRotation(gridUid.Value);
                var worldPosition = _mapSystem.LocalToWorld(gridUid.Value, mapGrid, MouseCoords.Position);
                var direction = (_eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();
                var multi = (direction == Direction.North || direction == Direction.South) ? -1f : 1f;

                args.WorldHandle.DrawCircle(worldPosition, GuideRadius, _guideColor);
                args.WorldHandle.DrawCircle(worldPosition + gridRotation.RotateVec(new Vector2(multi * GuideOffset, GuideOffset)), GuideRadius, _guideColor);
                args.WorldHandle.DrawCircle(worldPosition - gridRotation.RotateVec(new Vector2(multi * GuideOffset, GuideOffset)), GuideRadius, _guideColor);
            }
        }

        base.Render(args);
    }

    public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
    {
        _unalignedMouseCoords = ScreenToCursorGrid(mouseScreen);
        MouseCoords = _unalignedMouseCoords.AlignWithClosestGridTile(SearchBoxSize, _entityManager, _mapManager);

        var gridId = _transformSystem.GetGrid(MouseCoords);

        if (!_entityManager.TryGetComponent<MapGridComponent>(gridId, out var mapGrid))
            return;

        CurrentTile = _mapSystem.GetTileRef(gridId.Value, mapGrid, MouseCoords);

        float tileSize = mapGrid.TileSize;
        GridDistancing = tileSize;

        if (pManager.CurrentPermission!.IsTile)
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2,
                CurrentTile.Y + tileSize / 2));
        }
        else
        {
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, new Vector2(CurrentTile.X + tileSize / 2 + pManager.PlacementOffset.X,
                CurrentTile.Y + tileSize / 2 + pManager.PlacementOffset.Y));
        }

        var gridRotation = _transformSystem.GetWorldRotation(gridId.Value);
        var mouseCoordsDiff = _unalignedMouseCoords.Position - MouseCoords.Position;
        var layer = AtmosPipeLayer.Primary;

        if (mouseCoordsDiff.Length() > MouseDeadzoneRadius)
        {
            var direction = (new Angle(mouseCoordsDiff) + _eyeManager.CurrentEye.Rotation + gridRotation + Math.PI / 2).GetCardinalDir();
            layer = (direction == Direction.North || direction == Direction.East) ? AtmosPipeLayer.Secondary : AtmosPipeLayer.Tertiary;
        }

        Layer = layer;

        var player = _playerManager.LocalSession?.AttachedEntity;
        var heldEntity = player != null ? _hands.GetActiveItem(player.Value) : null;

        if (layer != _lastLayer || heldEntity != _lastHeldEntity)
        {
            _lastLayer = layer;
            _lastHeldEntity = heldEntity;

            if (heldEntity is { } activeEntity)
                _entityNetwork.SendSystemNetworkMessage(new RPDConstructionGhostLayerEvent(_entityManager.GetNetEntity(activeEntity), layer));
        }

        UpdatePlacer(layer);
    }

    private void UpdatePlacer(AtmosPipeLayer layer)
    {
        if (pManager.CurrentPermission?.EntityType == null)
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(pManager.CurrentPermission.EntityType, out var currentProto))
            return;

        if (!currentProto.TryGetComponent<AtmosPipeLayersComponent>(out var atmosPipeLayers, _entityManager.ComponentFactory))
            return;

        if (!_pipeLayersSystem.TryGetAlternativePrototype(atmosPipeLayers, layer, out var newProtoId))
            return;

        if (!_protoManager.TryIndex<EntityPrototype>(newProtoId, out var newProto))
            return;

        pManager.CurrentPermission.EntityType = newProtoId;

        if (newProto.TryGetComponent<SpriteComponent>(out var sprite, _entityManager.ComponentFactory))
        {
            var textures = new List<IDirectionalTextureProvider>();

            foreach (var spriteLayer in sprite.AllLayers)
            {
                if (spriteLayer.ActualRsi?.Path != null && spriteLayer.RsiState.Name != null)
                    textures.Add(_spriteSystem.RsiStateLike(new SpriteSpecifier.Rsi(spriteLayer.ActualRsi.Path, spriteLayer.RsiState.Name)));
            }

            pManager.CurrentTextures = textures;
        }
    }

    public override bool IsValidPosition(EntityCoordinates position)
    {
        var player = _playerManager.LocalSession?.AttachedEntity;

        if (!_entityManager.TryGetComponent<HandsComponent>(player, out var hands))
            return false;

        var heldEntity = _hands.GetActiveItem(player.Value);

        if (!_entityManager.TryGetComponent<RPDComponent>(heldEntity, out var rpd))
            return false;

        // If the destination is out of interaction range, set the placer alpha to zero
        if (!_entityManager.TryGetComponent<TransformComponent>(player, out var xform))
            return false;

        var range = rpd.CachedPrototype.Mode == RpdMode.ConstructObject ? rpd.Range : SharedInteractionSystem.InteractionRange;

        if (!xform.Coordinates.InRange(_entityManager, _transformSystem, position, range))
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(0);
            return false;
        }

        // Otherwise restore the alpha value
        else
        {
            InvalidPlaceColor = InvalidPlaceColor.WithAlpha(PlaceColorBaseAlpha);
        }

        // Retrieve the map grid data for the position
        if (!_rpdSystem.TryGetMapGridData(position, out var mapGridData))
            return false;

        // Determine if the user is hovering over a target
        var currentState = _stateManager.CurrentState;

        if (currentState is not GameplayStateBase screen)
            return false;

        var target = screen.GetClickedEntity(_unalignedMouseCoords.ToMap(_entityManager, _transformSystem));

        if (!_entityManager.TryGetComponent<MapGridComponent>(mapGridData.Value.GridUid, out var mapGrid))
        {
            return false;
        }

        if (rpd.CachedPrototype.Mode == RpdMode.Deconstruct && target == null)
        {
            return false;
        }

        if (target != null)
        {
            var tile = _mapSystem.GetTileRef(mapGridData.Value.GridUid, mapGrid, target.Value.ToCoordinates());
            var position2 = _mapSystem.TileIndicesFor(mapGridData.Value.GridUid, mapGrid, target.Value.ToCoordinates());
            // Determine if the RPD operation is valid or not
            if (!_rpdSystem.IsRPDOperationStillValid(heldEntity.Value, rpd, mapGridData.Value.GridUid, mapGrid, tile, position2, target, player.Value, false))
                return false;
        }
        else
        {
            var tile = _mapSystem.GetTileRef(mapGridData.Value.GridUid, mapGrid, mapGridData.Value.Location);
            var position2 = _mapSystem.TileIndicesFor(mapGridData.Value.GridUid, mapGrid, mapGridData.Value.Location);
            if (!_rpdSystem.IsRPDOperationStillValid(heldEntity.Value, rpd, mapGridData.Value.GridUid, mapGrid, tile, position2, null, player.Value, false))
                return false;
        }

        return true;
    }
}
