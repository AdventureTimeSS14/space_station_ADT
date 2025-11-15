using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.ADT._Mono.FireControl;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using System.Linq;
using Content.Shared.Physics;
using System.Numerics;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Server.ADT._Mono.FireControl;

public sealed partial class FireControlSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    /// <summary>
    /// Dictionary of entities that have visualization enabled
    /// </summary>
    private readonly HashSet<EntityUid> _visualizedEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireControlServerComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FireControlServerComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<FireControllableComponent, PowerChangedEvent>(OnControllablePowerChanged);
        SubscribeLocalEvent<FireControllableComponent, ComponentShutdown>(OnControllableShutdown);
        SubscribeLocalEvent<FireControllableComponent, EntParentChangedMessage>(OnControllableParentChanged);

        // Subscribe to grid split events to ensure we update when grids change
        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        InitializeConsole();
        InitializeTargetGuided();
    }

    private void OnPowerChanged(EntityUid uid, FireControlServerComponent component, PowerChangedEvent args)
    {
        if (args.Powered)
            TryConnect(uid, component);
        else
            Disconnect(uid, component);
    }

    private void OnShutdown(EntityUid uid, FireControlServerComponent component, ComponentShutdown args)
    {
        Disconnect(uid, component);
    }

    private void OnControllablePowerChanged(EntityUid uid, FireControllableComponent component, PowerChangedEvent args)
    {
        if (args.Powered)
            TryRegister(uid, component);
        else
            Unregister(uid, component);
    }

    private void OnControllableShutdown(EntityUid uid, FireControllableComponent component, ComponentShutdown args)
    {
        if (component.ControllingServer != null && TryComp<FireControlServerComponent>(component.ControllingServer, out var server))
        {
            Unregister(uid, component);

            foreach (var console in server.Consoles)
            {
                if (TryComp<FireControlConsoleComponent>(console, out var consoleComp))
                {
                    UpdateUi(console, consoleComp);
                }
            }
        }
    }

    private void OnControllableParentChanged(EntityUid uid, FireControllableComponent component, ref EntParentChangedMessage args)
    {
        if (component.ControllingServer == null)
            return;

        // Check if the weapon is still on the same grid as its controlling server
        if (!TryComp<FireControlServerComponent>(component.ControllingServer, out var server) ||
            server.ConnectedGrid == null)
            return;

        var currentGrid = _xform.GetGrid(uid);
        if (currentGrid != server.ConnectedGrid)
        {
            // Weapon is no longer on the same grid - unregister it
            Unregister(uid, component);

            // Update UI for any connected consoles
            foreach (var console in server.Consoles)
            {
                if (TryComp<FireControlConsoleComponent>(console, out var consoleComp))
                {
                    UpdateUi(console, consoleComp);
                }
            }
        }
    }


    private void Disconnect(EntityUid server, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return;

        if (!Exists(component.ConnectedGrid) || !TryComp<FireControlGridComponent>(component.ConnectedGrid, out var controlGrid))
            return;

        if (controlGrid.ControllingServer == server)
        {
            controlGrid.ControllingServer = null;
            RemComp<FireControlGridComponent>((EntityUid)component.ConnectedGrid);
        }

        foreach (var controllable in(component.Controlled))
            Unregister(controllable);

        foreach (var console in component.Consoles)
            UnregisterConsole(console);
    }

    public void RefreshControllables(EntityUid grid, FireControlGridComponent? component = null)
    {
        if (!Resolve(grid, ref component))
            return;

        if (component.ControllingServer == null || !TryComp<FireControlServerComponent>(component.ControllingServer, out var server))
            return;

        server.Controlled.Clear();

        var query = EntityQueryEnumerator<FireControllableComponent>();

        while (query.MoveNext(out var controllable, out var controlComp))
        {
            if (_xform.GetGrid(controllable) == grid)
                TryRegister(controllable, controlComp);
        }

        foreach (var console in server.Consoles)
            UpdateUi(console);
    }

    private bool TryConnect(EntityUid server, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return false;

        var grid = _xform.GetGrid(server);

        if (grid == null)
            return false;

        var controlGrid = EnsureComp<FireControlGridComponent>((EntityUid)grid);

        if (controlGrid.ControllingServer != null)
            return false;

        controlGrid.ControllingServer = server;
        component.ConnectedGrid = grid;

        RefreshControllables((EntityUid)grid, controlGrid);

        return true;
    }

    private void Unregister(EntityUid controllable, FireControllableComponent? component = null)
    {
        if (!Resolve(controllable, ref component))
            return;

        if (component.ControllingServer == null || !TryComp<FireControlServerComponent>(component.ControllingServer, out var controlComp))
            return;

        controlComp.Controlled.Remove(controllable);
        component.ControllingServer = null;
    }

    private bool TryRegister(EntityUid controllable, FireControllableComponent? component = null)
    {
        if (!Resolve(controllable, ref component))
            return false;

        var gridServer = TryGetGridServer(controllable);

        if (gridServer.ServerComponent == null)
            return false;

        if (gridServer.ServerComponent.Controlled.Add(controllable))
        {
            component.ControllingServer = gridServer.ServerUid;
            return true;
        }
        else
        {
            return false;
        }
    }

    private (EntityUid? ServerUid, FireControlServerComponent? ServerComponent) TryGetGridServer(EntityUid uid)
    {
        var grid = _xform.GetGrid(uid);

        if (grid == null)
            return (null, null);

        if (!TryComp<FireControlGridComponent>(grid, out var controlGrid))
            return (null, null);

        if (controlGrid.ControllingServer == null || !TryComp<FireControlServerComponent>(controlGrid.ControllingServer, out var server))
            return (null, null);

        return (controlGrid.ControllingServer, server);
    }

    public void FireWeapons(EntityUid server, List<NetEntity> weapons, NetCoordinates coordinates, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component))
            return;

        var targetCoords = GetCoordinates(coordinates);

        foreach (var weapon in weapons)
        {
            var localWeapon = GetEntity(weapon);
            if (!component.Controlled.Contains(localWeapon))
                continue;

            if (!TryComp<GunComponent>(localWeapon, out var gun))
                continue;

            // Check if the weapon is still on the same grid as the GCS server
            var weaponGrid = _xform.GetGrid(localWeapon);
            if (weaponGrid != component.ConnectedGrid)
            {
                // Weapon is no longer on the same grid as GCS - unregister it and skip firing
                if (TryComp<FireControllableComponent>(localWeapon, out var controllableComp))
                {
                    Unregister(localWeapon, controllableComp);
                }
                continue;
            }

            var weaponXform = Transform(localWeapon);
            var targetPos = targetCoords.ToMap(EntityManager, _xform);

            if (targetPos.MapId != weaponXform.MapID)
                continue;

            var weaponPos = _xform.GetWorldPosition(weaponXform);

            // Get direction to target
            var direction = (targetPos.Position - weaponPos);
            var distance = direction.Length();
            if (distance <= 0)
                continue;

            direction = Vector2.Normalize(direction);

            // Check for obstacles in the firing direction
            if (!CanFireInDirection(localWeapon, weaponPos, direction, targetPos.Position, weaponXform.MapID))
                continue;

            // If we can fire, fire the weapon
            _gun.AttemptShoot(localWeapon, localWeapon, gun, targetCoords);
        }
    }

    /// <summary>
    /// Checks all controllables on a grid and unregisters any that don't belong.
    /// </summary>
    /// <param name="server">The GCS server entity</param>
    /// <param name="component">The server component</param>
    public void UpdateAllControllables(EntityUid server, FireControlServerComponent? component = null)
    {
        if (!Resolve(server, ref component) || component.ConnectedGrid == null)
            return;

        // Get a copy of the controlled entities list to avoid modification during iteration
        var controlled = component.Controlled.ToList();

        foreach (var controllable in controlled)
        {
            if (TryComp<FireControllableComponent>(controllable, out var controlComp))
            {
                var currentGrid = _xform.GetGrid(controllable);
                if (currentGrid != component.ConnectedGrid)
                {
                    Unregister(controllable, controlComp);
                }
            }
        }

        // Update UI for all consoles
        foreach (var console in component.Consoles)
        {
            if (TryComp<FireControlConsoleComponent>(console, out var consoleComp))
            {
                UpdateUi(console, consoleComp);
            }
        }
    }

    private void OnGridSplit(ref GridSplitEvent ev)
    {
        // Check all GCS servers for affected grids
        var query = EntityQueryEnumerator<FireControlServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            if (server.ConnectedGrid == ev.Grid)
            {
                // Grid has been split, check all controllables
                UpdateAllControllables(serverUid, server);
            }
        }
    }

    /// <summary>
    /// Attempts to fire a weapon, handling aiming and firing logic.
    /// </summary>
    public bool AttemptFire(EntityUid weapon, EntityUid user, EntityCoordinates coords, FireControllableComponent? comp = null)
    {
        if (!Resolve(weapon, ref comp))
            return false;

        // Check if the weapon is ready to fire
        if (!CanFire(weapon, comp))
            return false;

        // Get weapon and target positions
        var weaponXform = Transform(weapon);
        var weaponPos = _xform.GetWorldPosition(weaponXform);
        var targetPos = coords.ToMap(EntityManager, _xform).Position;

        // Calculate direction
        var direction = targetPos - weaponPos;
        var distance = direction.Length();
        if (distance <= float.Epsilon)
            return false; // Can't fire at the same position

        direction = Vector2.Normalize(direction);

        // Check for obstacles in the firing direction
        if (!CanFireInDirection(weapon, weaponPos, direction, targetPos, weaponXform.MapID))
            return false;

        // Set the cooldown for next firing
        comp.NextFire = _timing.CurTime + TimeSpan.FromSeconds(comp.FireCooldown);

        // Try to get a gun component and fire the weapon
        if (TryComp<GunComponent>(weapon, out var gun))
        {
            _gun.AttemptShoot(weapon, user, gun, coords);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a weapon is ready to fire.
    /// </summary>
    private bool CanFire(EntityUid weapon, FireControllableComponent comp)
    {
        // Check if weapon is powered
        if (!_power.IsPowered(weapon))
            return false;

        // Check if weapon is connected to a server
        if (comp.ControllingServer == null)
            return false;

        // Check for other conditions like cooldowns if needed
        if (comp.NextFire > _timing.CurTime)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a weapon can fire in a specific direction without obstacles
    /// </summary>
    /// <param name="weapon">The weapon entity</param>
    /// <param name="weaponPos">The weapon's position</param>
    /// <param name="direction">Normalized direction vector</param>
    /// <param name="targetPos">The target position</param>
    /// <param name="mapId">The map ID</param>
    /// <param name="maxDistance">Maximum raycast distance in meters</param>
    /// <returns>True if the weapon can fire in that direction</returns>
    private bool CanFireInDirection(EntityUid weapon, Vector2 weaponPos, Vector2 direction, Vector2 targetPos, MapId mapId, float maxDistance = 10f)
    {
        // Get the weapon's grid for grid filtering
        var weaponTransform = Transform(weapon);
        var weaponGridUid = weaponTransform.GridUid;

        // Calculate distance to target (capped at maximum distance)
        var targetDistance = Vector2.Distance(weaponPos, targetPos);
        var distance = Math.Min(targetDistance, maxDistance);

        // Initialize ray collision
        var ray = new CollisionRay(weaponPos, direction, collisionMask: (int)(CollisionGroup.Opaque | CollisionGroup.Impassable));

        // Create a predicate that ignores entities not on the same grid
        bool IgnoreEntityNotOnSameGrid(EntityUid entity, EntityUid sourceWeapon)
        {
            // Always ignore the source weapon itself
            if (entity == sourceWeapon)
                return true;

            // If the weapon isn't on a grid, we'll check against all entities
            if (weaponGridUid == null)
                return false;

            // Get the entity's grid
            var entityTransform = Transform(entity);
            var entityGridUid = entityTransform.GridUid;

            // Ignore if not on the same grid
            return entityGridUid != weaponGridUid;
        }

        // Check if there's any obstacles in the firing direction, only considering entities on the same grid
        var raycastResults = _physics.IntersectRayWithPredicate(
            mapId,
            ray,
            weapon,
            IgnoreEntityNotOnSameGrid,
            distance,
            returnOnFirstHit: false
        ).ToList();

        // Can only fire if there's no obstacles in the path
        return raycastResults.Count == 0;
    }

    /// <summary>
    /// Checks if a weapon can fire in a full 360-degree circle around it to find clear firing lanes
    /// </summary>
    /// <param name="weapon">The weapon entity</param>
    /// <param name="maxDistance">Maximum raycast distance in meters</param>
    /// <param name="rayCount">Number of rays to cast around the entity</param>
    /// <returns>Dictionary mapping directions (angles in degrees) to whether they're clear for firing</returns>
    public Dictionary<float, bool> CheckAllDirections(EntityUid weapon, float maxDistance = 50f, int rayCount = 128)
    {
        var directions = new Dictionary<float, bool>();

        var transform = Transform(weapon);
        var position = _xform.GetWorldPosition(transform);
        var mapId = transform.MapID;
        var weaponGridUid = transform.GridUid;

        // Create a predicate that ignores entities not on the same grid
        bool IgnoreEntityNotOnSameGrid(EntityUid entity, EntityUid sourceWeapon)
        {
            // Always ignore the source weapon itself
            if (entity == sourceWeapon)
                return true;

            // If the weapon isn't on a grid, we'll check against all entities
            if (weaponGridUid == null)
                return false;

            // Get the entity's grid
            var entityTransform = Transform(entity);
            var entityGridUid = entityTransform.GridUid;

            // Ignore if not on the same grid
            return entityGridUid != weaponGridUid;
        }

        // Cast rays in all directions to check for clear firing lanes
        for (var i = 0; i < rayCount; i++)
        {
            // Calculate angle and direction for this ray
            var angle = (i / (float)rayCount) * MathF.Tau;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            // Initialize ray collision
            var ray = new CollisionRay(position, direction, collisionMask: (int)(CollisionGroup.Opaque | CollisionGroup.Impassable));

            // Check if there's any obstacles in this direction, only considering entities on the same grid
            var raycastResults = _physics.IntersectRayWithPredicate(
                mapId,
                ray,
                weapon,
                IgnoreEntityNotOnSameGrid,
                maxDistance,
                returnOnFirstHit: false
            ).ToList();

            // Direction is clear if there are no obstacles
            var canFire = raycastResults.Count == 0;
            directions[angle * 180 / MathF.PI] = canFire;
        }

        return directions;
    }

    /// <summary>
    /// Sends a visualization event to all clients
    /// </summary>
    /// <param name="entityUid">Entity to visualize</param>
    /// <param name="directions">Firing direction data</param>
    public void SendVisualizationEvent(EntityUid entityUid, Dictionary<float, bool> directions)
    {
        var netEntity = GetNetEntity(entityUid);

        var ev = new FireControlVisualizationEvent(
            netEntity,
            directions
        );

        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Toggles visualization for an entity
    /// </summary>
    /// <param name="entityUid">Entity to toggle visualization for</param>
    /// <returns>True if visualization was enabled, false if disabled</returns>
    public bool ToggleVisualization(EntityUid entityUid)
    {
        var netEntity = GetNetEntity(entityUid);

        // Check if already visualized
        if (_visualizedEntities.Contains(entityUid))
        {
            // Turn off visualization
            _visualizedEntities.Remove(entityUid);
            RaiseNetworkEvent(new FireControlVisualizationEvent(netEntity));
            return false;
        }

        // Turn on visualization
        _visualizedEntities.Add(entityUid);
        var directions = CheckAllDirections(entityUid);
        RaiseNetworkEvent(new FireControlVisualizationEvent(netEntity, directions));
        return true;
    }
}

public sealed class FireControllableStatusReportEvent : EntityEventArgs
{
    public List<(string type, string content)> StatusReports = new();
}
