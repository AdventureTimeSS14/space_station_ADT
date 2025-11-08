using Content.Server.Shuttles.Systems;
using Content.Shared.ADT._Mono.FireControl;
using Content.Shared.GameTicking;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.ADT._Mono.FireControl;

public sealed partial class FireControlSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ShuttleConsoleSystem _shuttleConsoleSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    private bool _completedCheck = false;

    private void InitializeConsole()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);

        SubscribeLocalEvent<FireControlConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FireControlConsoleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<FireControlConsoleComponent, FireControlConsoleRefreshServerMessage>(OnRefreshServer);
        SubscribeLocalEvent<FireControlConsoleComponent, FireControlConsoleFireMessage>(OnFire);
        SubscribeLocalEvent<FireControlConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<FireControlConsoleComponent, ActivatableUIOpenAttemptEvent>(OnConsoleUIOpenAttempt);
    }

    // scuffed one-time check of all station control consoles to ensure they're already refreshed
    // given this only happens once, we can assume all refreshed are things like Camelot's gunnery server.
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_completedCheck)
            return;

        var query = EntityQueryEnumerator<FireControlConsoleComponent>();

        while (query.MoveNext(out var uid, out var console))
        {
            DoRefreshServer(uid, console);
        }

        _completedCheck = true;
    }

    private void OnPowerChanged(EntityUid uid, FireControlConsoleComponent component, PowerChangedEvent args)
    {
        if (args.Powered)
            TryRegisterConsole(uid, component);
        else
            UnregisterConsole(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, FireControlConsoleComponent component, ComponentShutdown args)
    {
        UnregisterConsole(uid, component);
    }

    private void DoRefreshServer(EntityUid uid, FireControlConsoleComponent component)
    {
        // First, clean up any invalid server references across all grids
        CleanupInvalidServerReferences();

        // Get the console's grid to force server reconnection on it
        var consoleGrid = _xform.GetGrid(uid);
        if (consoleGrid != null)
        {
            // Force all servers on this grid to attempt reconnection
            ForceServerReconnectionOnGrid((EntityUid)consoleGrid);
        }

        // Check if the current connected server is still valid
        if (component.ConnectedServer != null)
        {
            if (!Exists(component.ConnectedServer) || !TryComp<FireControlServerComponent>(component.ConnectedServer, out _))
            {
                // Server no longer exists, clear the connection
                component.ConnectedServer = null;
            }
        }

        // Try to register console if not connected or if connection was cleared
        if (component.ConnectedServer == null)
        {
            TryRegisterConsole(uid, component);
        }

        // Refresh controllables if we have a valid server connection
        if (component.ConnectedServer != null &&
            TryComp<FireControlServerComponent>(component.ConnectedServer, out var server) &&
            server.ConnectedGrid != null)
        {
            RefreshControllables((EntityUid)server.ConnectedGrid);
        }

        // Always update UI to reflect current state
        UpdateUi(uid, component);
    }

    private void OnRefreshServer(EntityUid uid, FireControlConsoleComponent component, FireControlConsoleRefreshServerMessage args)
    {
        DoRefreshServer(uid, component);
    }

    private void OnFire(EntityUid uid, FireControlConsoleComponent component, FireControlConsoleFireMessage args)
    {
        if (component.ConnectedServer == null
            || !TryComp<FireControlServerComponent>(component.ConnectedServer, out var server)
            || !server.Consoles.Contains(uid))
            return;

        // Fire the actual weapons
        FireWeapons((EntityUid)component.ConnectedServer, args.Selected, args.Coordinates, server);

        UpdateUi(uid, component);

        // Raise an event to track the cursor position even when not firing
        var fireEvent = new FireControlConsoleFireEvent(args.Coordinates, args.Selected);
        RaiseLocalEvent(uid, fireEvent);
    }

    public void OnUIOpened(EntityUid uid, FireControlConsoleComponent component, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, component);
    }

    private void UnregisterConsole(EntityUid console, FireControlConsoleComponent? component = null)
    {
        if (!Resolve(console, ref component))
            return;

        if (component.ConnectedServer == null)
            return;

        // Check if server still exists before trying to unregister
        if (Exists(component.ConnectedServer) && TryComp<FireControlServerComponent>(component.ConnectedServer, out var server))
        {
            server.Consoles.Remove(console);
        }

        component.ConnectedServer = null;
        UpdateUi(console, component);
    }

    private bool CanRegister((EntityUid? ServerUid, FireControlServerComponent? ServerComponent) gridServer)
    {
        if (gridServer.ServerComponent == null)
            return false;

        if (gridServer.ServerComponent.EnforceMaxConsoles
            && gridServer.ServerComponent.Consoles.Count >= gridServer.ServerComponent.MaxConsoles)
            return false;

        return true;
    }

    private bool TryRegisterConsole(EntityUid console, FireControlConsoleComponent? consoleComponent = null)
    {
        if (!Resolve(console, ref consoleComponent))
            return false;

        // Clear any existing invalid connection first
        if (consoleComponent.ConnectedServer != null)
        {
            if (!Exists(consoleComponent.ConnectedServer) || !TryComp<FireControlServerComponent>(consoleComponent.ConnectedServer, out _))
            {
                consoleComponent.ConnectedServer = null;
            }
        }

        var gridServer = TryGetGridServer(console);

        if (gridServer.ServerUid == null || gridServer.ServerComponent == null)
            return false;

        var canRegister = CanRegister(gridServer);

        if (canRegister && gridServer.ServerComponent.Consoles.Add(console))
        {
            consoleComponent.ConnectedServer = gridServer.ServerUid;
            UpdateUi(console, consoleComponent);
            return true;
        }

        return false;
    }

    private void UpdateUi(EntityUid uid, FireControlConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        NavInterfaceState navState = _shuttleConsoleSystem.GetNavState(uid, _shuttleConsoleSystem.GetAllDocks());

        List<FireControllableEntry> controllables = new();
        if (component.ConnectedServer != null && TryComp<FireControlServerComponent>(component.ConnectedServer, out var server))
        {
            if (!server.Consoles.Contains(uid))
                return;

            foreach (var controllable in server.Controlled)
            {
                var controlled = new FireControllableEntry();
                controlled.NetEntity = EntityManager.GetNetEntity(controllable);
                controlled.Coordinates = GetNetCoordinates(Transform(controllable).Coordinates);
                controlled.Name = MetaData(controllable).EntityName;

                var (ammoCount, hasManualReload) = GetWeaponAmmunitionInfo(controllable);
                controlled.AmmoCount = ammoCount;
                controlled.HasManualReload = hasManualReload;

                controllables.Add(controlled);
            }
        }

        var array = controllables.ToArray();

        var state = new FireControlConsoleBoundInterfaceState(component.ConnectedServer != null, array, navState);
        _ui.SetUiState(uid, FireControlConsoleUiKey.Key, state);
    }

    /// <summary>
    /// Gets ammo information for a weapon to determine if it has manual reload.
    /// </summary>
    private (int? ammoCount, bool hasManualReload) GetWeaponAmmunitionInfo(EntityUid weaponEntity)
    {
        if (TryComp<BasicEntityAmmoProviderComponent>(weaponEntity, out var basicAmmo))
        {
            var hasRecharge = HasComp<RechargeBasicEntityAmmoComponent>(weaponEntity);

            return (basicAmmo.Count, !hasRecharge);
        }

        if (TryComp<BallisticAmmoProviderComponent>(weaponEntity, out var ballisticAmmo))
        {
            return (ballisticAmmo.Count, ballisticAmmo.Cycleable);
        }

        if (TryComp<MagazineAmmoProviderComponent>(weaponEntity, out var magazineAmmo))
        {
            var magazineEntity = GetMagazineEntity(weaponEntity);
            if (magazineEntity != null)
            {
                if (TryComp<BallisticAmmoProviderComponent>(magazineEntity, out var magazineBallisticAmmo))
                {
                    return (magazineBallisticAmmo.Count, magazineBallisticAmmo.Cycleable);
                }

                if (TryComp<BasicEntityAmmoProviderComponent>(magazineEntity, out var magazineBasicAmmo))
                {
                    var hasRecharge = HasComp<RechargeBasicEntityAmmoComponent>(magazineEntity);
                    return (magazineBasicAmmo.Count, !hasRecharge);
                }
            }
        }

        return (null, false);
    }

    /// <summary>
    /// Gets the magazine entity from a weapon's magazine slot.
    /// </summary>
    private EntityUid? GetMagazineEntity(EntityUid weaponEntity)
    {
        if (!_containers.TryGetContainer(weaponEntity, "gun_magazine", out var container) ||
            container is not ContainerSlot slot)
        {
            return null;
        }

        return slot.ContainedEntity;
    }
}
