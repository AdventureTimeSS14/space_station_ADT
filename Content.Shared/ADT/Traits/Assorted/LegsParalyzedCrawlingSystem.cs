using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Traits.Assorted;

public sealed class LegsParalyzedCrawlingSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, BuckledEvent>(OnBuckled);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, UnbuckledEvent>(OnUnbuckled);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMoveSpeed);
        SubscribeLocalEvent<LegsParalyzedCrawlyngComponent, ThrowPushbackAttemptEvent>(OnThrowPushbackAttempt);
    }

    private void OnStartup(EntityUid uid, LegsParalyzedCrawlyngComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out MetaDataComponent? meta) && meta.EntityLifeStage > EntityLifeStage.MapInitialized)
            EnsureCrawling(uid, component);

        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnMapInit(EntityUid uid, LegsParalyzedCrawlyngComponent component, MapInitEvent args)
    {
        EnsureCrawling(uid, component, true);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnPlayerAttached(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref PlayerAttachedEvent args)
    {
        EnsureCrawling(uid, component, true);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnShutdown(EntityUid uid, LegsParalyzedCrawlyngComponent component, ComponentShutdown args)
    {
        RemoveCrawling(uid, component);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnBuckled(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref BuckledEvent args)
    {
        RemoveCrawling(uid, component);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnUnbuckled(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref UnbuckledEvent args)
    {
        EnsureCrawling(uid, component, true);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnStandUpAttempt(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref StandUpAttemptEvent args)
    {
        if (ShouldIgnoreForcedCrawling(uid))
            return;

        args.Cancelled = true;
    }

    private void OnStood(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref StoodEvent args)
    {
        if (ShouldIgnoreForcedCrawling(uid))
            return;

        EnsureCrawling(uid, component, true);
        _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshMoveSpeed(EntityUid uid, LegsParalyzedCrawlyngComponent component, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ShouldIgnoreSpeedModifier(uid))
            return;

        args.ModifySpeed(0.2f);
    }

    private void OnThrowPushbackAttempt(EntityUid uid, LegsParalyzedCrawlyngComponent component, ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }

    private void EnsureCrawling(EntityUid uid, LegsParalyzedCrawlyngComponent component, bool forceDown = false)
    {
        if (ShouldIgnoreForcedCrawling(uid))
            return;

        if (!HasComp<KnockedDownComponent>(uid))
        {
            _stun.TryKnockdown(uid, null, refresh: true, autoStand: false, drop: false, force: true);
            component.AddedKnockdown = HasComp<KnockedDownComponent>(uid);
        }
    }

    private void RemoveCrawling(EntityUid uid, LegsParalyzedCrawlyngComponent component)
    {
        if (!component.AddedKnockdown || !HasComp<KnockedDownComponent>(uid))
            return;

        RemCompDeferred<KnockedDownComponent>(uid);
        component.AddedKnockdown = false;
    }

    private bool ShouldIgnoreForcedCrawling(EntityUid uid)
    {
        return TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled;
    }

    private bool ShouldIgnoreSpeedModifier(EntityUid uid)
    {
        return ShouldIgnoreForcedCrawling(uid);
    }
}
