using Content.Shared.Actions;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Server.ADT.Sleeping;

/// <summary>
/// Даёт действие сна мобам, лежащим на полу или пристёгнутым к предметам без HealOnBuckle.
/// </summary>
public sealed partial class SleepAnywhereSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<MobStateComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<MobStateComponent, DownedEvent>(OnStandingChanged);
        SubscribeLocalEvent<MobStateComponent, StoodEvent>(OnStandingChanged);

        SubscribeLocalEvent<SleepAnywhereComponent, UpdateCanMoveEvent>(OnCanMoveChanged);
        SubscribeLocalEvent<SleepAnywhereComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnBuckled(EntityUid uid, MobStateComponent comp, ref BuckledEvent args) => Refresh(uid);

    private void OnUnbuckled(EntityUid uid, MobStateComponent comp, ref UnbuckledEvent args) => Refresh(uid);

    private void OnStandingChanged(EntityUid uid, MobStateComponent comp, EntityEventArgs args) => Refresh(uid);

    private void OnCanMoveChanged(EntityUid uid, SleepAnywhereComponent comp, UpdateCanMoveEvent args) => Refresh(uid);

    private void OnMobStateChanged(EntityUid uid, SleepAnywhereComponent comp, MobStateChangedEvent args) => Refresh(uid);

    private void Refresh(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        SetAction((uid, EnsureComp<SleepAnywhereComponent>(uid)), CanSleepHere(uid));
    }

    private bool CanSleepHere(EntityUid uid)
    {
        if (TryComp<StunnedComponent>(uid, out var stunned) && stunned.LifeStage < ComponentLifeStage.Stopping)
            return false;

        if (_mobState.IsIncapacitated(uid))
            return false;

        if (TryComp<BuckleComponent>(uid, out var buckle) && buckle.BuckledTo is { } strap)
        {
            return !HasComp<HealOnBuckleComponent>(strap);
        }

        return _standing.IsDown(uid);
    }

    private void SetAction(Entity<SleepAnywhereComponent> ent, bool granted)
    {
        var attached = _actions.GetAction(ent.Comp.Action) is { } action && action.Comp.AttachedEntity == ent.Owner;
        if (attached == granted)
            return;

        if (granted)
            _actions.AddAction(ent.Owner, ref ent.Comp.Action, SleepingSystem.SleepActionId);
        else
            _actions.RemoveAction(ent.Owner, ent.Comp.Action);
    }
}
