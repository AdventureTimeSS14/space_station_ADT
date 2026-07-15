using System.Numerics;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.SuitSensors;

public abstract class SharedSuitSensorSystem : EntitySystem
{
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

    // #ADT-Tweak Start - New Monitor: wearer → OnMob sensor index
    /// <summary>
    /// Wearer → OnMob suit-sensor entity. Avoids an O(S) EntityQuery in GetSensorState.
    /// </summary>
    private readonly Dictionary<EntityUid, EntityUid> _onMobSensorsByWearer = new();
    // #ADT-Tweak End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuitSensorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SuitSensorComponent, ComponentStartup>(OnStartup); //ADT-Tweak: NewMonitor
        SubscribeLocalEvent<SuitSensorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SuitSensorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<SuitSensorComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SuitSensorComponent, EmpDisabledRemovedEvent>(OnEmpFinished);
        SubscribeLocalEvent<SuitSensorComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SuitSensorComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<SuitSensorComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<SuitSensorComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<SuitSensorComponent, SuitSensorChangeDoAfterEvent>(OnSuitSensorDoAfter);

    }

    private void OnMapInit(Entity<SuitSensorComponent> ent, ref MapInitEvent args)
    {
        // Fallback
        // #ADT-Tweak Start - New Monitor: OnMob self-user + index at map init
        if (ent.Comp.OnMob)
        {
            ent.Comp.User = ent.Owner;
            IndexOnMobSensor(ent);
        }
        // #ADT-Tweak End

        // generate random mode
        if (ent.Comp.RandomMode)
        {
            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                SuitSensorMode.SensorOff,
                SuitSensorMode.SensorBinary, SuitSensorMode.SensorBinary,
                SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals,
                SuitSensorMode.SensorCords, SuitSensorMode.SensorCords
            };
            ent.Comp.Mode = _random.Pick(modesDist);
        }

        // Spread initial reports over the first interval so a round start does
        // not update every uniform on the same tick.
        ent.Comp.NextUpdate =
            _timing.CurTime +
            TimeSpan.FromSeconds(_random.NextFloat() * (float) ent.Comp.UpdateRate.TotalSeconds);
        Dirty(ent);
    }

    // #ADT-Tweak Start - New Monitor: OnMob startup/shutdown indexing
    private void OnStartup(Entity<SuitSensorComponent> ent, ref ComponentStartup args)
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

    protected virtual void OnShutdown(Entity<SuitSensorComponent> ent, ref ComponentShutdown args)
    {
        UnindexOnMobSensor(ent);
    }

    private void IndexOnMobSensor(Entity<SuitSensorComponent> ent)
    {
        if (!ent.Comp.OnMob || ent.Comp.User == null)
            return;

        _onMobSensorsByWearer[ent.Comp.User.Value] = ent.Owner;
    }

    private void UnindexOnMobSensor(Entity<SuitSensorComponent> ent)
    {
        if (!ent.Comp.OnMob || ent.Comp.User == null)
            return;

        if (_onMobSensorsByWearer.TryGetValue(ent.Comp.User.Value, out var indexed) && indexed == ent.Owner)
            _onMobSensorsByWearer.Remove(ent.Comp.User.Value);
    }
    // #ADT-Tweak End

    private void OnEquipped(Entity<SuitSensorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        ent.Comp.User = args.Wearer;
        Dirty(ent);
    }

    private void OnUnequipped(Entity<SuitSensorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    private void OnEmpPulse(Entity<SuitSensorComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        ent.Comp.PreviousMode = ent.Comp.Mode;
        SetSensor(ent.AsNullable(), SuitSensorMode.SensorOff, null);

        ent.Comp.PreviousControlsLocked = ent.Comp.ControlsLocked;
        ent.Comp.ControlsLocked = true;
        // SetSensor already calls Dirty
    }

    private void OnEmpFinished(Entity<SuitSensorComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        SetSensor(ent.AsNullable(), ent.Comp.PreviousMode, null);
        ent.Comp.ControlsLocked = ent.Comp.PreviousControlsLocked;
    }

    private void OnExamine(Entity<SuitSensorComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        string msg;
        switch (ent.Comp.Mode)
        {
            case SuitSensorMode.SensorOff:
                msg = "suit-sensor-examine-off";
                break;
            case SuitSensorMode.SensorBinary:
                msg = "suit-sensor-examine-binary";
                break;
            case SuitSensorMode.SensorVitals:
                msg = "suit-sensor-examine-vitals";
                break;
            case SuitSensorMode.SensorCords:
                msg = "suit-sensor-examine-cords";
                break;
            default:
                return;
        }

        args.PushMarkup(Loc.GetString(msg));
    }

    private void OnVerb(Entity<SuitSensorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        // check if user can change sensor
        if (ent.Comp.ControlsLocked)
            return;

        // standard interaction checks
        if (!args.CanInteract || args.Hands == null)
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target))
            return;

        // #ADT-Tweak Start - New Monitor: OnMob skips wearer incapacitation check
        if (!ent.Comp.OnMob)
        {
            // check if target is incapacitated (cuffed, dead, etc)
            if (ent.Comp.User != null && args.User != ent.Comp.User && _actionBlocker.CanInteract(ent.Comp.User.Value, null))
                return;
        }
        // #ADT-Tweak End

        args.Verbs.UnionWith(new[]
        {
            CreateVerb(ent, args.User, SuitSensorMode.SensorOff),
            CreateVerb(ent, args.User, SuitSensorMode.SensorBinary),
            CreateVerb(ent, args.User, SuitSensorMode.SensorVitals),
            CreateVerb(ent, args.User, SuitSensorMode.SensorCords)
        });
    }

    private void OnInsert(Entity<SuitSensorComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = args.Container.Owner;
        Dirty(ent);
    }

    private void OnRemove(Entity<SuitSensorComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.OnMob) //ADT-Tweak: NewMonitor
            return;

        if (args.Container.ID != ent.Comp.ActivationContainer)
            return;

        ent.Comp.User = null;
        Dirty(ent);
    }

    private Verb CreateVerb(Entity<SuitSensorComponent> ent, EntityUid userUid, SuitSensorMode mode)
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

    public string GetModeName(SuitSensorMode mode)
    {
        string name;
        switch (mode)
        {
            case SuitSensorMode.SensorOff:
                name = "suit-sensor-mode-off";
                break;
            case SuitSensorMode.SensorBinary:
                name = "suit-sensor-mode-binary";
                break;
            case SuitSensorMode.SensorVitals:
                name = "suit-sensor-mode-vitals";
                break;
            case SuitSensorMode.SensorCords:
                name = "suit-sensor-mode-cords";
                break;
            default:
                return "";
        }

        return Loc.GetString(name);
    }

    /// <summary>
    /// Attempts to set <see cref="SuitSensorComponent"/> mode of the entity to the selected in params.
    /// Works instantly if the user is the player wearing the sensors and will start a DoAfter otherwise.
    /// </summary>
    /// <param name="sensors">Entity and its component that should be changed.</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">userUid, when not equal to the <see cref="SuitSensorComponent.User"/>, creates doafter</param>
    public bool TrySetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid userUid)
    {
        if (!Resolve(sensors, ref sensors.Comp, false))
            return false;

        if (sensors.Comp.User == null || userUid == sensors.Comp.User)
            SetSensor(sensors, mode, userUid);
        else
        {
            var doAfterEvent = new SuitSensorChangeDoAfterEvent(mode);
            var doAfterArgs = new DoAfterArgs(EntityManager, userUid, sensors.Comp.SensorsTime, doAfterEvent, sensors)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
        }
        return true;
    }

    private void OnSuitSensorDoAfter(Entity<SuitSensorComponent> sensors, ref SuitSensorChangeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        SetSensor(sensors.AsNullable(), args.Mode, args.User);
    }

    /// <summary>
    /// Sets mode of the <see cref="SuitSensorComponent"/> of the chosen entity.
    /// Makes popup when <param name="userUid"> not null
    /// </summary>
    /// <param name="sensors">Entity and it's component that should be changed</param>
    /// <param name="mode">Selected mode</param>
    /// <param name="userUid">uid, required for the popup</param>
    public void SetSensor(Entity<SuitSensorComponent?> sensors, SuitSensorMode mode, EntityUid? userUid = null)
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
    public void SetAllSensors(EntityUid target, SuitSensorMode mode, SlotFlags slots = SlotFlags.All)
    {
        // iterate over all inventory slots
        var slotEnumerator = _inventory.GetSlotEnumerator(target, slots);
        while (slotEnumerator.NextItem(out var item, out _))
        {
            if (TryComp<SuitSensorComponent>(item, out var sensorComp))
                SetSensor((item, sensorComp), mode);
        }
    }

    /// <summary>
    /// Attempts to get full <see cref="SuitSensorStatus"/> from the <see cref="SuitSensorComponent"/>
    /// </summary>
    /// <param name="uid">Entity to get status</param>
    /// <returns>Full <see cref="SuitSensorStatus"/> of the chosen uid</returns>
    public SuitSensorStatus? GetSensorState(Entity<SuitSensorComponent?, TransformComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return null;

        var sensor = ent.Comp1;
        var transform = ent.Comp2;

        // #ADT-Tweak Start - New Monitor: prefer active OnMob sensor over uniform
        // Prefer an *active* OnMob sensor over the uniform. An Off OnMob sensor
        // must not silence the jumpsuit, or that wearer vanishes from monitors.
        if (!sensor.OnMob &&
            sensor.User != null &&
            _onMobSensorsByWearer.TryGetValue(sensor.User.Value, out var onMobUid) &&
            TryComp(onMobUid, out SuitSensorComponent? onMob) &&
            onMob.Mode != SuitSensorMode.SensorOff)
        {
            return null;
        }
        // #ADT-Tweak End

        // The wearer is the source of truth for position. Clothing can be inside
        // containers and neither the clothing nor the wearer has to be on a grid.
        if (sensor.User == null ||
            !HasComp<MobStateComponent>(sensor.User) ||
            !TryComp<TransformComponent>(sensor.User.Value, out var userTransform))
        {
            return null;
        }

        // try to get mobs id from ID slot
        var userName = Loc.GetString("suit-sensor-component-unknown-name");
        var userJob = Loc.GetString("suit-sensor-component-unknown-job");
        var userJobIcon = "JobIconNoId";
        List<string>? userJobDepartments = null;

        if (_idCardSystem.TryFindIdCard(sensor.User.Value, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.LocalizedJobTitle != null)
                userJob = card.Comp.LocalizedJobTitle;
            userJobIcon = card.Comp.JobIcon;

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
        }

        userJobDepartments ??= SuitSensorStatus.NoDepartments;

        // get health mob state
        var isAlive = false;
        if (TryComp(sensor.User.Value, out MobStateComponent? mobState))
            isAlive = !_mobStateSystem.IsDead(sensor.User.Value, mobState);

        // finally, form suit sensor status
        var status = new SuitSensorStatus(GetNetEntity(sensor.User.Value), GetNetEntity(ent.Owner), userName, userJob, userJobIcon, userJobDepartments)
        {
            IsAlive = isAlive,
        };
        switch (sensor.Mode)
        {
            case SuitSensorMode.SensorBinary:
                status.IsAlive = isAlive;
                break;
            case SuitSensorMode.SensorVitals:
            case SuitSensorMode.SensorCords:
            {
                status.IsAlive = isAlive;

                // Damage / threshold only for vitals+ modes — skip for binary.
                if (TryComp<DamageableComponent>(sensor.User.Value, out var damageable))
                    status.TotalDamage = _damageableSystem.GetTotalDamage((sensor.User.Value, damageable)).Int();

                if (_mobThresholdSystem.TryGetThresholdForState(sensor.User.Value, MobState.Critical, out var critThreshold))
                    status.TotalDamageThreshold = critThreshold.Value.Int();

                if (sensor.Mode != SuitSensorMode.SensorCords)
                    break;

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

        // Preserve current sensor mode so the monitor UI can filter and mask data correctly.
        status.Mode = sensor.Mode; //ADT-Tweak: NewMonitor

        return status;
    }
}
