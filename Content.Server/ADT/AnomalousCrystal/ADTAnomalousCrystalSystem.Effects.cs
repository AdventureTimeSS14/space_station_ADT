using System.Linq;
using System.Numerics;
using Content.Server.Polymorph.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Traits.Assorted;
using Content.Shared.Actions;
using Content.Shared.ADT.AnomalousCrystal;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.Language;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Station;
using Content.Shared.Wall;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.AnomalousCrystal;

public sealed partial class ADTAnomalousCrystalSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private AreaSystem _areas = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedGodmodeSystem _godmode = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedLanguageSystem _language = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SharedStationSpawningSystem _stationSpawning = default!;
    [Dependency] private ITileDefinitionManager _tileDefs = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const string CollectiveMindLanguage = "ADTShadowlingCollectiveMind";

    private void InitializeEffects()
    {
        SubscribeLocalEvent<ADTCrystalThemeWarpComponent, MapInitEvent>(OnThemeWarpInit);
        SubscribeLocalEvent<ADTCrystalEmitterComponent, MapInitEvent>(OnEmitterInit);

        SubscribeLocalEvent<ADTCrystalHonkComponent, ADTCrystalActivatedEvent>(OnHonk);
        SubscribeLocalEvent<ADTCrystalThemeWarpComponent, ADTCrystalActivatedEvent>(OnThemeWarp);
        SubscribeLocalEvent<ADTCrystalEmitterComponent, ADTCrystalActivatedEvent>(OnEmitter);
        SubscribeLocalEvent<ADTCrystalDarkRepriseComponent, ADTCrystalActivatedEvent>(OnDarkReprise);
        SubscribeLocalEvent<ADTCrystalHelpersComponent, ADTCrystalActivatedEvent>(OnHelpers);
        SubscribeLocalEvent<ADTCrystalRefresherComponent, ADTCrystalActivatedEvent>(OnRefresher);
        SubscribeLocalEvent<ADTCrystalPossessorComponent, ADTCrystalActivatedEvent>(OnPossessor);

        SubscribeLocalEvent<ADTCrystalPossessedComponent, ADTExitPossessionActionEvent>(OnExitPossession);
        SubscribeLocalEvent<ADTCrystalPossessedComponent, MobStateChangedEvent>(OnPossessedStateChanged);
    }

    private void OnThemeWarpInit(Entity<ADTCrystalThemeWarpComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Themes.Count > 0)
            ent.Comp.Theme = _random.Next(ent.Comp.Themes.Count);
    }

    private void OnEmitterInit(Entity<ADTCrystalEmitterComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Projectiles.Count > 0)
            ent.Comp.Projectile = _random.Pick(ent.Comp.Projectiles);
    }

    private void OnHonk(Entity<ADTCrystalHonkComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        if (args.User is not { } user || !HasComp<HumanoidProfileComponent>(user))
            return;

        if (!TryComp<ADTAnomalousCrystalComponent>(ent, out var crystal) || !crystal.Affected.Add(user))
            return;

        _hands.DropAll(user, checkActionBlocker: false);

        if (_inventory.TryGetSlots(user, out var slots))
        {
            foreach (var slot in slots)
            {
                _inventory.TryUnequip(user, slot.Name, true, true);
            }
        }

        _stationSpawning.EquipStartingGear(user, ent.Comp.Gear);
    }

    private void OnThemeWarp(Entity<ADTCrystalThemeWarpComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        if (ent.Comp.Theme < 0 || ent.Comp.Theme >= ent.Comp.Themes.Count)
            return;

        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid
            || !TryComp<MapGridComponent>(gridUid, out var grid)
            || !TryComp<AreaGridComponent>(gridUid, out var areaGrid)
            || !_areas.TryGetArea(xform.Coordinates, out var area)
            || !ent.Comp.WarpedAreas.Add(area.Value.Id))
        {
            return;
        }

        var theme = ent.Comp.Themes[ent.Comp.Theme];
        var tiles = areaGrid.Areas.Where(pair => pair.Value.Id == area.Value.Id).Select(pair => pair.Key).ToList();

        foreach (var index in tiles)
        {
            WarpTile(ent, theme, gridUid, grid, index);
        }
    }

    private void WarpTile(Entity<ADTCrystalThemeWarpComponent> ent, ADTCrystalTheme theme, EntityUid gridUid, MapGridComponent grid, Vector2i index)
    {
        var anchored = _map.GetAnchoredEntities(gridUid, grid, index).ToList();
        var wall = false;

        foreach (var uid in anchored)
        {
            if (uid == ent.Owner)
                continue;

            if (HasComp<WallComponent>(uid))
            {
                wall = true;

                if (theme.Wall is { } newWall)
                    Replace(uid, newWall);

                continue;
            }

            if (theme.Chair is { } chair && _whitelist.IsWhitelistPass(ent.Comp.ChairWhitelist, uid))
            {
                Replace(uid, chair);
                continue;
            }

            if (theme.Table is { } table && _whitelist.IsWhitelistPass(ent.Comp.TableWhitelist, uid))
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

    private void OnEmitter(Entity<ADTCrystalEmitterComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        if (ent.Comp.Projectile is not { } proto)
            return;

        var direction = _transform.GetWorldRotation(ent).GetDir().ToVec();
        var projectile = Spawn(proto, _transform.GetMapCoordinates(ent));
        _gun.ShootProjectile(projectile, direction, Vector2.Zero, ent, ent, ent.Comp.Speed);
    }

    private void OnDarkReprise(Entity<ADTCrystalDarkRepriseComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        var coords = Transform(ent).Coordinates;

        Spawn(ent.Comp.Effect, coords);

        foreach (var body in _lookup.GetEntitiesInRange<HumanoidProfileComponent>(coords, ent.Comp.Range).ToList())
        {
            if (!_mobState.IsDead(body))
                continue;

            var config = new PolymorphConfiguration
            {
                Entity = ent.Comp.Mob,
                TransferDamage = false,
                Forced = true,
                Inventory = PolymorphInventoryChange.Transfer,
                RevertOnCrit = false,
                RevertOnDeath = false,
                TransferName = true,
            };

            if (_polymorph.PolymorphEntity(body, config) is not { } revived)
                continue;

            RemCompDeferred<PolymorphedEntityComponent>(revived);
            _rejuvenate.PerformRejuvenate(revived);
            _language.RemoveLanguage(revived, CollectiveMindLanguage);
            EnsureComp<UncloneableComponent>(revived);

            if (_mind.TryGetMind(revived, out var mindId, out var mind) && mind.VisitingEntity != null)
                _mind.UnVisit(mindId, mind);

            _popup.PopupEntity(Loc.GetString("adt-crystal-dark-reprise-revive"), revived, revived, PopupType.LargeCaution);
        }
    }

    private void OnHelpers(Entity<ADTCrystalHelpersComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        if (ent.Comp.Ready)
            return;

        ent.Comp.Ready = true;
        EntityManager.AddComponents(ent, ent.Comp.RoleComponents);
        _popup.PopupEntity(Loc.GetString("adt-crystal-helpers-ready"), ent, PopupType.Medium);
    }

    private void OnRefresher(Entity<ADTCrystalRefresherComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var front = _map.TileIndicesFor(gridUid, grid, xform.Coordinates) + DirectionToOffset(xform);
        var frontCoords = _map.GridTileToLocal(gridUid, grid, front);

        Spawn(ent.Comp.Effect, frontCoords);

        var candidates = new List<EntityUid>();

        foreach (var uid in _lookup.GetLocalEntitiesIntersecting(gridUid, front, 0f, LookupFlags.Uncontained))
        {
            if (!HasComp<ItemComponent>(uid) || _whitelist.IsWhitelistPass(ent.Comp.Blacklist, uid))
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

    private void OnPossessor(Entity<ADTCrystalPossessorComponent> ent, ref ADTCrystalActivatedEvent args)
    {
        if (args.User is not { } user || !HasComp<HumanoidProfileComponent>(user))
            return;

        if (!_mind.TryGetMind(user, out var mindId, out _))
            return;

        var coords = Transform(ent).Coordinates;

        foreach (var animal in _lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.Range))
        {
            if (!CanPossess(ent, animal))
                continue;

            Possess(ent, user, mindId, animal);
            return;
        }

        var xform = Transform(ent);

        if (xform.GridUid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var grid))
        {
            var front = _map.TileIndicesFor(gridUid, grid, xform.Coordinates) + DirectionToOffset(xform);
            Spawn(ent.Comp.Fallback, _map.GridTileToLocal(gridUid, grid, front));
        }
    }

    private bool CanPossess(Entity<ADTCrystalPossessorComponent> ent, Entity<MobStateComponent> animal)
    {
        if (_mobState.IsDead(animal, animal.Comp) || HasComp<HumanoidProfileComponent>(animal))
            return false;

        if (HasComp<ADTCrystalPossessedComponent>(animal))
            return false;

        if (TryComp<MindContainerComponent>(animal, out var mindContainer) && mindContainer.HasMind)
            return false;

        if (TryComp<MeleeWeaponComponent>(animal, out var melee) && (float) melee.Damage.GetTotal() > ent.Comp.MaxMeleeDamage)
            return false;

        if (TryComp<MobThresholdsComponent>(animal, out var thresholds)
            && thresholds.Thresholds.Count > 0
            && (float) thresholds.Thresholds.Keys.Max() > ent.Comp.MaxHealth)
        {
            return false;
        }

        return true;
    }

    private void Possess(Entity<ADTCrystalPossessorComponent> ent, EntityUid user, EntityUid mindId, EntityUid animal)
    {
        var possessed = EnsureComp<ADTCrystalPossessedComponent>(animal);
        var stasis = _container.EnsureContainer<ContainerSlot>(animal, possessed.ContainerId);

        if (!_container.Insert(user, stasis))
        {
            RemComp<ADTCrystalPossessedComponent>(animal);
            return;
        }

        possessed.Body = user;
        _godmode.EnableGodmode(user);
        _mind.TransferTo(mindId, animal);
        possessed.Action = _actions.AddAction(animal, ent.Comp.ExitAction);
    }

    private void OnExitPossession(Entity<ADTCrystalPossessedComponent> ent, ref ADTExitPossessionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ReleaseBody(ent, false);
        _gibbing.Gib(ent);
    }

    private void OnPossessedStateChanged(Entity<ADTCrystalPossessedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        ReleaseBody(ent, true);
        _gibbing.Gib(ent);
    }

    private void ReleaseBody(Entity<ADTCrystalPossessedComponent> ent, bool kill)
    {
        var body = ent.Comp.Body;

        _actions.RemoveAction(ent.Comp.Action);

        if (TerminatingOrDeleted(body))
            return;

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var stasis))
            _container.Remove(body, stasis, destination: Transform(ent).Coordinates);

        _godmode.DisableGodmode(body);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            _mind.TransferTo(mindId, body);

        if (kill)
            _mobState.ChangeMobState(body, MobState.Dead);

        RemCompDeferred<ADTCrystalPossessedComponent>(ent);
    }

    private static Vector2i DirectionToOffset(TransformComponent xform)
    {
        var vector = xform.LocalRotation.GetDir().ToIntVec();
        return new Vector2i(vector.X, vector.Y);
    }
}
