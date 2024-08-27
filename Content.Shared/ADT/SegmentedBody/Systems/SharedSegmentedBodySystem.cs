using System.Diagnostics;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.ADT.SegmentedBody;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.SegmentedBody;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed class SharedSegmentedBodySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _modifierSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly HeldSpeedModifierSystem _clothingMoveSpeed = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<SegmentedBodyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SegmentedBodyPartComponent, DamageChangedEvent>(OnPartDamage);
    }

    private void OnMapInit(EntityUid uid, SegmentedBodyComponent comp, MapInitEvent args)
    {
        if (comp.SegmentsCount <= 0 ||
            //_proto.TryIndex(comp.MiddleSegmentPrototype, out var midSegment) ||
            comp.Segments.Count > 0)
            return;

        for (var i = 0; i < comp.SegmentsCount; i++)
        {
            var segment = Spawn(comp.MiddleSegmentPrototype, Transform(uid).Coordinates);
            if (!TryComp<SegmentedBodyPartComponent>(segment, out var segmentComp))
                break;
            var jointId = $"segmented-body-joint-{GetNetEntity(segment)}-{GetNetEntity(uid)}";
            var parent = uid;
            if (comp.Segments.Count == 0)
            {
                jointId = $"segmented-body-joint-{GetNetEntity(segment)}-{GetNetEntity(uid)}";
                parent = uid;
            }
            else
            {
                jointId = $"segmented-body-joint-{GetNetEntity(segment)}-{GetNetEntity(comp.Segments[i - 1])}";
                parent = comp.Segments[i - 1];
            }

            if (i == comp.SegmentsCount - 1 &&
                comp.EndSegmentPrototype != null)
                //_proto.TryIndex(comp.EndSegmentPrototype, out var endSegment))
            {
                QueueDel(segment);
                segment = Spawn(comp.EndSegmentPrototype, Transform(uid).Coordinates);
            }

            segmentComp.Body = uid;
            segmentComp.ParentJointId = jointId;
            comp.Segments.Add(segment);
            if (!_timing.ApplyingState)
            {
                var union = _physics.GetHardAABB(parent).Union(_physics.GetHardAABB(segment));
                var length = Math.Max(union.Size.X, union.Size.Y) * 0.75f;

                var joint = _joints.CreateDistanceJoint(segment, parent, id: jointId);
                joint.CollideConnected = false;

                joint.MaxLength = Math.Max(1.0f, length);
                joint.Length = length * 0.75f;
                joint.MinLength = 0f;
                joint.Stiffness = 1f;
            }
            Dirty(segment, segmentComp);
        }
        Dirty(uid, comp);
    }

    private void OnPartDamage(EntityUid uid, SegmentedBodyPartComponent comp, DamageChangedEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
        {
            return;
        }

        if (comp.ShareDamage)
        {
            if (args.DamageDelta != null)
                _damage.TryChangeDamage(comp.Body, args.DamageDelta);
        }
        if (damageable.Damage.GetTotal() >= comp.DetachDamage)
        {
            if (!TryComp<SegmentedBodyComponent>(comp.Body, out var segmented) ||
                segmented.Segments.Contains(uid))
                return;

            var index = segmented.Segments.IndexOf(uid);
            for (var i = index; i < segmented.Segments.Count; i++)
            {
                var item = segmented.Segments[i];
                if (!TryComp<SegmentedBodyPartComponent>(item, out var segment))
                    continue;
                if (segment.ParentJointId != null)
                {
                    _joints.RemoveJoint(item, segment.ParentJointId);
                }
            }
        }
    }
}
