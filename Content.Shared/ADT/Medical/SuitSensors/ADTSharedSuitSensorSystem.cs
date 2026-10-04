using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
// using Content.Shared.DeviceNetwork;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
// using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.ADT.Medical.SuitSensors;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
// using Content.Shared.Station;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Medical.SuitSensors;

public abstract class ADTSharedSuitSensorSystem : EntitySystem
{
    // [Dependency] private readonly SharedStationSystem _stationSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    // ADT-Tweak Start - New Monitor: wearer -> OnMob sensor index
    /// <summary>
    /// Wearer → OnMob suit-sensor entity. Avoids an O(S) EntityQuery in GetSensorState.
    /// </summary>
    private readonly Dictionary<EntityUid, EntityUid> _onMobSensorsByWearer = new();
    // ADT-Tweak End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSuitSensorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTSuitSensorComponent, ComponentStartup>(OnStartup); //ADT-Tweak: NewMonitor
        SubscribeLocalEvent<ADTSuitSensorComponent, ComponentShutdown>(OnShutdown);
        // ADT-Tweak Start - New Monitor: PlayerSpawnCompleteEvent station assignment unused
        // SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
        // ADT-Tweak End
        SubscribeLocalEvent<ADTSuitSensorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ADTSuitSensorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ADTSuitSensorComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ADTSuitSensorComponent, EmpDisabledRemovedEvent>(OnEmpFinished);
        SubscribeLocalEvent<ADTSuitSensorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ADTSuitSensorComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<ADTSuitSensorComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<ADTSuitSensorComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<ADTSuitSensorComponent, ADTSuitSensorChangeDoAfterEvent>(OnADTSuitSensorDoAfter);

    }

    private void OnMapInit(Entity<ADTSuitSensorComponent> ent, ref MapInitEvent args)
    {
        // Fallback
        // ADT-Tweak Start - New Monitor: OnMob self-user + index at map init
        if (ent.Comp.OnMob)
        {
            ent.Comp.User = ent.Owner;
            IndexOnMobSensor(ent);
        }
        // ADT-Tweak End

        // generate random mode
        if (ent.Comp.RandomMode)
        {
            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                ADTSuitSensorMode.SensorOff,
                ADTSuitSensorMode.SensorBinary, ADTSuitSensorMode.SensorBinary,
                ADTSuitSensorMode.SensorVitals, ADTSuitSensorMode.SensorVitals, ADTSuitSensorMode.SensorVitals,
                ADTSuitSensorMode.SensorCords, ADTSuitSensorMode.SensorCords
            };
            ent.Comp.Mode = _random.Pick(modesDist);
        }
        // ADT-Tweak Start - NewMonitor:
        // Spread initial reports over the first interval so a round start does
        // not update every uniform on the same tick.
        ent.Comp.NextUpdate =
            _timing.CurTime +
            TimeSpan.FromSeconds(_random.NextFloat() * (float) ent.Comp.UpdateRate.TotalSeconds);
        // ADT-Tweak End
        Dirty(ent);
    }

    // ADT-Tweak Start - New Monitor: OnMob startup/shutdown indexing
    private void OnStartup(Entity<ADTSuitSensorComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.OnMob)
            return;

        var dirty = false;
        if (ent.Comp.User == null)
        {
            ent.Comp.User = ent.Owner;
            dirty = true;
        }

        IndexOnMobSensor(ent);

        if (dirty)
            Dirty(ent);
    }
    // ADT-Tweak Start - New Monitor: Deleted
    // private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    // {
    //     // If the player spawns in arrivals then the grid underneath them may not be appropriate.
    //     // in which case we'll just use the station spawn code told us they are attached to and set all of their
    //     // sensors.
    //     RecursiveSensor(ev.Mob, ev.Station);
    // }

    // private void RecursiveSensor(EntityUid uid, EntityUid stationUid)
    // {
    //     var xform = Transform(uid);
    //     var enumerator = xform.ChildEnumerator;

    //     while (enumerator.MoveNext(out var child))
    //     {
    //         if (_sensorQuery.TryComp(child, out var sensor))
    //         {
    //             sensor.StationId = stationUid;
    //             Dirty(child, sensor);
    //         }

    //         RecursiveSensor(child, stationUid);
    //     }
    // }

    // ADT-Tweak End

    // ADT-Tweak Start - New Monitor: New indexing
    protected virtual void OnShutdown(Entity<ADTSuitSensorComponent> ent, ref ComponentShutdown args)
    {
        UnindexOnMobSensor(ent);
    }

    private void IndexOnMobSensor(Entity<ADTSuitSensorComponent> ent)
    {
        if (!ent.Comp.OnMob || ent.Comp.User == null)
            return;

        _onMobSensorsByWearer[ent.Comp.User.Value] = ent.Owner;
    }

    private void UnindexOnMobSensor(Entity<ADTSuitSensorComponent> ent)
    {
        if (!ent.Comp.OnMob || ent.Comp.User == null)
            return;

        if (_onMobSensorsByWearer.TryGetValue(ent.Comp.User.Value, out var indexed) && indexed == ent.Owner)
            _onMobSensorsByWearer.Remove(ent.Comp.User.Value);
    }
    // ADT-Tweak End

    private void OnEquipped(Entity<ADTSuitSensorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        ent.Comp.User = args.Wearer;
        Dirty(ent);
    }

    private void OnUnequipped(Entity<ADTSuitSensorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    private void OnEmpPulse(Entity<ADTSuitSensorComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        ent.Comp.PreviousMode = ent.Comp.Mode;
        SetSensor(ent.AsNullable(), ADTSuitSensorMode.SensorOff, null);

        ent.Comp.PreviousControlsLocked = ent.Comp.ControlsLocked;
        ent.Comp.ControlsLocked = true;
        // SetSensor already calls Dirty
    }

    private void OnEmpFinished(Entity<ADTSuitSensorComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        SetSensor(ent.AsNullable(), ent.Comp.PreviousMode, null);
        ent.Comp.ControlsLocked = ent.Comp.PreviousControlsLocked;
    }

    private void OnExamine(Entity<ADTSuitSensorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string msg;
        switch (ent.Comp.Mode)
        {
            case ADTSuitSensorMode.SensorOff:
                msg = "suit-sensor-examine-off";
                break;
            case ADTSuitSensorMode.SensorBinary:
                msg = "suit-sensor-examine-binary";
                break;
            case ADTSuitSensorMode.SensorVitals:
                msg = "suit-sensor-examine-vitals";
                break;
            case ADTSuitSensorMode.SensorCords:
                msg = "suit-sensor-examine-cords";
                break;
            default:
                return;
        }

        args.PushMarkup(Loc.GetString(msg));
    }

    private void OnVerb(Entity<ADTSuitSensorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        // check if user can change sensor
        if (ent.Comp.ControlsLocked)
            return;

        // standard interaction checks
        if (!args.CanInteract || args.Hands == null)
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        //ADT-Tweak-Start
        if (!ent.Comp.OnMob)
        {
            // check if target is incapacitated (cuffed, dead, etc)
            if (ent.Comp.User != null && args.User != ent.Comp.User && _actionBlocker.CanInteract(ent.Comp.User.Value, null))
                return;
        }
        //ADT-Tweak-End

        args.Verbs.UnionWith(new[]
        {
            CreateVerb(ent, args.User, ADTSuitSensorMode.SensorOff),
            CreateVerb(ent, args.User, ADTSuitSensorMode.SensorBinary),
            CreateVerb(ent, args.User, ADTSuitSensorMode.SensorVitals),
            CreateVerb(ent, args.User, ADTSuitSensorMode.SensorCords)
        });
    }

    private void OnInsert(Entity<ADTSuitSensorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        //ADT-Tweak-Start
        if (ent.Comp.OnMob)
            return;
        //ADT-Tweak-End

        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = args.Container.Owner;
        Dirty(ent);
    }

    private void OnRemove(Entity<ADTSuitSensorComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        //ADT-Tweak-Start
        if (ent.Comp.OnMob)
            return;
        //ADT-Tweak-End

        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    private Verb CreateVerb(Entity<ADTSuitSensorComponent> ent, EntityUid userUid, ADTSuitSensorMode mode)
    {
        return new Verb()
        {
            Text = GetModeName(mode),
            Disabled = ent.Comp.Mode == mode,
            Priority = -(int)mode, // sort them in descending order
            Category = VerbCategory.SetSensor,
            // Must close: otherwise the sensor submenu stays open after a click.
            CloseMenu = true, //ADT-Tweak: NewMonitor
            Act = () => TrySetSensor(ent.AsNullable(), mode, userUid)
        };
    }

    public string GetModeName(ADTSuitSensorMode mode)
    {
        string name;
        switch (mode)
        {
            case ADTSuitSensorMode.SensorOff:
                name = "suit-sensor-mode-off";
                break;
            case ADTSuitSensorMode.SensorBinary:
                name = "suit-sensor-mode-binary";
                break;
            case ADTSuitSensorMode.SensorVitals:
                name = "suit-sensor-mode-vitals";
                break;
            case ADTSuitSensorMode.SensorCords:
                name = "suit-sensor-mode-cords";
                break;
            default:
                return "";
        }

        return Loc.GetString(name);
    }

    /// <summary>
    /// Attempts to set <see cref="ADTSuitSensorComponent"/> mode of the entity to the selected in params.
    /// Works instantly if the user is the player wearing the sensors and will start a DoAfter otherwise.
    /// </summary>
    /// <param name="sensors">Entity and its component that should be changed.</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">userUid, when not equal to the <see cref="ADTSuitSensorComponent.User"/>, creates doafter</param>
    public bool TrySetSensor(Entity<ADTSuitSensorComponent?> sensors, ADTSuitSensorMode mode, EntityUid userUid)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return false;

        if (sensors.Comp.User == null || userUid == sensors.Comp.User)
            SetSensor(sensors, mode, userUid);
        else
        {
            var doAfterEvent = new ADTSuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, sensors.Comp.SensorsTime, doAfterEvent, sensors)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }
        return true;
    }

    private void OnADTSuitSensorDoAfter(Entity<ADTSuitSensorComponent> sensors, ref ADTSuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetSensor(sensors.AsNullable(), args.Mode, args.User);
    }

    /// <summary>
    /// Sets mode of the <see cref="ADTSuitSensorComponent"/> of the chosen entity.
    /// Makes popup when <param name="userUid"> not null
    /// </summary>
    /// <param name="sensors">Entity and it's component that should be changed</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">uid, required for the popup</param>
    public void SetSensor(Entity<ADTSuitSensorComponent?> sensors, ADTSuitSensorMode mode, EntityUid? userUid = null)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return;

        sensors.Comp.Mode = mode;
        Dirty(sensors);

        if (userUid != null)
        {
            var msg = Loc.GetString("suit-sensor-mode-state", ("mode", GetModeName(mode)));
            _popupSystem.PopupClient(msg, sensors, userUid.Value);
        }
    }

    /// <summary>
    /// Set all suit sensors on the equipment someone is wearing to the specified mode.
    /// </summary>
    public void SetAllSensors(EntityUid target, ADTSuitSensorMode mode, SlotFlags slots = SlotFlags.All)
    {
        // iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(target, slots);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<ADTSuitSensorComponent>(item, out var sensorComp))
                SetSensor((item, sensorComp), mode);
        }
    }

    /// <summary>
    /// Attempts to get full <see cref="ADTSuitSensorStatus"/> from the <see cref="ADTSuitSensorComponent"/>
    /// </summary>
    /// <param name="uid">Entity to get status</param>
    /// <returns>Full <see cref="ADTSuitSensorStatus"/> of the chosen uid</returns>
    public ADTSuitSensorStatus? GetSensorState(Entity<ADTSuitSensorComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        var sensor = ent.Comp1;
        var transform = ent.Comp2;

        // ADT-Tweak Start - New Monitor: prefer active OnMob sensor over uniform
        // Prefer an *active* OnMob sensor over the uniform. An Off OnMob sensor
        // must not silence the jumpsuit, or that wearer vanishes from monitors.
        if (!sensor.OnMob &&
            sensor.User != null &&
            _onMobSensorsByWearer.TryGetValue(sensor.User.Value, out var onMobUid) &&
            TryComp(onMobUid, out ADTSuitSensorComponent? onMob) &&
            onMob.Mode != ADTSuitSensorMode.SensorOff)
        {
            return null;
        }

        // The wearer is the source of truth for position. Clothing can be inside
        // containers and neither the clothing nor the wearer has to be on a grid.
        if (sensor.User == null ||
            !HasComp<MobStateComponent>(sensor.User) ||
            !TryComp<TransformComponent>(sensor.User.Value, out var userTransform))
        {
            return null;
        }
        // ADT-Tweak End

        // try to get mobs id from ID slot
        var userName = Loc.GetString("suit-sensor-component-unknown-name");
        var userJob = Loc.GetString("suit-sensor-component-unknown-job");
        var userJobIcon = "JobIconNoId";
        List<string>? userJobDepartments = null;    // ADT-Tweak - New Monitor

        if (_idCardSystem.TryFindIdCard(sensor.User.Value, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.LocalizedJobTitle != null)
                userJob = card.Comp.LocalizedJobTitle;
            userJobIcon = card.Comp.JobIcon;
            // ADT-Tweak Start - New Monitor
            if (card.Comp.JobDepartments.Count > 0)
            {
                userJobDepartments = new List<string>(card.Comp.JobDepartments.Count);
                foreach (var department in card.Comp.JobDepartments)
                {
                    if (_proto.TryIndex(department, out var departmentProto))
                        userJobDepartments.Add(Loc.GetString(departmentProto.Name));
                }

                if (userJobDepartments.Count == 0)
                    userJobDepartments = null;
            }
            // ADT-Tweak End
        }

        userJobDepartments ??= ADTSuitSensorStatus.NoDepartments;  // ADT-Tweak - New Monitor

        // get health mob state
        var isAlive = false;
        var isCritical = false; // ADT-Tweak - New Monitor: IsCritical = MobState.Critical only — high damage while conscious is NOT crit.
        if (TryComp(sensor.User.Value, out MobStateComponent? mobState))
        {
            isAlive = !_mobStateSystem.IsDead(sensor.User.Value, mobState);
            isCritical = _mobStateSystem.IsCritical(sensor.User.Value, mobState); //ADT-Tweak: NewMonitor
        }

        // finally, form suit sensor status
        var status = new ADTSuitSensorStatus(GetNetEntity(sensor.User.Value), GetNetEntity(ent.Owner), userName, userJob, userJobIcon, userJobDepartments)
        {
            // ADT-Tweak Start - NewMonitor
            IsAlive = isAlive,
            IsCritical = isCritical,
            // ADT-Tweak End
        };
        switch (sensor.Mode)
        {
            case ADTSuitSensorMode.SensorBinary:
                status.IsAlive = isAlive;
                break;
            case ADTSuitSensorMode.SensorVitals:
            case ADTSuitSensorMode.SensorCords:
            {
                status.IsAlive = isAlive;
                // ADT-Tweak Start - NewMonitor:
                // Damage / threshold only for vitals+ modes — skip for binary.
                if (TryComp<DamageableComponent>(sensor.User.Value, out var damageable))
                    status.TotalDamage = _damageableSystem.GetTotalDamage((sensor.User.Value, damageable)).Int();

                if (_mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.SoftCritical, out var critThreshold)
                    || _mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.Critical, out critThreshold))
                    status.TotalDamageThreshold = critThreshold.Value.Int();

                if (sensor.Mode != ADTSuitSensorMode.SensorCords)
                    break;
                // ADT-Tweak End

                EntityCoordinates coordinates;
                var xformQuery = GetEntityQuery<TransformComponent>();

                if (userTransform.GridUid != null)
                {
                    coordinates = new EntityCoordinates(userTransform.GridUid.Value,
                        Vector2.Transform(_transform.GetWorldPosition(userTransform, xformQuery),
                            _transform.GetInvWorldMatrix(xformQuery.GetComponent(userTransform.GridUid.Value), xformQuery)));
                }
                else if (userTransform.MapUid != null)
                {
                    coordinates = new EntityCoordinates(userTransform.MapUid.Value,
                        _transform.GetWorldPosition(userTransform, xformQuery));
                }
                else
                {
                    coordinates = EntityCoordinates.Invalid;
                }

                status.Coordinates = GetNetCoordinates(coordinates);
                break;
            }
        }
        status.Mode = sensor.Mode;   //ADT-Tweak - NewMonitor: Preserve current sensor mode so the monitor UI can filter and mask data correctly.

        return status;
    }

    // ADT-Tweak Start - NewMonitor: Unused Networking
    // /// <summary>
    // /// Create a device network package from the suit sensors status.
    // /// </summary>
    // public NetworkPayload ADTSuitSensorToPacket(ADTSuitSensorStatus status)
    // {
    //     var payload = new NetworkPayload()
    //     {
    //         [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
    //         [ADTSuitSensorConstants.NET_NAME] = status.Name,
    //         [ADTSuitSensorConstants.NET_JOB] = status.Job,
    //         [ADTSuitSensorConstants.NET_JOB_ICON] = status.JobIcon,
    //         [ADTSuitSensorConstants.NET_JOB_DEPARTMENTS] = status.JobDepartments,
    //         [ADTSuitSensorConstants.NET_IS_ALIVE] = status.IsAlive,
    //         [ADTSuitSensorConstants.NET_SUIT_SENSOR_UID] = status.ADTSuitSensorUid,
    //         [ADTSuitSensorConstants.NET_OWNER_UID] = status.OwnerUid,
    //     };
    //
    //     if (status.TotalDamage != null)
    //         payload.Add(ADTSuitSensorConstants.NET_TOTAL_DAMAGE, status.TotalDamage);
    //     if (status.TotalDamageThreshold != null)
    //         payload.Add(ADTSuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, status.TotalDamageThreshold);
    //     if (status.Coordinates != null)
    //         payload.Add(ADTSuitSensorConstants.NET_COORDINATES, status.Coordinates);
    //
    //     return payload;
    // }
    //
    // /// <summary>
    // /// Try to create the suit sensors status from the device network message.
    // /// </summary>
    // public ADTSuitSensorStatus? PacketToADTSuitSensor(NetworkPayload payload)
    // {
    //     // check command
    //     if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
    //         return null;
    //     if (command != DeviceNetworkConstants.CmdUpdatedState)
    //         return null;
    //
    //     // check name, job and alive
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_NAME, out string? name)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_JOB, out string? job)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_JOB_ICON, out string? jobIcon)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_JOB_DEPARTMENTS, out List<string>? jobDepartments)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_IS_ALIVE, out bool? isAlive)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_SUIT_SENSOR_UID, out NetEntity suitSensorUid)) return null;
    //     if (!payload.TryGetValue(ADTSuitSensorConstants.NET_OWNER_UID, out NetEntity ownerUid)) return null;
    //
    //     // try get total damage and cords (optionals)
    //     payload.TryGetValue(ADTSuitSensorConstants.NET_TOTAL_DAMAGE, out int? totalDamage);
    //     payload.TryGetValue(ADTSuitSensorConstants.NET_TOTAL_DAMAGE_THRESHOLD, out int? totalDamageThreshold);
    //     payload.TryGetValue(ADTSuitSensorConstants.NET_COORDINATES, out NetCoordinates? coords);
    //
    //     var status = new ADTSuitSensorStatus(ownerUid, suitSensorUid, name, job, jobIcon, jobDepartments)
    //     {
    //         IsAlive = isAlive.Value,
    //         TotalDamage = totalDamage,
    //         TotalDamageThreshold = totalDamageThreshold,
    //         Coordinates = coords,
    //     };
    //     return status;
    // }
    // ADT-Tweak End
}
