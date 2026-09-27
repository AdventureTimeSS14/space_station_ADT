using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;

namespace Content.Shared.ADT.Mobs.Systems;

public abstract class SharedSoftCritSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _moveMod = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SoftCritComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnRefreshSpeed(Entity<SoftCritComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_mobState.IsSoftCritical(ent.Owner))
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    private void OnMobStateChanged(EntityUid uid, SoftCritComponent component, MobStateChangedEvent args)
    {
        _moveMod.RefreshMovementSpeedModifiers(uid);
        OnStateChanged((uid, component), args);
    }

    protected virtual void OnStateChanged(Entity<SoftCritComponent> ent, MobStateChangedEvent args)
    {
    }
}
