// Copyright Rane (elijahrane@gmail.com) 2025
// All rights reserved. Relicensed under AGPL with permission

using Content.Server.Administration.Logs;
using Content.Server.Shuttles.Systems;
using Content.Shared._Mono.FireControl;
using Content.Shared._Crescent.ShipShields;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Power;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Server._Mono.FireControl;

public sealed partial class FireControlSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private ShuttleConsoleSystem _shuttleConsoleSystem = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedMapSystem _map = default!; // ADT-Tweak: IMapManager > SharedMapSystem

    private bool _completedCheck = false;
    private readonly HashSet<EntityUid> _pendingUiConsoles = new(); // LuaM

    private void InitializeConsole()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);

        SubscribeLocalEvent<FireControlConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FireControlConsoleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<FireControlConsoleComponent, FireControlConsoleRefreshServerMessage>(OnRefreshServer);
        SubscribeLocalEvent<FireControlConsoleComponent, FireControlConsoleFireMessage>(OnFire);
        SubscribeLocalEvent<FireControlConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
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

        var xform = Transform(uid);
        var grid = xform.GridUid;
        if (grid == null)
            return;

        // Fire the actual weapons
        FireWeapons((EntityUid)component.ConnectedServer, args.Selected, args.Coordinates, server);
        if ((component.NextLog == null || component.NextLog < _timing.CurTime) && args.Selected.Any())
        {
            var firePos = _transform.ToMapCoordinates(GetCoordinates(args.Coordinates)).Position;
            var ourPos = _transform.GetWorldPosition(grid.Value);
            var grids = new List<Entity<MapGridComponent>>();
            var adjust = new Vector2(component.LogGridLookupRange, component.LogGridLookupRange);
            _map.FindGridsIntersecting(xform.MapID, new Box2(firePos - adjust, firePos + adjust), ref grids, approx: true, includeMap: false);
            grids.RemoveAll(g => g == grid);
            EntityUid? closest = null;
            foreach (var gridUid in grids)
            {
                var newPos = _transform.GetWorldPosition(gridUid);
                if (closest == null || (newPos - firePos).LengthSquared() < (_transform.GetWorldPosition(closest.Value) - firePos).LengthSquared())
                    closest = gridUid;
            }

            _adminLogger.Add(LogType.ShipgunFired, LogImpact.High,
                    $"{ToPrettyString(args.Actor):user} fired weaponry of ship {ToPrettyString(grid):entity} from ({ourPos}) to ({firePos}), closest grid: {ToPrettyString(closest)}");

            component.NextLog = _timing.CurTime + component.LogSpacing;
        }
//        UpdateUi(uid, component); // Commented by LuaM
// LuaM-start:
        if (args.Selected.Count > 0)
            QueueUiUpdate(uid, component);
// LuaM-end.

        // Raise an event to track the cursor position even when not firing
        var fireEvent = new FireControlConsoleFireEvent(args.Coordinates, args.Selected);
        RaiseLocalEvent(uid, fireEvent);
    }

// LuaM-start:
    public void QueueUiUpdate(EntityUid uid, FireControlConsoleComponent component) // LuaM: public for the reload display
    {
        if (component.NextUiUpdate == null || component.NextUiUpdate <= _timing.CurTime)
        {
            component.NextUiUpdate = _timing.CurTime + component.UiUpdateSpacing;
            UpdateUi(uid, component);
            return;
        }

        _pendingUiConsoles.Add(uid);
    }

    private void UpdatePendingConsoleUi() // LuaM
    {
        if (_pendingUiConsoles.Count == 0)
            return;

        var now = _timing.CurTime;
        _pendingUiConsoles.RemoveWhere(uid =>
        {
            if (!TryComp<FireControlConsoleComponent>(uid, out var component))
                return true;

            if (component.NextUiUpdate > now)
                return false;

            component.NextUiUpdate = now + component.UiUpdateSpacing;
            UpdateUi(uid, component);
            return true;
        });
    }
// LuaM-end.

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

    // Forge-Change-Start
    /// <summary>
    /// Pushes refreshed UI state to every fire control console anchored to the given grid.
    /// </summary>
    public void RefreshConsolesOnGrid(EntityUid gridUid)
    {
        var query = EntityQueryEnumerator<FireControlConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var consoleUid, out var consoleComp, out var xform))
        {
            if (xform.GridUid != gridUid)
                continue;
            UpdateUi(consoleUid, consoleComp);
        }
    }
    // Forge-Change-End

    private void UpdateUi(EntityUid uid, FireControlConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

// LuaM-start:
        if (!_ui.IsUiOpen(uid, FireControlConsoleUiKey.Key))
            return;
// LuaM-end.

        NavInterfaceState navState = _shuttleConsoleSystem.GetNavState(uid, _shuttleConsoleSystem.GetAllDocks());

        var array = Array.Empty<FireControllableEntry>(); // Lua: entries are built in BuildControllableEntries
        if (component.ConnectedServer != null && TryComp<FireControlServerComponent>(component.ConnectedServer, out var server))
        {
            if (!server.Consoles.Contains(uid))
                return;

            array = BuildControllableEntries(server); // Lua
        }

        var shieldState = GetShieldState(Transform(uid).GridUid); // ADT-Tweak
        var state = new FireControlConsoleBoundInterfaceState(component.ConnectedServer != null, array, navState, shieldState);
        _ui.SetUiState(uid, FireControlConsoleUiKey.Key, state);
    }

    /// <summary>
    /// Builds the weapon list a console shows for this fire control server.
    /// </summary>
    public FireControllableEntry[] BuildControllableEntries(FireControlServerComponent server)
    {
        var controllables = new List<FireControllableEntry>();
        foreach (var controllable in server.Controlled)
        {
            var controlled = new FireControllableEntry();
            controlled.NetEntity = GetNetEntity(controllable);
            controlled.Coordinates = GetNetCoordinates(Transform(controllable).Coordinates);
            controlled.Name = MetaData(controllable).EntityName;
            TryComp<FireControllableComponent>(controllable, out var fcComp);
            controlled.IgnoresLos = fcComp != null ? fcComp.IgnoreLos : false;

            var (ammoCount, hasManualReload) = GetWeaponAmmunitionInfo(controllable);
            controlled.AmmoCount = ammoCount;
            controlled.HasManualReload = hasManualReload;

            // LuaM: reload progress, from the last shot until the weapon may fire again
            var reloadEnd = fcComp?.NextFire ?? TimeSpan.Zero;
            if (_gunQuery.TryComp(controllable, out var gun) && gun.NextFire > reloadEnd)
                reloadEnd = gun.NextFire;

            if (fcComp != null && fcComp.LastShot > TimeSpan.Zero && reloadEnd > _timing.CurTime && fcComp.LastShot < reloadEnd)
            {
                controlled.ReloadStart = fcComp.LastShot;
                controlled.ReloadEnd = reloadEnd;
            }
            // End LuaM

            controllables.Add(controlled);
        }

        return controllables.ToArray();
    }

    // ADT-Tweak-Start: Forge kept this in ShuttleConsoleSystem, here only the gunnery console shows the shield
    /// <summary>
    /// Finds the first ship shield emitter on the given grid and reports its current state.
    /// </summary>
    private ShipShieldState GetShieldState(EntityUid? gridUid)
    {
        if (gridUid == null)
            return default;

        var query = AllEntityQuery<ShipShieldEmitterComponent, TransformComponent>();
        while (query.MoveNext(out _, out var emitter, out var emitterXform))
        {
            if (emitterXform.GridUid != gridUid)
                continue;

            var limit = emitter.DamageLimit > 0 ? emitter.DamageLimit : 1f;
            var percent = Math.Clamp(1f - emitter.Damage / limit, 0f, 1f);
            var online = emitter.Shield != null;

            TimeSpan? endTime = null;
            if (!online)
            {
                var healRate = emitter.HealPerSecond * emitter.UnpoweredBonus;
                var rechargeSeconds = healRate > 0f ? emitter.Damage / healRate : 0f;
                var seconds = MathF.Max(rechargeSeconds, emitter.OverloadAccumulator);
                endTime = _timing.CurTime + TimeSpan.FromSeconds(seconds);
            }

            return new ShipShieldState(true, online, percent, endTime);
        }

        return default;
    }
    // ADT-Tweak-End

    /// <summary>
    /// Gets ammo information for a weapon to determine if it has manual reload.
    /// </summary>
    private (int? ammoCount, bool hasManualReload) GetWeaponAmmunitionInfo(EntityUid weaponEntity)
    {
        // LuaM: battery weapons first. Energy artillery also inherits an empty grenade slot (BallisticAmmoProvider)
        // from the base launcher, which made them always show 0 ammo.
        if (TryComp<BatteryAmmoProviderComponent>(weaponEntity, out var battery)) // ADT-Tweak: projectile and hitscan battery providers are one component here
            return (battery.Shots, true);
        // End LuaM

        if (TryComp<BasicEntityAmmoProviderComponent>(weaponEntity, out var basicAmmo))
        {
            var hasRecharge = HasComp<RechargeBasicEntityAmmoComponent>(weaponEntity);

            return (basicAmmo.Count, !hasRecharge);
        }

        if (TryComp<BallisticAmmoProviderComponent>(weaponEntity, out var ballisticAmmo))
        {
            // if we're InfiniteUnspawned consider us to be non-reloading when at 0 ammo
            return (ballisticAmmo.Count, ballisticAmmo.Cycleable && (ballisticAmmo.Count != 0 || !ballisticAmmo.InfiniteUnspawned));
        }

        if (TryComp<MagazineAmmoProviderComponent>(weaponEntity, out var magazineAmmo))
        {
            var magazineEntity = GetMagazineEntity(weaponEntity);
            if (magazineEntity != null)
            {
                if (TryComp<BallisticAmmoProviderComponent>(magazineEntity, out var magazineBallisticAmmo))
                {
                    var hasAmmo = magazineBallisticAmmo.Cycleable
                             && (magazineBallisticAmmo.Count != 0 || !magazineBallisticAmmo.InfiniteUnspawned);
                    return (magazineBallisticAmmo.Count, hasAmmo);
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
