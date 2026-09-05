// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Shared._RMC14.Atmos;

public abstract class SharedRMCFlammableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCOnCollideSystem _onCollide = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly ProtoId<ReagentPrototype> WaterReagent = "Water";
    private static readonly ProtoId<TagPrototype> StructureTag = "Structure";
    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    private EntityQuery<BlockTileFireComponent> _blockTileFireQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<FlammableComponent> _flammableQuery;
    private EntityQuery<RMCIgniteOnCollideComponent> _igniteOnCollideQuery;
    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<TileFireComponent> _tileFireQuery;
    private EntityQuery<InventoryComponent> _inventoryQuery;

    private readonly TileFireSchedule _tileFireSchedule = new();

    private readonly List<EntityUid> _pendingIgnite = new();
    private readonly List<EntityUid> _pendingExtinguish = new();
    private readonly List<EntityUid> _pendingExtinguishTile = new();
    private readonly HashSet<EntityUid> _contacts = new();

    public override void Initialize()
    {
        _blockTileFireQuery = GetEntityQuery<BlockTileFireComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _flammableQuery = GetEntityQuery<FlammableComponent>();
        _igniteOnCollideQuery = GetEntityQuery<RMCIgniteOnCollideComponent>();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _tileFireQuery = GetEntityQuery<TileFireComponent>();
        _inventoryQuery = GetEntityQuery<InventoryComponent>();

        SubscribeLocalEvent<IgniteOnProjectileHitComponent, ProjectileHitEvent>(OnIgniteOnProjectileHit);

        SubscribeLocalEvent<TileFireComponent, MapInitEvent>(OnTileFireMapInit);
        SubscribeLocalEvent<TileFireComponent, VaporHitEvent>(OnTileFireVaporHit);
        SubscribeLocalEvent<TileFireComponent, InteractHandEvent>(OnTileFireInteractHand, before: new[] { typeof(InteractionPopupSystem), typeof(DamagePopupSystem) });
        SubscribeLocalEvent<TileFireComponent, PreventCollideEvent>(OnTileFirePreventCollide);

        SubscribeLocalEvent<CraftsIntoMolotovComponent, ExaminedEvent>(OnCraftsIntoMolotovExamined);
        SubscribeLocalEvent<CraftsIntoMolotovComponent, InteractUsingEvent>(OnCraftsIntoMolotovInteractUsing);
        SubscribeLocalEvent<CraftsIntoMolotovComponent, CraftMolotovDoAfterEvent>(OnCraftsIntoMolotovDoAfter);

        SubscribeLocalEvent<RMCIgniteOnCollideComponent, ComponentStartup>(OnIgniteStartup);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, StartCollideEvent>(OnIgniteCollide);
        SubscribeLocalEvent<RMCIgniteOnCollideComponent, RMCDamageCollideEvent>(OnIgniteDamageCollide);

        SubscribeLocalEvent<ExtinguishFireComponent, ComponentStartup>(OnExtinguishFireStartup);
        SubscribeLocalEvent<SprayExtinguishTileFireComponent, ComponentStartup>(OnExtinguishTileFireStartup);

        SubscribeLocalEvent<SteppingOnFireComponent, DamageModifyEvent>(OnSteppingDamageModify, after: new[] { typeof(InventorySystem) });
        SubscribeLocalEvent<RMCFireArmorDebuffModifierComponent, ExaminedEvent>(OnFireArmorDebuffExamined);
        Subs.SubscribeWithRelay<RMCFireArmorDebuffModifierComponent, RMCGetFireArmorDebuffEvent>(OnGetFireArmorDebuff);

        SubscribeLocalEvent<RMCDamageOnCollideComponent, RMCDamageCollideAttemptEvent>(OnDamageCollideAttempt);

        SubscribeLocalEvent<CanBeFirePattedComponent, InteractHandEvent>(OnCanBeFirePattedInteractHand, before: new[] { typeof(InteractionPopupSystem), typeof(DamagePopupSystem) });

        SubscribeLocalEvent<FlammableComponent, IgnitedEvent>(OnFlammableIgnite);
        SubscribeLocalEvent<FlammableComponent, ExtinguishedEvent>(OnFlammableExtinguished);

        SubscribeLocalEvent<RMCImmuneToIgnitionComponent, ExaminedEvent>(OnIgnitionImmunityExamined);

        SubscribeLocalEvent<RMCImmuneToFireTileDamageComponent, RMCGetFireImmunityEvent>(OnImmuneToTileFireGet);
        SubscribeLocalEvent<RMCImmuneToFireTileDamageComponent, ExaminedEvent>(OnImmuneToTileFireExamined);

        Subs.SubscribeWithRelay<RMCImmuneToIgnitionComponent, GetIgnitionImmunityEvent>(OnGetIgnitionImmunity);
    }

    private void OnIgniteOnProjectileHit(Entity<IgniteOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!CanBeIgnited(args.Target, ent, ent.Comp.Intensity))
            return;

        ChangeBurnColor(args.Target, ent.Comp.BurnColor);
        if (Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.Duration))
            ApplyVacuumBehaviour(args.Target, ent.Comp.BurnsInVacuum, ent.Comp.VacuumBurnout, ent.Comp.Duration);
    }

    private void OnTileFireMapInit(Entity<TileFireComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpawnedAt = _timing.CurTime;
        Dirty(ent);

        ScheduleTileFire(ent);
    }

    private void OnTileFireVaporHit(Entity<TileFireComponent> ent, ref VaporHitEvent args)
    {
        if (_net.IsClient)
            return;

        var water = false;
        foreach (var container in args.Solution.Comp.Containers)
        {
            if (!_solutionContainer.TryGetSolution(args.Solution.Owner, container, out _, out var solution))
                continue;

            if (solution.ContainsPrototype(WaterReagent))
            {
                water = true;
                break;
            }
        }

        if (!water)
            return;

        if (ent.Comp.ExtinguishInstantly)
        {
            QueueDel(ent.Owner);
            return;
        }

        ent.Comp.Duration -= TimeSpan.FromSeconds(args.Power);
        Dirty(ent);
        ScheduleTileFire(ent);
    }

    private void OnTileFireInteractHand(Entity<TileFireComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (!TryComp(user, out TileFirePatterComponent? patter))
            return;

        var time = _timing.CurTime;
        if (time < patter.Last + patter.Cooldown)
            return;

        patter.Last = time;
        Dirty(user, patter);

        ent.Comp.Duration -= patter.RemoveDuration * ent.Comp.PatExtinguishMultiplier;
        Dirty(ent);
        ScheduleTileFire(ent);

        _audio.PlayPredicted(patter.Sound, user, user, AudioParams.Default.WithVolume(-8).WithVariation(0.05f));
    }

    private void OnTileFirePreventCollide(Entity<TileFireComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        if (_projectileQuery.HasComp(args.OtherEntity) ||
            _tileFireQuery.HasComp(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    private void OnCraftsIntoMolotovExamined(Entity<CraftsIntoMolotovComponent> ent, ref ExaminedEvent args)
    {
        if (!CanCraftMolotovPopup(ent, args.Examiner, false, out _))
            return;

        using (args.PushGroup(nameof(CraftsIntoMolotovComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-molotov-can-craft"));
        }
    }

    private void OnCraftsIntoMolotovInteractUsing(Entity<CraftsIntoMolotovComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<PaperComponent>(args.Used))
            return;

        if (!CanCraftMolotovPopup(ent, args.User, true, out _))
            return;

        var ev = new CraftMolotovDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, ev, ent, ent, args.Used)
        {
            BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnCraftsIntoMolotovDoAfter(Entity<CraftsIntoMolotovComponent> ent, ref CraftMolotovDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        if (!HasComp<PaperComponent>(args.Used))
            return;

        if (!CanCraftMolotovPopup(ent, args.User, true, out var intensity))
            return;

        if (_net.IsClient)
            return;

        var coords = _transform.GetMoverCoordinates(ent.Owner);
        var molotov = Spawn(ent.Comp.Spawns, coords);

        var tileFire = EnsureComp<TileFireOnTriggerComponent>(molotov);
        tileFire.Duration = intensity.Int();
        Dirty(molotov, tileFire);

        Del(ent.Owner);
        Del(args.Used);

        _hands.TryPickupAnyHand(args.User, molotov);
    }

    private void OnIgniteCollide(Entity<RMCIgniteOnCollideComponent> ent, ref StartCollideEvent args)
    {
        TryIgnite(ent, args.OtherEntity, false);
    }

    private void OnIgniteDamageCollide(Entity<RMCIgniteOnCollideComponent> ent, ref RMCDamageCollideEvent args)
    {
        if (!CanBeIgnited(args.Target, ent, ent.Comp.Intensity, true))
            return;

        if (Ignite(args.Target, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks, ent.Comp.TileDamage))
            ApplyVacuumBehaviour(args.Target, ent.Comp.BurnsInVacuum, ent.Comp.VacuumBurnout, ent.Comp.Duration);
    }

    private void OnDamageCollideAttempt(Entity<RMCDamageOnCollideComponent> ent, ref RMCDamageCollideAttemptEvent args)
    {
        if (args.Cancelled || !args.Fire)
            return;

        if (!CanFireBypassImmunity(ent.Owner, args.Target))
            args.Cancelled = true;
    }

    private void OnCanBeFirePattedInteractHand(Entity<CanBeFirePattedComponent> ent, ref InteractHandEvent args)
    {
        var user = args.User;
        if (args.Target != ent.Owner ||
            user == args.Target ||
            !TryComp(user, out FirePatterComponent? patter) ||
            _entityWhitelist.IsWhitelistPass(patter.Blacklist, ent) ||
            !TryComp(ent, out FlammableComponent? flammable) ||
            !flammable.OnFire)
        {
            return;
        }

        args.Handled = true;
        var time = _timing.CurTime;
        if (time < patter.LastPat + patter.Cooldown)
            return;

        patter.LastPat = time;
        Dirty(user, patter);

        Pat(ent.Owner, patter.Stacks);

        _audio.PlayPredicted(patter.Sound, user, user);
        _popup.PopupClient(Loc.GetString("rmc-fire-pat-self", ("target", ent.Owner)), ent, user, PopupType.SmallCaution);
        _popup.PopupEntity(Loc.GetString("rmc-fire-pat-target", ("user", user)), ent, ent, PopupType.SmallCaution);

        var others = Filter.PvsExcept(ent.Owner).RemoveWhereAttachedEntity(e => e == user || e == ent.Owner);
        _popup.PopupEntity(Loc.GetString("rmc-fire-pat-others", ("user", user), ("target", ent.Owner)), ent, others, true);
    }

    private void OnFlammableIgnite(Entity<FlammableComponent> ent, ref IgnitedEvent args)
    {
        EnsureComp<OnFireComponent>(ent);
    }

    private void OnFlammableExtinguished(Entity<FlammableComponent> ent, ref ExtinguishedEvent args)
    {
        RemCompDeferred<OnFireComponent>(ent);
        RemCompDeferred<RMCFireBypassActiveComponent>(ent);
    }

    private void OnGetIgnitionImmunity(Entity<RMCImmuneToIgnitionComponent> ent, ref GetIgnitionImmunityEvent args)
    {
        if (ent.Comp.IntensityResistance < args.Intensity)
            return;

        if (!ent.Comp.ImmuneToDirectHits && args.DirectHit)
            return;

        args.Ignite = false;
    }

    private void OnIgnitionImmunityExamined(Entity<RMCImmuneToIgnitionComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCImmuneToIgnitionComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-immune-to-ignition-examine", ("ent", ent.Owner), ("direct", ent.Comp.ImmuneToDirectHits)));
        }
    }

    private void OnImmuneToTileFireGet(Entity<RMCImmuneToFireTileDamageComponent> ent, ref RMCGetFireImmunityEvent args)
    {
        if (args.Fire == null)
        {
            args.Immune = true;
            return;
        }

        if (_entityWhitelist.IsWhitelistPass(ent.Comp.BypassWhitelist, args.Fire.Value))
            return;

        args.Immune = true;
    }

    private void OnImmuneToTileFireExamined(Entity<RMCImmuneToFireTileDamageComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCImmuneToFireTileDamageComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-immune-to-fire-tile-damage-examine", ("ent", ent.Owner)));
        }
    }

    public bool IsOnFire(Entity<FlammableComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.OnFire;
    }

    public virtual bool Ignite(Entity<FlammableComponent?> flammable, int intensity, int duration, int? maxStacks, DamageSpecifier? tileDamage = null)
    {
        return false;
    }

    public virtual void Extinguish(Entity<FlammableComponent?> flammable)
    {
    }

    public virtual void Pat(Entity<FlammableComponent?> flammable, int stacks)
    {
    }

    public virtual void AdjustStacks(Entity<FlammableComponent?> flammable, int stacks)
    {
    }

    private void SpawnFireChain(EntProtoId spawn, EntityUid chain, EntityCoordinates coordinates, int? intensity, int? duration)
    {
        var spawned = Spawn(spawn, coordinates);
        if (intensity != null || duration != null)
        {
            var ignite = EnsureComp<RMCIgniteOnCollideComponent>(spawned);
            var tileFire = EnsureComp<TileFireComponent>(spawned);
            if (intensity != null)
                ignite.Intensity = intensity.Value;

            if (duration != null)
            {
                ignite.Duration = duration.Value;
                tileFire.Duration = TimeSpan.FromSeconds(duration.Value);
            }

            Dirty(spawned, tileFire);
            ScheduleTileFire((spawned, tileFire));
        }

        var onCollide = EnsureComp<RMCDamageOnCollideComponent>(spawned);
        _onCollide.SetChain((spawned, onCollide), chain);
    }

    private void SpawnFires(EntProtoId spawn, EntityCoordinates coordinates, int range, EntityUid chain, int? intensity, int? duration, HashSet<EntityCoordinates>? spawned = null)
    {
        if (_net.IsClient)
            return;

        spawned ??= new HashSet<EntityCoordinates>();
        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var target = coordinates.Offset(cardinal.ToVec());
            if (!spawned.Add(target))
                continue;

            var nextRange = SpawnFire(target, spawn, chain, range, intensity, duration, out var cont);
            if (nextRange == 0 || cont)
                continue;

            Timer.Spawn(TimeSpan.FromMilliseconds(50),
                () =>
                {
                    try
                    {
                        SpawnFires(spawn, target, nextRange, chain, intensity, duration, spawned);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error occurred spawning fires:\n{e}");
                    }
                });
        }
    }

    public void SpawnFireDiamond(EntProtoId spawn, EntityCoordinates center, int range, int? intensity = null, int? duration = null)
    {
        if (_net.IsClient)
            return;

        var chain = _onCollide.SpawnChain();
        SpawnFire(center, spawn, chain, range, intensity, duration, out _);
        SpawnFires(spawn, center, range, chain, intensity, duration);
        _onCollide.CleanupChain(chain);
    }

    public int SpawnFire(EntityCoordinates target, EntProtoId spawn, EntityUid chain, int range, int? intensity, int? duration, out bool cont)
    {
        cont = false;
        if (!_rmcMap.TryGetTileDef(target, out var tile) ||
            tile.ID == ContentTileDefinition.SpaceID)
        {
            cont = true;
            return range;
        }

        if (_rmcMap.HasAnchoredEntityEnumerator<TileFireComponent>(target, out var oldTileFire))
        {
            if (spawn == oldTileFire.Comp.Id)
            {
                cont = true;
                return range;
            }

            QueueDel(oldTileFire.Owner);
        }

        var nextRange = range - 1;
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(target);
        while (anchored.MoveNext(out var uid))
        {
            if (_blockTileFireQuery.HasComp(uid))
            {
                nextRange = 0;
                break;
            }

            if (_tag.HasAnyTag(uid, StructureTag, WallTag) &&
                !_doorQuery.HasComp(uid))
            {
                nextRange = 0;
                break;
            }
        }

        SpawnFireChain(spawn, chain, target, intensity, duration);
        return nextRange;
    }

    private bool CanCraftMolotovPopup(Entity<CraftsIntoMolotovComponent> ent, EntityUid user, bool popup, out FixedPoint2 intensity)
    {
        intensity = default;
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution) ||
            solution.Volume <= FixedPoint2.Zero)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-molotov-empty", ("bottle", ent.Owner)), ent, user, PopupType.SmallCaution);

            return false;
        }

        intensity = FixedPoint2.Zero;
        foreach (var solutionReagent in solution)
        {
            if (!_prototype.TryIndex(solutionReagent.Reagent.Prototype, out ReagentPrototype? reagent))
                continue;

            intensity += reagent.IntensityMod * solutionReagent.Quantity;
        }

        if (intensity < ent.Comp.MinIntensity)
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("rmc-molotov-not-flammable", ("bottle", ent.Owner)), ent, user, PopupType.SmallCaution);

            return false;
        }

        intensity = FixedPoint2.Min(intensity, ent.Comp.MaxIntensity);
        return true;
    }

    private void ApplyVacuumBehaviour(EntityUid target, bool burnsInVacuum, TimeSpan burnout, int duration)
    {
        if (!TryComp(target, out OnFireComponent? onFire))
            return;

        onFire.BurnsInVacuum = burnsInVacuum;
        onFire.VacuumDecay = burnout.TotalSeconds <= 0 ? duration : duration / (float) burnout.TotalSeconds;
        Dirty(target, onFire);
    }

    public void SetVacuumBehaviour(Entity<RMCIgniteOnCollideComponent?, TileFireComponent?> ent, bool burnsInVacuum, TimeSpan burnout)
    {
        Resolve(ent, ref ent.Comp1, ref ent.Comp2, false);

        if (ent.Comp1 != null)
        {
            ent.Comp1.BurnsInVacuum = burnsInVacuum;
            ent.Comp1.VacuumBurnout = burnout;
        }

        if (ent.Comp2 != null)
        {
            ent.Comp2.BurnsInVacuum = burnsInVacuum;
            ent.Comp2.VacuumDuration = burnout;
            Dirty(ent.Owner, ent.Comp2);
            ScheduleTileFire((ent.Owner, ent.Comp2));
        }
    }

    public void SetIntensityDuration(Entity<RMCIgniteOnCollideComponent?, RMCDamageOnCollideComponent?, TileFireComponent?> ent, int? intensity, int? duration)
    {
        Resolve(ent, ref ent.Comp1, ref ent.Comp2, ref ent.Comp3, false);
        if (ent.Comp1 != null)
        {
            if (intensity != null)
                ent.Comp1.Intensity = intensity.Value;

            if (duration != null)
                ent.Comp1.Duration = duration.Value;
        }

        if (ent.Comp2 != null)
        {
            if (intensity != null)
                ent.Comp2.Damage.DamageDict[HeatDamage] = intensity.Value * ent.Comp2.DirectHitMultiplier;
        }

        if (ent.Comp3 != null)
        {
            if (duration != null)
            {
                ent.Comp3.Duration = TimeSpan.FromSeconds(duration.Value);
                Dirty(ent.Owner, ent.Comp3);
                ScheduleTileFire((ent.Owner, ent.Comp3));
            }
        }
    }

    private void TryIgnite(Entity<RMCIgniteOnCollideComponent> ent, EntityUid other, bool checkIgnited)
    {
        if (!HasComp<DamageableComponent>(other))
            return;

        if (_tileFireQuery.HasComp(ent.Owner) && _blockTileFireQuery.HasComp(other))
        {
            RemCompDeferred<SteppingOnFireComponent>(other);
            return;
        }

        if (Transform(other).Anchored)
            return;

        EnsureComp<SteppingOnFireComponent>(other);
        var flammableEnt = new Entity<FlammableComponent?>(other, null);
        if (!Resolve(flammableEnt, ref flammableEnt.Comp, false))
            return;

        var wasOnFire = IsOnFire(flammableEnt);
        if (checkIgnited && wasOnFire)
            return;

        if (!CanBeIgnited(other, ent, ent.Comp.Intensity))
            return;

        var tileEv = new RMCGetFireImmunityEvent(ent.Owner);
        RaiseLocalEvent(other, ref tileEv);

        if (!tileEv.Ignite)
            return;

        if (!Ignite(flammableEnt, ent.Comp.Intensity, ent.Comp.Duration, ent.Comp.MaxStacks, ent.Comp.TileDamage))
            return;

        ApplyVacuumBehaviour(other, ent.Comp.BurnsInVacuum, ent.Comp.VacuumBurnout, ent.Comp.Duration);
        ChangeBurnColor(flammableEnt, ent.Comp.BurnColor);

        if (CanFireBypassImmunity(ent.Owner, other))
            EnsureComp<RMCFireBypassActiveComponent>(other);
        else
            RemCompDeferred<RMCFireBypassActiveComponent>(other);

        if (!wasOnFire && IsOnFire(flammableEnt) && CanFireBypassImmunity(ent.Owner, other))
            _damageable.TryChangeDamage(flammableEnt.Owner, flammableEnt.Comp.Damage * ent.Comp.Intensity, true);
    }

    private void ApplyTileEffect(Entity<SteppingOnFireComponent> ent, RMCIgniteOnCollideComponent ignite, EntityUid fireEntity)
    {
        if (_blockTileFireQuery.HasComp(ent.Owner))
        {
            RemCompDeferred<SteppingOnFireComponent>(ent);
            return;
        }

        var timing = _timing.CurTime;
        var stepping = ent.Comp;
        var uid = ent.Owner;

        if (ignite.ArmorMultiplier < stepping.ArmorMultiplier &&
            _entityWhitelist.IsWhitelistPassOrNull(ignite.ArmorWhitelist, uid))
        {
            var debuffEv = new RMCGetFireArmorDebuffEvent();
            RaiseLocalEvent(uid, ref debuffEv);

            if (_inventoryQuery.TryComp(uid, out var debuffInv))
                _inventory.RelayEvent((uid, debuffInv), ref debuffEv);

            stepping.ArmorMultiplier = Math.Min(1f, ignite.ArmorMultiplier * debuffEv.Modifier);
        }

        if (ignite.TileDamage is not { } tile)
            return;

        var coords = _transform.GetMoverCoordinates(uid);
        if (stepping.LastPosition is { } last &&
            last.TryDistance(EntityManager, _transform, coords, out var distance))
        {
            stepping.Distance += distance;
            if (stepping.Distance >= 1)
            {
                stepping.Distance = 0;
                if (CanFireBypassImmunity(fireEntity, uid))
                    _damageable.TryChangeDamage(uid, tile * ignite.Intensity, true);
            }
        }

        if (!_flammableQuery.TryComp(ent.Owner, out var flammable))
            return;

        if (CanBeIgnited(uid, fireEntity, ignite.Intensity))
        {
            if (Ignite((uid, flammable), ignite.Intensity, ignite.Duration, ignite.MaxStacks, ignite.TileDamage))
                ApplyVacuumBehaviour(uid, ignite.BurnsInVacuum, ignite.VacuumBurnout, ignite.Duration);

            if (CanFireBypassImmunity(fireEntity, uid))
                EnsureComp<RMCFireBypassActiveComponent>(uid);
            else
                RemCompDeferred<RMCFireBypassActiveComponent>(uid);
        }
        else if (CanFireBypassImmunity(fireEntity, uid))
        {
            var ev = new GetFireProtectionEvent();
            RaiseLocalEvent(uid, ref ev);

            if (_inventoryQuery.TryComp(uid, out var inv))
                _inventory.RelayEvent((uid, inv), ref ev);

            if (stepping.UpdateAt <= timing)
            {
                _damageable.TryChangeDamage(uid, ignite.Intensity / 5f * flammable.Damage * ev.Multiplier, true, false);
                stepping.UpdateAt = timing + stepping.UpdateTime;
            }
        }

        stepping.LastPosition = coords;
    }

    protected virtual bool HasOxygen(EntityUid uid)
    {
        return true;
    }

    public bool CanBurnThroughImmunity(EntityUid uid)
    {
        var ev = new RMCGetFireImmunityEvent(null);
        RaiseLocalEvent(uid, ref ev);

        if (!ev.Immune && !HasComp<RMCImmuneToFireTileDamageComponent>(uid))
            return true;

        return HasComp<RMCFireBypassActiveComponent>(uid);
    }

    private bool CanFireBypassImmunity(EntityUid fireEntity, EntityUid targetEntity)
    {
        if (HasComp<RMCFireImmunityBypassComponent>(fireEntity))
            return true;

        var tileEv = new RMCGetFireImmunityEvent(fireEntity);
        RaiseLocalEvent(targetEntity, ref tileEv);

        return !tileEv.Immune;
    }

    public bool CanBeIgnited(EntityUid target, EntityUid fireSource, int intensity, bool directHit = false)
    {
        var ev = new GetIgnitionImmunityEvent(intensity, directHit);
        RaiseLocalEvent(target, ref ev);

        if (_inventoryQuery.TryComp(target, out var inv))
            _inventory.RelayEvent((target, inv), ref ev);

        return ev.Ignite;
    }

    public void ChangeBurnColor(EntityUid target, Color color)
    {
        if (TryComp<RMCFireColorComponent>(target, out var fireColorComp))
        {
            fireColorComp.Color = color;
            Dirty(target, fireColorComp);
        }
    }

    private void OnIgniteStartup(Entity<RMCIgniteOnCollideComponent> ent, ref ComponentStartup args)
    {
        _pendingIgnite.Add(ent.Owner);
    }

    private void OnExtinguishFireStartup(Entity<ExtinguishFireComponent> ent, ref ComponentStartup args)
    {
        _pendingExtinguish.Add(ent.Owner);
    }

    private void OnExtinguishTileFireStartup(Entity<SprayExtinguishTileFireComponent> ent, ref ComponentStartup args)
    {
        _pendingExtinguishTile.Add(ent.Owner);
    }

    private void ProcessPendingIgnite()
    {
        if (_pendingIgnite.Count == 0)
            return;

        try
        {
            foreach (var uid in _pendingIgnite)
            {
                if (!_igniteOnCollideQuery.TryComp(uid, out var apply) || apply.InitDamaged)
                    continue;

                apply.InitDamaged = true;

                foreach (var contact in _physics.GetEntitiesIntersectingBody(uid, (int) apply.Collision))
                {
                    TryIgnite((uid, apply), contact, true);
                }

                _onCollide.DisableDamageOnCollide(uid);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing {nameof(RMCIgniteOnCollideComponent)}:\n{e}");
        }
        finally
        {
            _pendingIgnite.Clear();
        }
    }

    private static TileFireVisuals GetTileFireVisual(TileFireComponent fire, TimeSpan time)
    {
        if (time < fire.SpawnedAt + fire.BigFireDuration)
            return TileFireVisuals.Four;

        var timeLeft = fire.SpawnedAt + fire.Duration - time;
        if (timeLeft < fire.Duration * 0.33)
            return TileFireVisuals.One;

        if (timeLeft < fire.Duration * 0.66)
            return TileFireVisuals.Two;

        return TileFireVisuals.Three;
    }

    private static TimeSpan GetNextTileFireEvent(TileFireComponent fire, TimeSpan time)
    {
        var despawnAt = fire.SpawnedAt + fire.Duration;
        var next = despawnAt;

        Consider(fire.SpawnedAt + fire.BigFireDuration);
        Consider(despawnAt - fire.Duration * 0.66);
        Consider(despawnAt - fire.Duration * 0.33);

        return next;

        void Consider(TimeSpan candidate)
        {
            if (candidate > time && candidate < next)
                next = candidate;
        }
    }

    public void ScheduleTileFire(Entity<TileFireComponent> ent)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var timeLeft = ent.Comp.SpawnedAt + ent.Comp.Duration - time;
        if (timeLeft <= TimeSpan.Zero)
        {
            QueueDel(ent.Owner);
            return;
        }

        if (!ent.Comp.BurnsInVacuum && timeLeft > ent.Comp.VacuumDuration && !HasOxygen(ent.Owner))
        {
            ent.Comp.Duration -= timeLeft - ent.Comp.VacuumDuration;
            Dirty(ent);
        }

        var visual = GetTileFireVisual(ent.Comp, time);
        if (visual != ent.Comp.Visual)
        {
            ent.Comp.Visual = visual;
            _appearance.SetData(ent.Owner, TileFireLayers.Base, visual);
        }

        var next = GetNextTileFireEvent(ent.Comp, time);
        ent.Comp.ScheduledAt = next;
        _tileFireSchedule.Enqueue(ent.Owner, next);
    }

    private void ProcessTileFireSchedule()
    {
        var time = _timing.CurTime;

        try
        {
            while (_tileFireSchedule.TryPeek(out var uid, out var at) && at <= time)
            {
                _tileFireSchedule.Dequeue();

                if (!_tileFireQuery.TryComp(uid, out var fire))
                    continue;

                if (fire.ScheduledAt != at)
                    continue;

                ScheduleTileFire((uid, fire));
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing {nameof(TileFireComponent)}:\n{e}");
        }
    }

    private void ProcessPendingExtinguish()
    {
        if (_pendingExtinguish.Count == 0)
            return;

        try
        {
            foreach (var uid in _pendingExtinguish)
            {
                if (!TryComp(uid, out ExtinguishFireComponent? extinguish) || extinguish.Extinguished)
                    continue;

                extinguish.Extinguished = true;

                foreach (var entIntersecting in _physics.GetEntitiesIntersectingBody(uid, (int) extinguish.Collision))
                {
                    if (!_flammableQuery.TryComp(entIntersecting, out var flammable))
                        continue;

                    var ev = new ExtinguishFireAttemptEvent(uid, entIntersecting);
                    RaiseLocalEvent(uid, ref ev);

                    if (!ev.Cancelled)
                        Extinguish((entIntersecting, flammable));
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing {nameof(ExtinguishFireComponent)}:\n{e}");
        }
        finally
        {
            _pendingExtinguish.Clear();
        }
    }

    private void ProcessPendingExtinguishTile()
    {
        if (_pendingExtinguishTile.Count == 0)
            return;

        try
        {
            foreach (var uid in _pendingExtinguishTile)
            {
                if (!TryComp(uid, out SprayExtinguishTileFireComponent? extinguishTile) || extinguishTile.Extinguished)
                    continue;

                extinguishTile.Extinguished = true;

                var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(uid);
                while (anchored.MoveNext(out var anchorUid))
                {
                    if (!_tileFireQuery.TryComp(anchorUid, out var tileFire))
                        continue;

                    tileFire.Duration -= extinguishTile.ExtinguishAmount * tileFire.SprayExtinguishMultiplier;
                    Dirty(anchorUid, tileFire);
                    ScheduleTileFire((anchorUid, tileFire));
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing {nameof(SprayExtinguishTileFireComponent)}:\n{e}");
        }
        finally
        {
            _pendingExtinguishTile.Clear();
        }
    }

    private void RunSteppingOnFire()
    {
        try
        {
            var steppingQuery = EntityQueryEnumerator<SteppingOnFireComponent, PhysicsComponent>();
            while (steppingQuery.MoveNext(out var uid, out var stepping, out var body))
            {
                stepping.ArmorMultiplier = 1;

                var isStepping = false;

                _contacts.Clear();
                _physics.GetContactingEntities((uid, body), _contacts, approximate: true);
                foreach (var contact in _contacts)
                {
                    if (!_igniteOnCollideQuery.TryComp(contact, out var ignite))
                        continue;

                    ApplyTileEffect((uid, stepping), ignite, contact);
                    isStepping = true;
                    break;
                }

                if (!isStepping)
                    RemCompDeferred<SteppingOnFireComponent>(uid);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error processing {nameof(SteppingOnFireComponent)}:\n{e}");
        }
        finally
        {
            _contacts.Clear();
        }
    }

    private void OnSteppingDamageModify(Entity<SteppingOnFireComponent> ent, ref DamageModifyEvent args)
    {
        var multiplier = ent.Comp.ArmorMultiplier;
        if (multiplier >= 1f)
            return;

        var blocked = args.OriginalDamage - args.Damage;
        args.Damage += blocked * (1f - multiplier);
    }

    private void OnGetFireArmorDebuff(Entity<RMCFireArmorDebuffModifierComponent> ent, ref RMCGetFireArmorDebuffEvent args)
    {
        args.Modifier *= ent.Comp.DebuffModifier;
    }

    private void OnFireArmorDebuffExamined(Entity<RMCFireArmorDebuffModifierComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCFireArmorDebuffModifierComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-fire-armor-debuff-modifier-examine",
                ("ent", ent.Owner),
                ("percentage", $"{(ent.Comp.DebuffModifier - 1) * 100:F0}")));
        }
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        ProcessPendingIgnite();
        ProcessTileFireSchedule();
        ProcessPendingExtinguish();
        ProcessPendingExtinguishTile();
        RunSteppingOnFire();
    }
}

[ByRefEvent]
public record struct GetIgnitionImmunityEvent(int Intensity, bool DirectHit, bool Ignite = true) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => ~SlotFlags.POCKET;
}

[ByRefEvent]
public record struct RMCGetFireImmunityEvent(EntityUid? Fire, bool Ignite = true, bool Immune = false);
