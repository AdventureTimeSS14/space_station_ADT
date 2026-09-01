using Content.Shared.ADT.SoftCrit.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.SoftCrit;

/// <summary>
///     SS13-style soft crit system: while the mob is alive but has taken more
///     than <see cref="SoftCritComponent.DamageThreshold"/> damage, it falls
///     down and can only weakly crawl — between normal state and hard crit.
/// </summary>
public sealed class SoftCritSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<SoftCritComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SoftCritComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SoftCritComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    public bool IsSoftCrit(Entity<SoftCritComponent> ent)
    {
        if (HasComp<IgnoreSoftCritComponent>(ent))
            return false;

        if (!TryComp<MobStateComponent>(ent, out var mobState) || mobState.CurrentState != MobState.Alive)
            return false;

        if (!TryComp<DamageableComponent>(ent, out var damage))
            return false;

        return _damageable.GetTotalDamage((ent.Owner, damage)) >= ent.Comp.DamageThreshold;
    }

    private void OnDamageChanged(Entity<SoftCritComponent> ent, ref DamageChangedEvent args)
    {
        UpdateState(ent);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMobStateChanged(Entity<SoftCritComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateState(ent);
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnComponentShutdown(Entity<SoftCritComponent> ent, ref ComponentShutdown args)
    {
        // if the component is removed while the mob is downed - stand it back up
        if (HasComp<SoftCritDownedComponent>(ent))
        {
            RemComp<SoftCritDownedComponent>(ent);
            _standing.Stand(ent.Owner);
        }
    }

    private void UpdateState(Entity<SoftCritComponent> ent)
    {
        var soft = IsSoftCrit(ent);

        if (soft && !HasComp<SoftCritDownedComponent>(ent))
        {
            EnsureComp<SoftCritDownedComponent>(ent);
            _standing.Down(ent.Owner, dropHeldItems: false);
        }
        else if (!soft && HasComp<SoftCritDownedComponent>(ent))
        {
            RemComp<SoftCritDownedComponent>(ent);
            _standing.Stand(ent.Owner);
        }
    }

    private void OnRefreshMovespeed(Entity<SoftCritComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!IsSoftCrit(ent))
            return;

        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }
}
