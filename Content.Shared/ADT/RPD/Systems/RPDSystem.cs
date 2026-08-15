using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.ADT.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.ADT.RPD.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.ADT.RPD.Systems;

[Virtual]
public class RPDSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;


    private HashSet<EntityUid> _intersectingEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RPDComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RPDComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RPDComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RPDComponent, RPDDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<RPDComponent, DoAfterAttemptEvent<RPDDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RPDComponent, RPDSystemMessage>(OnRPDSystemMessage);
        SubscribeLocalEvent<RPDComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeNetworkEvent<RPDConstructionGhostRotationEvent>(OnRPDconstructionGhostRotationEvent);
    }

    #region Event handling

    private void OnMapInit(EntityUid uid, RPDComponent component, MapInitEvent args)
    {
        // On init, set the RPD to its first available recipe
        if (component.AvailablePrototypes.Any())
        {
            component.ProtoId = component.AvailablePrototypes.First();
            UpdateCachedPrototype(uid, component);
            Dirty(uid, component);

            return;
        }

        // The RPD has no valid recipes somehow? Get rid of it
        QueueDel(uid);
    }

    private void OnRPDSystemMessage(EntityUid uid, RPDComponent component, RPDSystemMessage args)
    {
        // Exit if the RPD doesn't actually know the supplied prototype
        if (!component.AvailablePrototypes.Contains(args.ProtoId))
            return;

        if (!_protoManager.HasIndex(args.ProtoId))
            return;

        if (args.Secondary)
        {
            // Set the secondary (Alt) RPD prototype
            component.SecondaryProtoId = args.ProtoId;
            UpdateCachedSecondaryPrototype(uid, component);
            Dirty(uid, component);
            return;
        }

        // Set the current RPD prototype to the one supplied
        component.ProtoId = args.ProtoId;
        UpdateCachedPrototype(uid, component);
        Dirty(uid, component);
    }

    private void OnAltVerb(EntityUid uid, RPDComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new AlternativeVerb
        {
            Act = () =>
            {
                _uiSystem.OpenUi(uid, RpdUiKey.Secondary, args.User);
            },
            Text = Loc.GetString("rpd-component-select-secondary"),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void OnExamine(EntityUid uid, RPDComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        // Update cached prototype if required
        UpdateCachedPrototype(uid, component);

        var msg = Loc.GetString("rpd-component-examine-mode-details", ("mode", Loc.GetString(component.CachedPrototype.SetName)));

        if (component.CachedPrototype.Mode == RpdMode.ConstructObject)
        {
            var name = Loc.GetString(component.CachedPrototype.SetName);

            if (component.CachedPrototype.Prototype != null &&
                _protoManager.TryIndex(component.CachedPrototype.Prototype, out var proto))
                name = proto.Name;

            msg = Loc.GetString("rpd-component-examine-build-details", ("name", name));
        }

        // ADT-Tweak: show the secondary (Alt) configuration if one is set
        UpdateCachedSecondaryPrototype(uid, component);
        if (component.CachedSecondaryPrototype != null)
        {
            var secondaryName = Loc.GetString(component.CachedSecondaryPrototype.SetName);

            if (component.CachedSecondaryPrototype.Prototype != null &&
                _protoManager.TryIndex(component.CachedSecondaryPrototype.Prototype, out var secondaryProto))
                secondaryName = secondaryProto.Name;

            msg += "\n" + Loc.GetString("rpd-component-examine-secondary-details", ("name", secondaryName));
        }

        args.PushMarkup(msg);
    }

    private void OnAfterInteract(EntityUid uid, RPDComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryStartRPDOperation(uid, component, args.User, args.ClickLocation, args.Target, false))
            args.Handled = true;
    }

    /// <summary>
    /// Проверяет операцию и запускает её DoAfter. Secondary: вторичная конфигурация (Alt+клик по тайлу).
    /// </summary>
    public bool TryStartRPDOperation(EntityUid uid, RPDComponent component, EntityUid user, EntityCoordinates location, EntityUid? target, bool secondary)
    {
        if (!location.IsValid(EntityManager))
            return false;

        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            _popup.PopupClient(Loc.GetString("rpd-component-no-valid-grid"), uid, user);
            return false;
        }

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        UpdateCachedPrototype(uid, component);
        if (secondary)
            UpdateCachedSecondaryPrototype(uid, component);

        var prototype = secondary ? component.CachedSecondaryPrototype : component.CachedPrototype;

        // Вторичная конфигурация не выбрана
        if (prototype == null)
            return false;

        if (!IsRPDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, target, user, prototype))
            return false;

        if (!_net.IsServer)
            return false;

        var cost = prototype.Cost;
        var delay = prototype.Delay;
        var effectPrototype = prototype.Effect;

        // Deconstructing an object
        if (prototype.Mode == RpdMode.Deconstruct && target != null &&
            TryComp<RPDDeconstructableComponent>(target, out var destructible))
        {
            cost = destructible.Cost;
            delay = destructible.Delay;
            effectPrototype = destructible.Effect;
        }

        var effect = Spawn(effectPrototype, location);
        var ev = new RPDDoAfterEvent(GetNetCoordinates(location), component.ConstructionDirection, secondary ? component.SecondaryProtoId : component.ProtoId, cost, GetNetEntity(effect), secondary);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, ev, uid, target: target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            QueueDel(effect);
            return false;
        }

        return true;
    }

    private void OnDoAfterAttempt(EntityUid uid, RPDComponent component, DoAfterAttemptEvent<RPDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        // Exit if the RPD prototype has changed (check the correct slot: primary or secondary)
        var expectedProtoId = args.Event.Secondary ? component.SecondaryProtoId : component.ProtoId;
        if (expectedProtoId != args.Event.StartingProtoId)
        {
            args.Cancel();
            return;
        }

        // Ensure the RPD operation is still valid
        var location = GetCoordinates(args.Event.Location);
        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            args.Cancel();
            return;
        }

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        var attemptPrototype = args.Event.Secondary ? component.CachedSecondaryPrototype : component.CachedPrototype;
        if (attemptPrototype == null)
        {
            args.Cancel();
            return;
        }

        if (!IsRPDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, args.Event.Target, args.Event.User, attemptPrototype))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RPDComponent component, RPDDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            if (_net.IsServer)
                QueueDel(GetEntity(args.Effect));
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        var location = GetCoordinates(args.Location);

        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);

        // Ensure the RPD operation is still valid
        var finalizePrototype = args.Secondary ? component.CachedSecondaryPrototype : component.CachedPrototype;
        if (finalizePrototype == null)
        {
            if (_net.IsServer)
                QueueDel(GetEntity(args.Effect));
            return;
        }

        if (!IsRPDOperationStillValid(uid, component, gridUid.Value, mapGrid, tile, position, args.Target, args.User, finalizePrototype))
            return;

        // Finalize the operation
        FinalizeRPDOperation(uid, component, gridUid.Value, mapGrid, position, args.Direction, args.Target, args.User, finalizePrototype);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _charges.TryUseCharges(uid, args.Cost);
    }

    private void OnRPDconstructionGhostRotationEvent(RPDConstructionGhostRotationEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);

        // Determine if player that send the message is carrying the specified RPD in their active hand
        if (session.SenderSession.AttachedEntity is not { } player)
            return;

        if (!TryComp<HandsComponent>(player, out var hands) ||
            _hands.GetActiveItem(player) != uid)
            return;

        if (!TryComp<RPDComponent>(uid, out var rpd))
            return;

        // Update the construction direction
        rpd.ConstructionDirection = ev.Direction;
        Dirty(uid, rpd);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRPDOperationStillValid(EntityUid uid, RPDComponent component, EntityUid gridUid, MapGridComponent mapGrid, TileRef tile, Vector2i position, EntityUid? target, EntityUid user, RPDPrototype? prototype = null, bool popMsgs = true)
    {
        // Update cached prototype if required
        UpdateCachedPrototype(uid, component);
        prototype ??= component.CachedPrototype;

        // Check that the RPD has enough ammo to get the job done
        TryComp<LimitedChargesComponent>(uid, out var charges);

        if (_charges.IsEmpty((uid, charges)))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-no-ammo-message"), uid, user);

            return false;
        }

        if (!_charges.HasCharges((uid, charges), prototype.Cost))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-insufficient-ammo-message"), uid, user);

            return false;
        }

        // Exit if the target / target location is obstructed
        var unobstructed = (target == null)
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(gridUid, mapGrid, position), popup: popMsgs)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: popMsgs);

        if (!unobstructed)
            return false;

        // Return whether the operation location is valid
        switch (prototype.Mode)
        {
            case RpdMode.ConstructObject: return IsConstructionLocationValid(uid, component, gridUid, tile, position, user, popMsgs, prototype);
            case RpdMode.Deconstruct: return IsDeconstructionStillValid(uid, tile, target, user, popMsgs);
        }

        return false;
    }

    private bool IsConstructionLocationValid(EntityUid uid, RPDComponent component, EntityUid gridUid, TileRef tile, Vector2i position, EntityUid user, bool popMsgs = true, RPDPrototype? prototype = null)
    {
        prototype ??= component.CachedPrototype;

        // Check rule: Must place on subfloor
        if (prototype.ConstructionRules.Contains(RpdConstructionRule.MustBuildOnSubfloor) && !_turf.GetContentTileDefinition(tile).IsSubFloor)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-must-build-on-subfloor-message"), uid, user);

            return false;
        }

        // Entity specific rules

        // Check rule: The tile is unoccupied
        var isWindow = prototype.ConstructionRules.Contains(RpdConstructionRule.IsWindow);
        var isWall = prototype.ConstructionRules.Contains(RpdConstructionRule.IsWall);

        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(gridUid, position, _intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in _intersectingEntities)
        {
            if (isWindow && HasComp<SharedCanBuildWindowOnTopRPDComponent>(ent))
                continue;

            if (isWall && HasComp<SharedCanBuildWallOnTopRPDComponent>(ent))
                continue;

            if (prototype.CollisionMask != CollisionGroup.None && TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int)prototype.CollisionMask) == 0)
                        continue;

                    // Continue if our custom collision bounds are not intersected
                    if (prototype.CollisionPolygon != null &&
                        !DoesCustomBoundsIntersectWithFixture(prototype.CollisionPolygon, component.ConstructionTransform, ent, fixture))
                        continue;

                    // Collision was detected
                    if (popMsgs)
                        _popup.PopupClient(Loc.GetString("rpd-component-cannot-build-on-occupied-tile-message"), uid, user);

                    return false;
                }
            }
        }

        return true;
    }

    private bool IsDeconstructionStillValid(EntityUid uid, TileRef tile, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Attempt to get, tile or not
        if (target == null)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rcd-component-deconstruct-target-not-on-whitelist-message"), uid, user);

            return false;
        }
        // Attempt to deconstruct an object
        else
        {
            // The object is not in the whitelist
            if (!TryComp<RPDDeconstructableComponent>(target, out var deconstructible) || !deconstructible.Deconstructable)
            {
                if (popMsgs)
                    _popup.PopupClient(Loc.GetString("rpd-component-deconstruct-target-not-on-whitelist-message"), uid, user);

                return false;
            }
        }

        return true;
    }

    #endregion

    #region Entity construction/deconstruction

    private void FinalizeRPDOperation(EntityUid uid, RPDComponent component, EntityUid gridUid, MapGridComponent mapGrid, Vector2i position, Direction direction, EntityUid? target, EntityUid user, RPDPrototype? prototype = null)
    {
        if (!_net.IsServer)
            return;

        prototype ??= component.CachedPrototype;

        if (prototype.Prototype == null)
            return;

        switch (prototype.Mode)
        {
            case RpdMode.ConstructObject:
                var ent = Spawn(prototype.Prototype, _mapSystem.GridTileToLocal(gridUid, mapGrid, position));

                switch (prototype.Rotation)
                {
                    case RpdRotation.Fixed:
                        Transform(ent).LocalRotation = Angle.Zero;
                        break;
                    case RpdRotation.Camera:
                        Transform(ent).LocalRotation = Transform(uid).LocalRotation;
                        break;
                    case RpdRotation.User:
                        Transform(ent).LocalRotation = direction.ToAngle();
                        break;
                }

                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RPD to spawn {ToPrettyString(ent)} at {position} on grid {gridUid}");
                break;

            case RpdMode.Deconstruct:

                if (target != null)
                {
                    // Deconstruct object
                    _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RPD to delete {ToPrettyString(target):target}");
                    QueueDel(target);
                }

                break;
        }
    }

    #endregion

    #region Utility functions

    public bool TryGetMapGridData(EntityCoordinates location, [NotNullWhen(true)] out MapGridData? mapGridData)
    {
        mapGridData = null;
        var gridUid = _transform.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            location = location.AlignWithClosestGridTile(1.75f, EntityManager);
            gridUid = _transform.GetGrid(location);

            // Check if we got a grid ID the second time round
            if (!TryComp(gridUid, out mapGrid))
                return false;
        }

        var tile = _mapSystem.GetTileRef(gridUid!.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid!.Value, mapGrid, location);
        mapGridData = new MapGridData(gridUid!.Value, mapGrid, location, tile, position);

        return true;
    }

    private bool DoesCustomBoundsIntersectWithFixture(PolygonShape boundingPolygon, Transform boundingTransform, EntityUid fixtureOwner, Fixture fixture)
    {
        var entXformComp = Transform(fixtureOwner);
        var entXform = new Transform(new(), entXformComp.LocalRotation);

        return boundingPolygon.ComputeAABB(boundingTransform, 0).Intersects(fixture.Shape.ComputeAABB(entXform, 0));
    }

    public void UpdateCachedPrototype(EntityUid uid, RPDComponent component)
    {
        if (component.ProtoId.Id != component.CachedPrototype?.Prototype)
            component.CachedPrototype = _protoManager.Index(component.ProtoId);
    }

    public void UpdateCachedSecondaryPrototype(EntityUid uid, RPDComponent component)
    {
        if (component.SecondaryProtoId.Id == "Invalid" || !_protoManager.HasIndex(component.SecondaryProtoId))
        {
            component.CachedSecondaryPrototype = null;
            return;
        }

        if (component.SecondaryProtoId.Id != component.CachedSecondaryPrototype?.ID)
            component.CachedSecondaryPrototype = _protoManager.Index(component.SecondaryProtoId);
    }

    #endregion
}

public struct MapGridData
{
    public EntityUid GridUid;
    public MapGridComponent Component;
    public EntityCoordinates Location;
    public TileRef Tile;
    public Vector2i Position;

    public MapGridData(EntityUid gridUid, MapGridComponent component, EntityCoordinates location, TileRef tile, Vector2i position)
    {
        GridUid = gridUid;
        Component = component;
        Location = location;
        Tile = tile;
        Position = position;
    }
}

[Serializable, NetSerializable]
public sealed partial class RPDDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Location { get; private set; } = default!;

    [DataField]
    public Direction Direction { get; private set; } = default!;

    [DataField]
    public ProtoId<RPDPrototype> StartingProtoId { get; private set; } = default!;

    [DataField]
    public int Cost { get; private set; } = 1;

    [DataField("fx")]
    public NetEntity? Effect { get; private set; } = null;

    [DataField]
    public bool Secondary { get; private set; }

    private RPDDoAfterEvent() { }

    public RPDDoAfterEvent(NetCoordinates location, Direction direction, ProtoId<RPDPrototype> startingProtoId, int cost, NetEntity? effect = null, bool secondary = false)
    {
        Location = location;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
        Secondary = secondary;
    }

    public override DoAfterEvent Clone() => this;
}
