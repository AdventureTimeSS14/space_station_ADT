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

namespace Content.Server.ADT._Mono.FireControl;

public sealed partial class FireControlSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

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

            var direction = (targetPos.Position - weaponPos);
            var distance = direction.Length();
            if (distance <= 0)
                continue;

            direction = Vector2.Normalize(direction);

            var ray = new CollisionRay(weaponPos, direction, collisionMask: (int)(CollisionGroup.Opaque | CollisionGroup.Impassable));
            var rayCastResults = _physics.IntersectRay(weaponXform.MapID, ray, distance, localWeapon, false).ToList();

            if (rayCastResults.Count == 0)
            {
                _gun.AttemptShoot(localWeapon, localWeapon, gun, targetCoords);
            }
        }
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

public sealed class FireControllableStatusReportEvent : EntityEventArgs
{
    public List<(string type, string content)> StatusReports = new();
}
