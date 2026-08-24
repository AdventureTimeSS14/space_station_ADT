// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using System.Numerics;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.CombatMode;
using Content.Shared.Wieldable.Components;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._RMC14.Weapons.Ranged.Flamer;

public sealed class RMCFlamerPreviewOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private static readonly Color FallbackColor = Color.OrangeRed;
    private const float OutlineAlpha = 0.45f;
    private const float OutlineThickness = 0.04f;

    private readonly IInputManager _input;
    private readonly IEyeManager _eye;
    private readonly IPlayerManager _player;
    private readonly GunSystem _guns;
    private readonly IMapManager _mapManager;
    private readonly SharedMapSystem _mapSystem;
    private readonly SharedTransformSystem _transform;
    private readonly SharedRMCFlamerSystem _flamer;
    private readonly EntityQuery<CombatModeComponent> _combatQ;
    private readonly EntityQuery<RMCFlamerAmmoProviderComponent> _flamerQ;
    private readonly EntityQuery<WieldableComponent> _wieldableQ;
    private readonly EntityQuery<TransformComponent> _xformQ;

    public RMCFlamerPreviewOverlay(IEntityManager ents)
    {
        _input = IoCManager.Resolve<IInputManager>();
        _eye = IoCManager.Resolve<IEyeManager>();
        _player = IoCManager.Resolve<IPlayerManager>();
        _mapManager = IoCManager.Resolve<IMapManager>();
        _guns = ents.System<GunSystem>();
        _mapSystem = ents.System<SharedMapSystem>();
        _transform = ents.System<SharedTransformSystem>();
        _flamer = ents.System<SharedRMCFlamerSystem>();
        _combatQ = ents.GetEntityQuery<CombatModeComponent>();
        _flamerQ = ents.GetEntityQuery<RMCFlamerAmmoProviderComponent>();
        _wieldableQ = ents.GetEntityQuery<WieldableComponent>();
        _xformQ = ents.GetEntityQuery<TransformComponent>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var player = _player.LocalEntity;
        if (player == null)
            return;

        if (!_combatQ.TryComp(player.Value, out var combat) || !combat.IsInCombatMode)
            return;

        if (!_guns.TryGetGun(player.Value, out var gun))
            return;

        var gunUid = gun.Owner;

        if (!_wieldableQ.TryComp(gunUid, out var wieldable) || !wieldable.Wielded)
            return;

        if (!_flamerQ.TryComp(gunUid, out var flamer))
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        if (!_xformQ.TryComp(player.Value, out var xform))
            return;

        var originMap = _transform.GetMapCoordinates(player.Value, xform: xform);
        if (originMap.MapId != mousePos.MapId)
            return;

        var fromCoordinates = xform.Coordinates;
        var toCoordinates = _transform.ToCoordinates(player.Value, mousePos);

        if (!_flamer.TryGetPreviewTiles((gunUid, flamer), fromCoordinates, toCoordinates, out var tiles))
            return;

        var baseColor = _flamer.TryGetFuelColor((gunUid, flamer), out var fuelColor) ? fuelColor : FallbackColor;
        var outlineColor = baseColor.WithAlpha(OutlineAlpha);

        var tilesByGrid = new Dictionary<EntityUid, TileSet>();
        foreach (var tile in tiles)
        {
            var tileMap = _transform.ToMapCoordinates(tile.Coordinates);
            if (tileMap.MapId != args.MapId)
                continue;

            if (!_mapManager.TryFindGridAt(tileMap, out var gridUid, out var grid))
                continue;

            var indices = _mapSystem.CoordinatesToTile(gridUid, grid, tileMap);
            if (!tilesByGrid.TryGetValue(gridUid, out var set))
            {
                set = new TileSet(grid);
                tilesByGrid.Add(gridUid, set);
            }

            set.Tiles.Add(indices);
        }

        if (tilesByGrid.Count == 0)
            return;

        var handle = args.WorldHandle;
        foreach (var (gridUid, set) in tilesByGrid)
        {
            var tileSize = set.Grid.TileSize;
            var tileSizeVec = new Vector2(tileSize, tileSize);

            foreach (var indices in set.Tiles)
            {
                var baseLocal = new Vector2(indices.X * tileSize, indices.Y * tileSize);
                var p00 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal)).Position;
                var p10 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + new Vector2(tileSize, 0f))).Position;
                var p11 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + tileSizeVec)).Position;
                var p01 = _transform.ToMapCoordinates(new EntityCoordinates(gridUid, baseLocal + new Vector2(0f, tileSize))).Position;

                if (!set.Tiles.Contains(new Vector2i(indices.X, indices.Y + 1)))
                    DrawOutline(handle, p01, p11, outlineColor);

                if (!set.Tiles.Contains(new Vector2i(indices.X, indices.Y - 1)))
                    DrawOutline(handle, p00, p10, outlineColor);

                if (!set.Tiles.Contains(new Vector2i(indices.X + 1, indices.Y)))
                    DrawOutline(handle, p10, p11, outlineColor);

                if (!set.Tiles.Contains(new Vector2i(indices.X - 1, indices.Y)))
                    DrawOutline(handle, p00, p01, outlineColor);
            }
        }
    }

    private static void DrawOutline(DrawingHandleWorld handle, Vector2 from, Vector2 to, Color color)
    {
        var delta = to - from;
        if (delta.LengthSquared() <= 0f || OutlineThickness <= 0f)
        {
            handle.DrawLine(from, to, color);
            return;
        }

        var perp = new Vector2(-delta.Y, delta.X);
        if (perp.LengthSquared() <= 0f)
        {
            handle.DrawLine(from, to, color);
            return;
        }

        perp = Vector2.Normalize(perp);
        var offset = perp * (OutlineThickness * 0.5f);

        handle.DrawLine(from, to, color);
        handle.DrawLine(from + offset, to + offset, color);
        handle.DrawLine(from - offset, to - offset, color);
    }

    private sealed class TileSet(MapGridComponent grid)
    {
        public readonly MapGridComponent Grid = grid;
        public readonly HashSet<Vector2i> Tiles = new();
    }
}
