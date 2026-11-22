using System.Linq;
using System.Numerics;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Traits.Assorted;
using Content.Shared.ADT.AnomalousCrystal;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.Language;
using Content.Shared.Administration.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Wall;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.AnomalousCrystal;

public sealed partial class ADTCrystalThemeWarpEffectSystem : EntityEffectSystem<ADTAnomalousCrystalComponent, ADTCrystalThemeWarp>
{
    [Dependency] private AreaSystem _areas = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDefs = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Effect(Entity<ADTAnomalousCrystalComponent> entity, ref EntityEffectEvent<ADTCrystalThemeWarp> args)
    {
        var xform = Transform(entity.Owner);

        if (xform.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid)
            || !TryComp<AreaGridComponent>(gridUid, out var areaGrid)
            || !_areas.TryGetArea(xform.Coordinates, out var area)
            || !_proto.TryIndex(args.Effect.Theme, out var theme)
            || !entity.Comp.WarpedAreas.Add(area.Value.Id))
        {
            return;
        }

        var tiles = areaGrid.Areas.Where(pair => pair.Value.Id == area.Value.Id).Select(pair => pair.Key).ToList();

        foreach (var index in tiles)
        {
            WarpTile(entity.Owner, args.Effect, theme, gridUid, grid, index);
        }
    }

    private void WarpTile(EntityUid crystal, ADTCrystalThemeWarp effect, ADTCrystalThemePrototype theme, EntityUid gridUid, MapGridComponent grid, Vector2i index)
    {
        var anchored = _map.GetAnchoredEntities(gridUid, grid, index).ToList();
        var wall = false;

        foreach (var uid in anchored)
        {
            if (uid == crystal)
                continue;

            if (HasComp<WallComponent>(uid))
            {
                wall = true;

                if (theme.Wall is { } newWall)
                    Replace(uid, newWall);

                continue;
            }

            if (theme.Chair is { } chair && _whitelist.IsWhitelistPass(effect.ChairWhitelist, uid))
            {
                Replace(uid, chair);
                continue;
            }

            if (theme.Table is { } table && _whitelist.IsWhitelistPass(effect.TableWhitelist, uid))
                Replace(uid, table);
        }

        if (wall || theme.Floor is not { } floor || !_tileDefs.TryGetDefinition(floor, out var floorDef))
            return;

        _map.SetTile(gridUid, grid, index, new Tile(floorDef.TileId));

        if (theme.Flora.Count == 0 || !_random.Prob(theme.FloraChance) || anchored.Count > 0)
            return;

        Spawn(_random.Pick(theme.Flora), _map.GridTileToLocal(gridUid, grid, index));
    }

    private void Replace(EntityUid old, EntProtoId proto)
    {
        var xform = Transform(old);
        var spawned = Spawn(proto, xform.Coordinates);
        _transform.SetLocalRotation(spawned, xform.LocalRotation);
        QueueDel(old);
    }
}

public sealed partial class ADTCrystalEmitProjectileEffectSystem : EntityEffectSystem<TransformComponent, ADTCrystalEmitProjectile>
{
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTCrystalEmitProjectile> args)
    {
        var direction = _transform.GetWorldRotation(entity.Owner).GetDir().ToVec();
        var projectile = Spawn(args.Effect.Projectile, _transform.GetMapCoordinates(entity.Owner));
        _gun.ShootProjectile(projectile, direction, Vector2.Zero, entity.Owner, entity.Owner, args.Effect.Speed);
    }
}

public sealed partial class ADTCrystalDarkRepriseEffectSystem : EntityEffectSystem<TransformComponent, ADTCrystalDarkReprise>
{
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTCrystalDarkReprise> args)
    {
        var coords = entity.Comp.Coordinates;

        if (args.Effect.Effect is { } effect)
            Spawn(effect, coords);

        foreach (var body in _lookup.GetEntitiesInRange<HumanoidProfileComponent>(coords, args.Effect.Range).ToList())
        {
            if (!_mobState.IsDead(body))
                continue;

            if (_polymorph.PolymorphEntity(body, args.Effect.Polymorph) is not { } revived)
                continue;

            RemCompDeferred<PolymorphedEntityComponent>(revived);
            _rejuvenate.PerformRejuvenate(revived);
            EnsureComp<UncloneableComponent>(revived);

            foreach (var language in args.Effect.RemoveLanguages)
            {
                _language.RemoveLanguage(revived, language);
            }

            if (_mind.TryGetMind(revived, out var mindId, out var mind) && mind.VisitingEntity != null)
                _mind.UnVisit(mindId, mind);

            if (args.Effect.Popup is { } popup)
                _popup.PopupEntity(Loc.GetString(popup), revived, revived, PopupType.LargeCaution);
        }
    }
}

public sealed partial class ADTAddComponentsEffectSystem : EntityEffectSystem<MetaDataComponent, ADTAddComponents>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTAddComponents> args)
    {
        EntityManager.AddComponents(entity.Owner, args.Effect.Components);

        if (args.Effect.Popup is { } popup)
            _popup.PopupEntity(Loc.GetString(popup), entity.Owner, PopupType.Medium);
    }
}

public sealed partial class ADTCrystalRefreshItemEffectSystem : EntityEffectSystem<TransformComponent, ADTCrystalRefreshItem>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTCrystalRefreshItem> args)
    {
        var xform = entity.Comp;

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var front = _map.TileIndicesFor(gridUid, grid, xform.Coordinates) + ADTAnomalousCrystalSystem.FrontOffset(xform);
        var frontCoords = _map.GridTileToLocal(gridUid, grid, front);

        if (args.Effect.Effect is { } effect)
            Spawn(effect, frontCoords);

        var candidates = new List<EntityUid>();

        foreach (var uid in _lookup.GetLocalEntitiesIntersecting(gridUid, front, 0f, LookupFlags.Uncontained))
        {
            if (!HasComp<ItemComponent>(uid) || _whitelist.IsWhitelistPass(args.Effect.Blacklist, uid))
                continue;

            if (MetaData(uid).EntityPrototype is null)
                continue;

            candidates.Add(uid);
        }

        if (candidates.Count == 0)
            return;

        var chosen = _random.Pick(candidates);
        Spawn(MetaData(chosen).EntityPrototype!.ID, frontCoords);
        QueueDel(chosen);
    }
}

public sealed partial class ADTCrystalPossessEffectSystem : EntityEffectSystem<TransformComponent, ADTCrystalPossess>
{
    [Dependency] private ADTAnomalousCrystalSystem _crystal = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTCrystalPossess> args)
    {
        if (args.User is not { } user || !_whitelist.IsWhitelistPassOrNull(args.Effect.UserWhitelist, user))
            return;

        if (!_mind.TryGetMind(user, out var mindId, out _))
            return;

        foreach (var animal in _lookup.GetEntitiesInRange<MobStateComponent>(entity.Comp.Coordinates, args.Effect.Range))
        {
            if (!CanPossess(args.Effect, animal))
                continue;

            _crystal.TryPossess(user, mindId, animal, args.Effect.ExitAction);
            return;
        }

        var xform = entity.Comp;

        if (args.Effect.Fallback is not { } fallback
            || xform.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            return;
        }

        var front = _map.TileIndicesFor(gridUid, grid, xform.Coordinates) + ADTAnomalousCrystalSystem.FrontOffset(xform);
        Spawn(fallback, _map.GridTileToLocal(gridUid, grid, front));
    }

    private bool CanPossess(ADTCrystalPossess effect, Entity<MobStateComponent> animal)
    {
        if (_mobState.IsDead(animal, animal.Comp) || HasComp<HumanoidProfileComponent>(animal))
            return false;

        if (HasComp<ADTCrystalPossessedComponent>(animal))
            return false;

        if (TryComp<MindContainerComponent>(animal, out var mindContainer) && mindContainer.HasMind)
            return false;

        if (TryComp<MeleeWeaponComponent>(animal, out var melee) && (float) melee.Damage.GetTotal() > effect.MaxMeleeDamage)
            return false;

        if (TryComp<MobThresholdsComponent>(animal, out var thresholds)
            && thresholds.Thresholds.Count > 0
            && (float) thresholds.Thresholds.Keys.Max() > effect.MaxHealth)
        {
            return false;
        }

        return true;
    }
}

public sealed partial class ADTEquipStartingGearEffectSystem : EntityEffectSystem<HumanoidProfileComponent, ADTEquipStartingGear>
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStationSpawningSystem _stationSpawning = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<ADTEquipStartingGear> args)
    {
        _hands.DropAll(entity.Owner, checkActionBlocker: false);

        if (_inventory.TryGetSlots(entity.Owner, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(entity.Owner, slot.Name, true, true);
            }
        }

        _stationSpawning.EquipStartingGear(entity.Owner, args.Effect.Gear);
    }
}
