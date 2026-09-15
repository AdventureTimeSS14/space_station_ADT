//

using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Teleportation.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class LockPortalSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _look = default!;

    [Dependency] private readonly EntityQuery<FirelockComponent> _firelockQuery = default!;
    [Dependency] private readonly EntityQuery<LockTrappedDoorComponent> _trapQuery = default!;

    private readonly List<Entity<DoorComponent, TransformComponent>> _possibleDestinations = new();
    private readonly HashSet<Entity<PhysicsComponent>> _intersecting = new();
    private readonly HashSet<Entity<LockPortalComponent>> _portals = new();

    public const int LockPortalMask = (int) CollisionGroup.InteractImpassable;
    public const int BlockerTeleportMask = (int) CollisionGroup.Impassable;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LockPortalComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<LockPortalComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<LockPortalComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<LockPortalComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<LockPortalComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;

        var status = ent.Comp.Inverted
            ? Loc.GetString("lock-portal-component-examine-inverted")
            : Loc.GetString("lock-portal-component-examine-not-inverted");
        args.PushMarkup(Loc.GetString("lock-portal-component-examine-message", ("status", status)));
    }

    private void OnGetVerbs(Entity<LockPortalComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!_heretic.IsHereticOrGhoul(args.User))
            return;

        if (!HasComp<EldritchIdCardComponent>(args.Using))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("lock-portal-component-clear-portals"),
            Icon = new SpriteSpecifier.Rsi(new ResPath("ADT/Heretic/Effects/effects.rsi"), "realitycrack"),
            Act = () => ClearPortals(ent),
        };

        args.Verbs.Add(verb);
    }

    private void ClearPortals(Entity<LockPortalComponent> ent)
    {
        PredictedQueueDel(ent.Comp.LinkedPortal);
        PredictedQueueDel(ent);
    }

    private void OnEndCollide(Entity<LockPortalComponent> ent, ref EndCollideEvent args)
    {
        var subject = args.OtherEntity;

        if (!ShouldCollide(subject))
            return;

        if (TryComp<PortalTimeoutComponent>(subject, out var timeout) && timeout.EnteredPortal != ent)
            RemCompDeferred<PortalTimeoutComponent>(subject);
    }

    private void OnCollide(Entity<LockPortalComponent> ent, ref StartCollideEvent args)
    {
        var subject = args.OtherEntity;

        if (!ShouldCollide(subject))
            return;

        if (HasComp<PortalTimeoutComponent>(subject))
            return;

        var ev = new TeleportAttemptEvent();
        RaiseLocalEvent(subject, ref ev);
        if (ev.Cancelled)
            return;

        var linkResolved = Exists(ent.Comp.LinkedPortal);
        var invertedBehavior = !linkResolved || _heretic.IsHereticOrGhoul(subject) == ent.Comp.Inverted;

        if (invertedBehavior)
        {
            if (_net.IsClient)
                return;

            var parent = Transform(ent).ParentUid;

            if (HasComp<DoorComponent>(parent) && FindRandomDoor(parent) is { } destination)
                Teleport(subject, ent, destination.AsNullable(), false);
            return;
        }

        var link = ent.Comp.LinkedPortal!.Value;
        var linkParent = Transform(link).ParentUid;

        Teleport(subject, ent, linkParent, true);
    }

    private void Teleport(EntityUid uid,
        Entity<LockPortalComponent> portal,
        Entity<DoorComponent?, TransformComponent?, DoorBoltComponent?> destination,
        bool addTimeout)
    {
        if (!Resolve(destination, ref destination.Comp1, ref destination.Comp2, false))
            return;

        var to = destination.Comp2.Coordinates;

        EntityUid? pulling = null;
        if (TryComp(uid, out PullerComponent? puller) && puller.Pulling != null)
            pulling = puller.Pulling.Value;

        if (Resolve(destination, ref destination.Comp3, false))
            _door.SetBoltsDown((destination, destination.Comp3), false);

        _door.StartOpening(destination, destination.Comp1);

        _intersecting.Clear();
        _look.GetEntitiesInRange(to, 0.1f, _intersecting, LookupFlags.Static);
        foreach (var (blocker, body) in _intersecting)
        {
            if (blocker == destination.Owner)
                continue;

            if ((body.CollisionLayer & BlockerTeleportMask) == 0)
                continue;

            PredictedQueueDel(blocker);
        }

        if (addTimeout)
        {
            var timeout = EnsureComp<PortalTimeoutComponent>(uid);
            timeout.EnteredPortal = portal.Owner;
            Dirty(uid, timeout);
        }

        _audio.PlayPredicted(portal.Comp.DepartureSound, Transform(uid).Coordinates, uid);
        _transform.SetCoordinates(uid, to);
        _audio.PlayPredicted(portal.Comp.ArrivalSound, to, uid);

        if (pulling == null)
            return;

        if (addTimeout)
        {
            var timeout2 = EnsureComp<PortalTimeoutComponent>(pulling.Value);
            timeout2.EnteredPortal = portal.Owner;
            Dirty(pulling.Value, timeout2);
        }

        _transform.SetCoordinates(pulling.Value, to);
        if (TryComp(uid, out PullerComponent? pullerComp))
            _pulling.TryStartPull(uid, pulling.Value, pullerComp, force: true);
    }

    private bool ShouldCollide(EntityUid uid)
    {
        return HasComp<MobStateComponent>(uid);
    }

    private Entity<DoorComponent, TransformComponent>? FindRandomDoor(EntityUid ourAirlock)
    {
        var ourXform = Transform(ourAirlock);

        if (ourXform.GridUid == null)
            return null;

        var query = EntityQueryEnumerator<DoorComponent, PhysicsComponent, TransformComponent>();
        _possibleDestinations.Clear();
        while (query.MoveNext(out var uid, out var door, out var body, out var xform))
        {
            if (IsDoorValid((uid, door, body)) || uid == ourAirlock ||
                xform.MapID != ourXform.MapID ||
                xform.GridUid != ourXform.GridUid)
                continue;

            _possibleDestinations.Add((uid, door, xform));
        }

        return _possibleDestinations.Count == 0 ? null : _random.Pick(_possibleDestinations);
    }

    public bool IsDoorValid(Entity<DoorComponent?, PhysicsComponent?> door)
    {
        if (!Resolve(door, ref door.Comp1, ref door.Comp2, false))
            return false;

        return door.Comp1 is not { BumpOpen: false, ClickOpen: false } &&
               (door.Comp2.CollisionLayer & LockPortalMask) != 0 &&
               !_firelockQuery.HasComp(door) && !_trapQuery.HasComp(door);
    }

    public bool IsDoorOccupied(EntityUid door, EntityUid? user)
    {
        _portals.Clear();
        var coords = Transform(door).Coordinates;
        _look.GetEntitiesInRange(coords, 0.4f, _portals, LookupFlags.StaticSundries | LookupFlags.Sensors);

        var result = _portals.Count > 0;
        if (result && user is { } u)
            _popup.PopupEntity(Loc.GetString("heretic-ability-fail-tile-occupied"), u, u);

        return result;
    }
}
