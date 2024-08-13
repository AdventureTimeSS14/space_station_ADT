using Content.Shared.Mind;
using Robust.Shared.Map;
using Content.Shared.Alert;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using System.Numerics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Movement.Events;
using Content.Shared.Mobs;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;

namespace Content.Shared.ADT.Controlled;

public sealed partial class SharedControlledSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ControlledObserverComponent, UpdateCanMoveEvent>(OnTryInteract);
        SubscribeLocalEvent<ControlledObserverComponent, InteractionAttemptEvent>(OnInteractionAttempt);

        SubscribeLocalEvent<ControlledComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<ControlledComponent, MobStateChangedEvent>(OnMobState);
    }

    /// <summary>
    ///     Attempts to overtake control of the target
    /// </summary>
    /// <param name="uid">The entity to get control.</param>
    /// <param name="target">The entity that to be controlled.</param>
    /// <param name="duration">Controlling duration (in seconds).</param>
    /// <param name="priority">Control priority. If higher than active one, will steal stolen control (press F for that one controlled).</param>
    /// <returns>If the entity could be controlled.</returns>
    public bool TryStartControlling(EntityUid uid, EntityUid target, float duration, int priority = 1, string key = "Default")
    {
        if (Deleted(target) || Deleted(uid))
            return false;
        if (duration <= 0)
            return false;
        if (TryComp<ControlledComponent>(target, out var comp))
        {
            if (comp.Priority >= priority)
                return false;
            StopControlling(target, comp);
            StartControlling(uid, target, duration, priority);
            return true;
        }
        else
        {
            StartControlling(uid, target, duration, priority, key);
            return true;
        }
    }

    /// <summary>
    ///     Overtaking control of the target and sets params.
    /// </summary>
    /// <param name="uid">The entity to get control.</param>
    /// <param name="target">The entity that to be controlled.</param>
    /// <param name="duration">Controlling duration (in seconds).</param>
    /// <param name="priority">Control priority. If higher than in exsisting ControlledComponent, will steal stolen control. bruh.</param>
    /// <param name="key">Control keyword for specific controllers.</param>
    private void StartControlling(EntityUid uid, EntityUid target, float duration, int priority, string key = "Default")
    {
        if (!_mind.TryGetMind(uid, out var uidMindId, out var uidMind))
            return;
        var observer = Spawn("PseudoEntity", Transform(target).Coordinates);
        if (_mind.TryGetMind(target, out var targetMindId, out var targetMind))
            _mind.TransferTo(targetMindId, observer);

        #region Parenting pseudoentity
        var targetXform = Transform(target);
        while (targetXform.ParentUid.IsValid())
        {
            if (targetXform.ParentUid == observer)
                return;

            targetXform = Transform(targetXform.ParentUid);
        }

        _actionBlocker.UpdateCanMove(observer);

        var eye = EnsureComp<EyeComponent>(observer);
        _eye.SetDrawFov(observer, true, eye);
        //EnsureComp<AlternativeSpeechComponent>(observer);
        var observerComp = EnsureComp<ControlledObserverComponent>(observer);
        observerComp.Source = target;

        var xform = Transform(observer);
        ContainerSystem.AttachParentToContainerOrGrid((observer, xform));

        // If we didn't get to parent's container.
        if (xform.ParentUid != Transform(xform.ParentUid).ParentUid)
        {
            _transform.SetCoordinates(observer, xform, new EntityCoordinates(target, Vector2.Zero), rotation: Angle.Zero);
        }

        _physicsSystem.SetLinearVelocity(observer, Vector2.Zero);

        Dirty(observer, observerComp);
        #endregion

        var comp = EnsureComp<ControlledComponent>(target);
        comp.Duration = duration;
        comp.Priority = priority;
        comp.Observer = observer;
        comp.Controller = uid;
        comp.Key = key;

        UpdateVisuals(observer, observerComp);

        if (TryComp<DamageableComponent>(target, out var damageable))
            _damageable.TryChangeDamage(observer, damageable.Damage);

        _mind.TransferTo(uidMindId, target);
    }

    /// <summary>
    ///     Attempts to stop any players controlling target (excluding body owner, ofc)
    /// </summary>
    /// <param name="uid">The entity to stop control.</param>
    /// <param name="component">ControlledComponent of the target.</param>
    /// <param name="maxPriority">Maximum priority for stopping control. If null, stops any.</param>
    /// <param name="key">Keyword that indicates is stopping possible.</param>
    /// <returns>If the control could be stopped by entered params.</returns>
    public bool StopControlling(EntityUid uid, ControlledComponent? component = null, int? maxPriority = null, string? key = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (maxPriority != null && maxPriority <= component.Priority)
            return false;
        if (key != null && component.Key != key)
            return false;

        if (!_mind.TryGetMind(uid, out var controllingMindId, out _))
        {
            if (_mind.TryGetMind(component.Observer, out var mindId, out _))
                _mind.TransferTo(mindId, uid);
            QueueDel(component.Observer);
            RemComp<ControlledComponent>(uid);
            var damage_brute = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 1f);
            _damageable.TryChangeDamage(uid, damage_brute);
            return true;
        }
        else
        {
            if (!Deleted(component.Controller))
                _mind.TransferTo(controllingMindId, component.Controller);

            if (_mind.TryGetMind(component.Observer, out var mindId, out _))
                _mind.TransferTo(mindId, uid);
            QueueDel(component.Observer);
            RemComp<ControlledComponent>(uid);
            var damage_brute = new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), 1f);
            _damageable.TryChangeDamage(uid, damage_brute);
            return true;
        }
    }

    public void UpdateVisuals(EntityUid uid, ControlledObserverComponent component)
    {
        var target = component.Source;

        if (TryComp<AlertsComponent>(target, out var alerts))
        {
            var pseudoAlerts = EnsureComp<AlertsComponent>(uid);
            pseudoAlerts.Alerts = alerts.Alerts;
        }
    }

    private void OnMobState(EntityUid uid, ControlledComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Critical)
            StopControlling(uid, component);
        if (args.NewMobState == MobState.Dead)
            StopControlling(uid, component);
    }

    private void OnTryInteract<T>(EntityUid uid, ControlledObserverComponent component, T args) where T : CancellableEntityEventArgs
    {
        args.Cancel();
    }

    private void OnInteractionAttempt(EntityUid uid, ControlledObserverComponent component, InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnDamage(EntityUid uid, ControlledComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;
        _damageable.TryChangeDamage(component.Observer, args.DamageDelta);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ControlledComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Deleted(comp.Controller))
                continue;

            comp.Accumulator += frameTime;

            if (TryComp<ControlledObserverComponent>(comp.Observer, out var obs))
                UpdateVisuals(comp.Observer, obs);

            if (comp.Accumulator <= 1)
                continue;
            comp.Accumulator -= 1;

            comp.Duration -= 1f;

            if (comp.Duration <= 0f)
                StopControlling(uid, comp);
        }
    }

}
