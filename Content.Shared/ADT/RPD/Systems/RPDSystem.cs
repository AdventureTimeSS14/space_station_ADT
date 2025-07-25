using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.ADT.Construction;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.ADT.RPD.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;


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

        // Set the current RPD prototype to the one supplied
        component.ProtoId = args.ProtoId;
        UpdateCachedPrototype(uid, component);
        Dirty(uid, component);
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

        args.PushMarkup(msg);
    }

    private void OnAfterInteract(EntityUid uid, RPDComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        var user = args.User;
        var location = args.ClickLocation;

        // Initial validity checks
        if (!location.IsValid(EntityManager))
            return;

        if (!TryGetMapGridData(location, out var mapGridData))
        {
            _popup.PopupClient(Loc.GetString("rpd-component-no-valid-grid"), uid, user);
            return;
        }

        if (!IsRPDOperationStillValid(uid, component, mapGridData.Value, args.Target, args.User))
            return;

        if (!_net.IsServer)
            return;

        // Get the starting cost, delay, and effect from the prototype
        var cost = component.CachedPrototype.Cost;
        var delay = component.CachedPrototype.Delay;
        var effectPrototype = component.CachedPrototype.Effect;

        #region: Operation modifiers

        // Deconstruction modifiers
        switch (component.CachedPrototype.Mode)
        {
            case RpdMode.Deconstruct:

                // Deconstructing an object
                if (args.Target != null)
                {
                    if (TryComp<RPDDeconstructableComponent>(args.Target, out var destructible))
                    {
                        cost = destructible.Cost;
                        delay = destructible.Delay;
                        effectPrototype = destructible.Effect;
                    }
                }
                break;
        }

        #endregion

        // Try to start the do after
        var effect = Spawn(effectPrototype, mapGridData.Value.Location);
        var ev = new RPDDoAfterEvent(GetNetCoordinates(mapGridData.Value.Location), component.ConstructionDirection, component.ProtoId, cost, EntityManager.GetNetEntity(effect));

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay, ev, uid, target: args.Target, used: uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        args.Handled = true;

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            QueueDel(effect);
    }

    private void OnDoAfterAttempt(EntityUid uid, RPDComponent component, DoAfterAttemptEvent<RPDDoAfterEvent> args)
    {
        if (args.Event?.DoAfter?.Args == null)
            return;

        // Exit if the RPD prototype has changed
        if (component.ProtoId != args.Event.StartingProtoId)
        {
            args.Cancel();
            return;
        }

        // Ensure the RPD operation is still valid
        var location = GetCoordinates(args.Event.Location);

        if (!TryGetMapGridData(location, out var mapGridData))
        {
            args.Cancel();
            return;
        }

        if (!IsRPDOperationStillValid(uid, component, mapGridData.Value, args.Event.Target, args.Event.User))
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, RPDComponent component, RPDDoAfterEvent args)
    {
        if (args.Cancelled && _net.IsServer)
            QueueDel(EntityManager.GetEntity(args.Effect));

        if (args.Handled || args.Cancelled || !_timing.IsFirstTimePredicted)
            return;

        args.Handled = true;

        var location = GetCoordinates(args.Location);

        if (!TryGetMapGridData(location, out var mapGridData))
            return;

        // Ensure the RPD operation is still valid
        if (!IsRPDOperationStillValid(uid, component, mapGridData.Value, args.Target, args.User))
            return;

        // Finalize the operation
        FinalizeRPDOperation(uid, component, mapGridData.Value, args.Direction, args.Target, args.User);

        // Play audio and consume charges
        _audio.PlayPredicted(component.SuccessSound, uid, args.User);
        _charges.TryUseCharges(uid, args.Cost);
    }

    private void OnRPDconstructionGhostRotationEvent(RPDConstructionGhostRotationEvent ev, EntitySessionEventArgs session)
    {
        var uid = GetEntity(ev.NetEntity);

        // Determine if player that send the message is carrying the specified RPD in their active hand
        if (session.SenderSession.AttachedEntity == null)
            return;

        if (!TryComp<HandsComponent>(session.SenderSession.AttachedEntity, out var hands) ||
            uid != hands.ActiveHand?.HeldEntity)
            return;

        if (!TryComp<RPDComponent>(uid, out var rpd))
            return;

        // Update the construction direction
        rpd.ConstructionDirection = ev.Direction;
        Dirty(uid, rpd);
    }

    #endregion

    #region Entity construction/deconstruction rule checks

    public bool IsRPDOperationStillValid(EntityUid uid, RPDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool popMsgs = true)
    {
        // Update cached prototype if required
        UpdateCachedPrototype(uid, component);

        // Check that the RPD has enough ammo to get the job done
        TryComp<LimitedChargesComponent>(uid, out var charges);

        if (_charges.IsEmpty((uid, charges)))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-no-ammo-message"), uid, user);

            return false;
        }

        if (!_charges.HasCharges((uid, charges), component.CachedPrototype.Cost))
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-insufficient-ammo-message"), uid, user);

            return false;
        }

        // Exit if the target / target location is obstructed
        var unobstructed = (target == null)
            ? _interaction.InRangeUnobstructed(user, _mapSystem.GridTileToWorld(mapGridData.GridUid, mapGridData.Component, mapGridData.Position), popup: popMsgs)
            : _interaction.InRangeUnobstructed(user, target.Value, popup: popMsgs);

        if (!unobstructed)
            return false;

        // Return whether the operation location is valid
        switch (component.CachedPrototype.Mode)
        {
            case RpdMode.ConstructObject: return IsConstructionLocationValid(uid, component, mapGridData, user, popMsgs);
            case RpdMode.Deconstruct: return IsDeconstructionStillValid(uid, component, mapGridData, target, user, popMsgs);
        }

        return false;
    }

    private bool IsConstructionLocationValid(EntityUid uid, RPDComponent component, MapGridData mapGridData, EntityUid user, bool popMsgs = true)
    {
        // Check rule: Must place on subfloor
        if (component.CachedPrototype.ConstructionRules.Contains(RpdConstructionRule.MustBuildOnSubfloor) && !mapGridData.Tile.Tile.GetContentTileDefinition().IsSubFloor)
        {
            if (popMsgs)
                _popup.PopupClient(Loc.GetString("rpd-component-must-build-on-subfloor-message"), uid, user);

            return false;
        }

        // Entity specific rules

        // Check rule: The tile is unoccupied
        var isWindow = component.CachedPrototype.ConstructionRules.Contains(RpdConstructionRule.IsWindow);
        var isWall = component.CachedPrototype.ConstructionRules.Contains(RpdConstructionRule.IsWall);

        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(mapGridData.GridUid, mapGridData.Position, _intersectingEntities, -0.05f, LookupFlags.Uncontained);

        foreach (var ent in _intersectingEntities)
        {
            if (isWindow && HasComp<SharedCanBuildWindowOnTopRPDComponent>(ent))
                continue;

            if (isWall && HasComp<SharedCanBuildWallOnTopRPDComponent>(ent))
                continue;

            if (component.CachedPrototype.CollisionMask != CollisionGroup.None && TryComp<FixturesComponent>(ent, out var fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    // Continue if no collision is possible
                    if (!fixture.Hard || fixture.CollisionLayer <= 0 || (fixture.CollisionLayer & (int)component.CachedPrototype.CollisionMask) == 0)
                        continue;

                    // Continue if our custom collision bounds are not intersected
                    if (component.CachedPrototype.CollisionPolygon != null &&
                        !DoesCustomBoundsIntersectWithFixture(component.CachedPrototype.CollisionPolygon, component.ConstructionTransform, ent, fixture))
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

    private bool IsDeconstructionStillValid(EntityUid uid, RPDComponent component, MapGridData mapGridData, EntityUid? target, EntityUid user, bool popMsgs = true)
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

    private void FinalizeRPDOperation(EntityUid uid, RPDComponent component, MapGridData mapGridData, Direction direction, EntityUid? target, EntityUid user)
    {
        if (!_net.IsServer)
            return;

        if (component.CachedPrototype.Prototype == null)
            return;

        switch (component.CachedPrototype.Mode)
        {
            case RpdMode.ConstructObject:
                var ent = Spawn(component.CachedPrototype.Prototype, _mapSystem.GridTileToLocal(mapGridData.GridUid, mapGridData.Component, mapGridData.Position));

                switch (component.CachedPrototype.Rotation)
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

                _adminLogger.Add(LogType.RCD, LogImpact.High, $"{ToPrettyString(user):user} used RPD to spawn {ToPrettyString(ent)} at {mapGridData.Position} on grid {mapGridData.GridUid}");
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
        var gridUid = location.GetGridUid(EntityManager);

        if (!TryComp<MapGridComponent>(gridUid, out var mapGrid))
        {
            location = location.AlignWithClosestGridTile(1.75f, EntityManager);
            gridUid = location.GetGridUid(EntityManager);

            // Check if we got a grid ID the second time round
            if (!TryComp(gridUid, out mapGrid))
                return false;
        }

        gridUid = mapGrid.Owner;

        var tile = _mapSystem.GetTileRef(gridUid.Value, mapGrid, location);
        var position = _mapSystem.TileIndicesFor(gridUid.Value, mapGrid, location);
        mapGridData = new MapGridData(gridUid.Value, mapGrid, location, tile, position);

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

    private RPDDoAfterEvent() { }

    public RPDDoAfterEvent(NetCoordinates location, Direction direction, ProtoId<RPDPrototype> startingProtoId, int cost, NetEntity? effect = null)
    {
        Location = location;
        Direction = direction;
        StartingProtoId = startingProtoId;
        Cost = cost;
        Effect = effect;
    }

    public override DoAfterEvent Clone() => this;
}
