using System.Numerics;
using Content.Shared.ADT.Segmented;
using Content.Shared.ADT.Segmented.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Segmented;

public sealed class ADTSegmentedSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    private const float FreeSpotProbeStep = 0.25f;
    private const float GroupJoinFraction = 0.5f;
    private const float MinStepFraction = 0.5f;

    private readonly List<(EntityUid Uid, bool Landed)> _finished = new();

    public EntityUid? ForwardingSegment { get; private set; }

    private EntityUid? _selfDamaging;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTSegmentedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTSegmentedComponent, ComponentShutdown>(OnRootShutdown);
        SubscribeLocalEvent<ADTSegmentedComponent, MoveEvent>(OnRootMove);
        SubscribeLocalEvent<ADTSegmentedComponent, MobStateChangedEvent>(OnRootMobStateChanged);

        SubscribeLocalEvent<ADTSegmentComponent, BeforeDamageChangedEvent>(OnSegmentBeforeDamage);
        SubscribeLocalEvent<ADTSegmentComponent, ComponentShutdown>(OnSegmentShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        _finished.Clear();

        var query = EntityQueryEnumerator<ADTSegmentSteppingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stepping, out var xform))
        {
            if (xform.ParentUid != stepping.Parent)
            {
                _finished.Add((uid, false));
                continue;
            }

            var duration = (stepping.End - stepping.Start).TotalSeconds;
            var progress = 1f;

            if (duration > 0)
                progress = (float) Math.Clamp((now - stepping.Start).TotalSeconds / duration, 0, 1);

            var eased = progress * progress * (3f - 2f * progress);
            _transform.SetLocalPosition(uid, Vector2.Lerp(stepping.From, stepping.To, eased), xform);

            if (progress >= 1f)
                _finished.Add((uid, true));
        }

        foreach (var (uid, landed) in _finished)
        {
            FinishStep(uid, landed);
        }
    }

    private void OnMapInit(Entity<ADTSegmentedComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (xform.MapUid == null)
            return;

        EntityUid? chainLeader = null;

        for (var i = 0; i < ent.Comp.Segments.Count; i++)
        {
            var entry = ent.Comp.Segments[i];
            var segment = Spawn(entry.Proto, xform.Coordinates);
            var comp = EnsureComp<ADTSegmentComponent>(segment);

            comp.Root = ent.Owner;
            comp.Index = i;
            comp.Leader = ent.Owner;

            if (entry.Mode == ADTSegmentMode.Chain)
            {
                comp.Leader = chainLeader ?? ent.Owner;
                chainLeader = segment;
            }

            Dirty(segment, comp);
            ent.Comp.Spawned.Add(segment);
        }

        SnapAll(ent, xform);

        var ev = new ADTSegmentsSpawnedEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnRootShutdown(Entity<ADTSegmentedComponent> ent, ref ComponentShutdown args)
    {
        DeleteSegments(ent);
    }

    private void OnRootMobStateChanged(Entity<ADTSegmentedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !ent.Comp.DeleteOnDeath)
            return;

        DeleteSegments(ent);
    }

    private void OnRootMove(Entity<ADTSegmentedComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.Spawned.Count == 0 || TerminatingOrDeleted(ent))
            return;

        var xform = args.Component;

        if (args.ParentChanged)
        {
            SnapAll(ent, xform);
            return;
        }

        var delta = args.NewPosition.Position - args.OldPosition.Position;
        var distance = delta.Length();

        if (distance > ent.Comp.TeleportDistance)
        {
            SnapAll(ent, xform);
            return;
        }

        if (distance > 0.0001f)
        {
            ent.Comp.MoveDirection = delta / distance;
            ent.Comp.AnchorOverrides.Clear();
        }

        UpdateFixed(ent, xform);
        UpdateChain(ent, xform);
        UpdateSteps(ent, xform);
    }

    private void OnSegmentBeforeDamage(Entity<ADTSegmentComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || _selfDamaging == ent.Owner)
            return;

        if (!args.Damage.AnyPositive())
        {
            if (!ent.Comp.TakeDamage)
                args.Cancelled = true;

            return;
        }

        if (ent.Comp.Root is not { } root || TerminatingOrDeleted(root))
            return;

        args.Cancelled = true;

        var ev = new ADTSegmentDamageForwardEvent(root, args.Damage, ent.Comp.DamageModifier, args.Origin);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled || ev.Modifier <= 0f)
            return;

        var forwarded = ev.Damage * ev.Modifier;
        ForwardingSegment = ent.Owner;

        try
        {
            _damageable.TryChangeDamage(root, forwarded, origin: args.Origin);
        }
        finally
        {
            ForwardingSegment = null;
        }

        if (!ent.Comp.TakeDamage || TerminatingOrDeleted(ent))
            return;

        _selfDamaging = ent.Owner;

        try
        {
            _damageable.TryChangeDamage(ent.Owner, forwarded, origin: args.Origin);
        }
        finally
        {
            _selfDamaging = null;
        }
    }

    private void OnSegmentShutdown(Entity<ADTSegmentComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Root is not { } root || !TryComp<ADTSegmentedComponent>(root, out var segmented))
            return;

        var index = ent.Comp.Index;

        if (index < 0 || index >= segmented.Spawned.Count || segmented.Spawned[index] != ent.Owner)
            return;

        segmented.Spawned[index] = null;
    }

    public void DeleteSegments(Entity<ADTSegmentedComponent> ent)
    {
        foreach (var segment in ent.Comp.Spawned)
        {
            if (segment is { } uid && !TerminatingOrDeleted(uid))
                QueueDel(uid);
        }

        ent.Comp.Spawned.Clear();
    }

    public void SnapAll(Entity<ADTSegmentedComponent> ent, TransformComponent? xform = null)
    {
        if (!Resolve(ent, ref xform) || !xform.ParentUid.IsValid() || TerminatingOrDeleted(xform.ParentUid))
            return;

        ent.Comp.AnchorOverrides.Clear();

        var leader = ent.Owner;
        var leaderPos = xform.LocalPosition;

        for (var i = 0; i < ent.Comp.Spawned.Count; i++)
        {
            var entry = ent.Comp.Segments[i];

            if (!TryGetSegment(ent, i, out var segment, out var segmentXform))
                continue;

            StopStep(segment);

            switch (entry.Mode)
            {
                case ADTSegmentMode.Step:
                    Place(segment, segmentXform, xform, FindFreeSpot(xform, GetAnchor(ent, i, xform)), segmentXform.LocalRotation);
                    break;
                case ADTSegmentMode.Chain:
                    var position = leaderPos - ent.Comp.MoveDirection * entry.Spacing;
                    Place(segment, segmentXform, xform, position, ent.Comp.MoveDirection.ToWorldAngle());
                    SetLeader(segment, leader);
                    leader = segment;
                    leaderPos = position;
                    break;
                case ADTSegmentMode.Fixed:
                    var rotation = entry.RotateWithRoot ? xform.LocalRotation : segmentXform.LocalRotation;
                    Place(segment, segmentXform, xform, GetAnchor(ent, i, xform), rotation);
                    break;
            }
        }
    }

    private void UpdateFixed(Entity<ADTSegmentedComponent> ent, TransformComponent xform)
    {
        for (var i = 0; i < ent.Comp.Spawned.Count; i++)
        {
            var entry = ent.Comp.Segments[i];

            if (entry.Mode != ADTSegmentMode.Fixed || !TryGetSegment(ent, i, out var segment, out var segmentXform))
                continue;

            var rotation = entry.RotateWithRoot ? xform.LocalRotation : segmentXform.LocalRotation;
            Place(segment, segmentXform, xform, GetAnchor(ent, i, xform), rotation);
        }
    }

    private void UpdateChain(Entity<ADTSegmentedComponent> ent, TransformComponent xform)
    {
        var leader = ent.Owner;
        var leaderPos = xform.LocalPosition;

        for (var i = 0; i < ent.Comp.Spawned.Count; i++)
        {
            var entry = ent.Comp.Segments[i];

            if (entry.Mode != ADTSegmentMode.Chain || !TryGetSegment(ent, i, out var segment, out var segmentXform))
                continue;

            SetLeader(segment, leader);

            if (segmentXform.ParentUid != xform.ParentUid)
            {
                var fallback = leaderPos - ent.Comp.MoveDirection * entry.Spacing;
                Place(segment, segmentXform, xform, fallback, ent.Comp.MoveDirection.ToWorldAngle());

                leader = segment;
                leaderPos = fallback;
                continue;
            }

            var position = segmentXform.LocalPosition;
            var toLeader = leaderPos - position;
            var distance = toLeader.Length();

            if (distance > entry.Spacing)
            {
                var direction = toLeader / distance;
                position = leaderPos - direction * entry.Spacing;
                _transform.SetLocalPositionRotation(segment, position, direction.ToWorldAngle(), segmentXform);
            }

            leader = segment;
            leaderPos = position;
        }
    }

    private void UpdateSteps(Entity<ADTSegmentedComponent> ent, TransformComponent xform)
    {
        var activeGroup = GetActiveGroup(ent);
        int? bestGroup = null;
        var bestDistance = 0f;

        for (var i = 0; i < ent.Comp.Spawned.Count; i++)
        {
            var entry = ent.Comp.Segments[i];

            if (entry.Mode != ADTSegmentMode.Step || !TryGetSegment(ent, i, out var segment, out var segmentXform))
                continue;

            if (HasComp<ADTSegmentSteppingComponent>(segment))
                continue;

            var anchor = GetAnchor(ent, i, xform);
            var distance = (segmentXform.LocalPosition - anchor).Length();

            if (segmentXform.ParentUid != xform.ParentUid)
            {
                Place(segment, segmentXform, xform, FindFreeSpot(xform, anchor), segmentXform.LocalRotation);
                continue;
            }

            if (distance > entry.LeashDistance)
            {
                var reachable = FindFreeSpot(xform, anchor);

                if ((segmentXform.LocalPosition - reachable).Length() > entry.LeashDistance)
                {
                    Place(segment, segmentXform, xform, reachable, segmentXform.LocalRotation);
                    continue;
                }
            }

            if (activeGroup != null)
                continue;

            if (distance <= entry.StepThreshold || distance <= bestDistance)
                continue;

            if (!CanStep(ent, entry, segmentXform, xform, anchor, out _))
                continue;

            bestGroup = entry.Group;
            bestDistance = distance;
        }

        if (bestGroup is not { } group)
            return;

        for (var i = 0; i < ent.Comp.Spawned.Count; i++)
        {
            var entry = ent.Comp.Segments[i];

            if (entry.Mode != ADTSegmentMode.Step || entry.Group != group)
                continue;

            if (!TryGetSegment(ent, i, out var segment, out var segmentXform) || segmentXform.ParentUid != xform.ParentUid)
                continue;

            if (HasComp<ADTSegmentSteppingComponent>(segment))
                continue;

            var anchor = GetAnchor(ent, i, xform);

            if ((segmentXform.LocalPosition - anchor).Length() <= entry.StepThreshold * GroupJoinFraction)
                continue;

            if (!CanStep(ent, entry, segmentXform, xform, anchor, out var target))
                continue;

            StartStep(ent, entry, segment, segmentXform, xform, target);
        }
    }

    private bool CanStep(
        Entity<ADTSegmentedComponent> ent,
        ADTSegmentEntry entry,
        TransformComponent segmentXform,
        TransformComponent xform,
        Vector2 anchor,
        out Vector2 target)
    {
        target = FindFreeSpot(xform, anchor + ent.Comp.MoveDirection * entry.StepThreshold * entry.StepLead);
        return (target - segmentXform.LocalPosition).Length() >= entry.StepThreshold * MinStepFraction;
    }

    private void StartStep(
        Entity<ADTSegmentedComponent> ent,
        ADTSegmentEntry entry,
        EntityUid segment,
        TransformComponent segmentXform,
        TransformComponent xform,
        Vector2 target)
    {
        var now = _timing.CurTime;

        var stepping = EnsureComp<ADTSegmentSteppingComponent>(segment);
        stepping.Parent = xform.ParentUid;
        stepping.From = segmentXform.LocalPosition;
        stepping.To = target;
        stepping.Group = entry.Group;
        stepping.Start = now;
        stepping.End = now + entry.StepDuration;

        if (TryComp<ADTSegmentComponent>(segment, out var comp))
        {
            comp.StepStart = stepping.Start;
            comp.StepEnd = stepping.End;
            comp.StepHeight = entry.StepHeight;
            Dirty(segment, comp);
        }

        var ev = new ADTSegmentStepStartedEvent(ent.Owner);
        RaiseLocalEvent(segment, ref ev);
    }

    private void FinishStep(EntityUid uid, bool landed)
    {
        RemComp<ADTSegmentSteppingComponent>(uid);

        if (!landed || TerminatingOrDeleted(uid))
            return;

        if (!TryComp<ADTSegmentComponent>(uid, out var segment) || segment.Root is not { } root || TerminatingOrDeleted(root))
            return;

        var ev = new ADTSegmentLandedEvent(root);
        RaiseLocalEvent(uid, ref ev);

        var rootEv = new ADTRootSegmentLandedEvent(uid);
        RaiseLocalEvent(root, ref rootEv);

        if (TerminatingOrDeleted(root) || !TryComp<ADTSegmentedComponent>(root, out var segmented))
            return;

        UpdateSteps((root, segmented), Transform(root));
    }

    private void StopStep(EntityUid segment)
    {
        if (!RemComp<ADTSegmentSteppingComponent>(segment))
            return;

        if (!TryComp<ADTSegmentComponent>(segment, out var comp))
            return;

        comp.StepEnd = _timing.CurTime;
        Dirty(segment, comp);
    }

    private int? GetActiveGroup(Entity<ADTSegmentedComponent> ent)
    {
        foreach (var segment in ent.Comp.Spawned)
        {
            if (segment is { } uid && TryComp<ADTSegmentSteppingComponent>(uid, out var stepping))
                return stepping.Group;
        }

        return null;
    }

    private bool TryGetSegment(Entity<ADTSegmentedComponent> ent, int index, out EntityUid segment, out TransformComponent segmentXform)
    {
        segment = default;
        segmentXform = default!;

        if (ent.Comp.Spawned[index] is not { } uid || TerminatingOrDeleted(uid))
            return false;

        segment = uid;
        segmentXform = Transform(uid);
        return true;
    }

    private void SetLeader(EntityUid segment, EntityUid leader)
    {
        if (!TryComp<ADTSegmentComponent>(segment, out var comp) || comp.Leader == leader)
            return;

        comp.Leader = leader;
        Dirty(segment, comp);
    }

    private void Place(EntityUid segment, TransformComponent segmentXform, TransformComponent rootXform, Vector2 position, Angle rotation)
    {
        if (segmentXform.ParentUid != rootXform.ParentUid)
        {
            _transform.SetCoordinates((segment, segmentXform, MetaData(segment)), new EntityCoordinates(rootXform.ParentUid, position), rotation);
            return;
        }

        _transform.SetLocalPositionRotation(segment, position, rotation, segmentXform);
    }

    private Vector2 FindFreeSpot(TransformComponent rootXform, Vector2 target)
    {
        if (rootXform.GridUid is not { } grid || rootXform.ParentUid != grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return target;

        var origin = rootXform.LocalPosition;
        var toTarget = target - origin;
        var length = toTarget.Length();

        if (length < 0.0001f)
            return target;

        var steps = (int) MathF.Ceiling(length / FreeSpotProbeStep);
        var lastFree = origin;
        var lastIndices = _map.TileIndicesFor(grid, gridComp, new EntityCoordinates(grid, origin));

        for (var i = 1; i <= steps; i++)
        {
            var point = Vector2.Lerp(origin, target, Math.Min(1f, i / (float) steps));
            var indices = _map.TileIndicesFor(grid, gridComp, new EntityCoordinates(grid, point));

            if (indices != lastIndices)
            {
                if (_turf.IsTileBlocked(grid, indices, CollisionGroup.Impassable, gridComp))
                    return lastFree;

                lastIndices = indices;
            }

            lastFree = point;
        }

        return target;
    }

    public void SetAnchorOverride(Entity<ADTSegmentedComponent> ent, int index, Vector2 offset)
    {
        if (index < 0 || index >= ent.Comp.Segments.Count || index >= ent.Comp.Spawned.Count)
            return;

        ent.Comp.AnchorOverrides[index] = offset;

        var entry = ent.Comp.Segments[index];

        if (entry.Mode != ADTSegmentMode.Step || !TryGetSegment(ent, index, out var segment, out var segmentXform))
            return;

        var xform = Transform(ent);

        if (segmentXform.ParentUid != xform.ParentUid || HasComp<ADTSegmentSteppingComponent>(segment))
            return;

        var target = FindFreeSpot(xform, GetAnchor(ent, index, xform));

        if ((target - segmentXform.LocalPosition).Length() < entry.StepThreshold * GroupJoinFraction)
            return;

        StartStep(ent, entry, segment, segmentXform, xform, target);
    }

    private static Vector2 GetAnchor(Entity<ADTSegmentedComponent> ent, int index, TransformComponent rootXform)
    {
        var entry = ent.Comp.Segments[index];

        if (!ent.Comp.AnchorOverrides.TryGetValue(index, out var offset))
            offset = entry.Offset;

        if (entry.RotateWithRoot)
            offset = rootXform.LocalRotation.RotateVec(offset);

        return rootXform.LocalPosition + offset;
    }
}
