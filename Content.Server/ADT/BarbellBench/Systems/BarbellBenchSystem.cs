using Content.Server.ADT.BarbellBench.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.ADT.BarbellBench;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.ADT.BarbellBench.Systems;
using Content.Shared.ADT.Damage.Events;
using Content.Shared.ADT.Silicon;
using Content.Shared.Damage.Events;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.Verbs;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Server.ADT.BarbellBench.Systems;

public sealed class BarbellBenchSystem : SharedBarbellBenchSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RespiratorSystem _respirator = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    private readonly Dictionary<EntityUid, float> _playerPullStrength = new();

    private readonly Dictionary<EntityUid, bool> _staminaBonusMaxLevelApplied = new();

    private readonly HashSet<EntityUid> _performingReps = new();

    private TimeSpan _nextSuffocationDamage = TimeSpan.Zero;

    private const double PinDurationSeconds = 27;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BarbellBenchComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BarbellBenchComponent, BarbellBenchPerformRepEvent>(OnPerformRep);
        SubscribeLocalEvent<BarbellBenchComponent, AttachableHolderAttachablesAlteredEvent>(OnAttachableAltered);

        SubscribeLocalEvent<BarbellPinnedComponent, UnbuckleAttemptEvent>(OnPinnedUnbuckleAttempt);
        SubscribeLocalEvent<BarbellPinnedComponent, UnbuckleAlertEvent>(OnPinnedUnbuckleAlert);
        SubscribeLocalEvent<BarbellPinnedComponent, ComponentShutdown>(OnPinnedShutdown);
        SubscribeLocalEvent<StaminaComponent, BeforeStaminaCritEvent>(OnBeforeStaminaCrit);
        SubscribeLocalEvent<BarbellPinnedComponent, BeforeStaminaDamageEvent>(OnPinnedStaminaDamage);

        SubscribeLocalEvent<BarbellPinnedComponent, InteractHandEvent>(OnPinnedInteractHand,
            before: new[] { typeof(SharedBuckleSystem) });

        SubscribeLocalEvent<BarbellPinnedComponent, GetVerbsEvent<InteractionVerb>>(OnPinnedGetVerbs,
            before: new[] { typeof(SharedBuckleSystem) });

        SubscribeLocalEvent<PullerComponent, ComponentShutdown>(OnPullerShutdown);
        SubscribeLocalEvent<PullerComponent, EntityTerminatingEvent>(OnPullerTerminating);
        SubscribeLocalEvent<BarbellBenchComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnAttachableAltered(EntityUid uid, BarbellBenchComponent component, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (args.SlotId != component.BarbellSlotId)
            return;

        if (component.OverlayEntity is not { } overlay || !Exists(overlay))
            return;

        _appearance.SetData(uid, BarbellBenchVisuals.HasBarbell, args.Alteration == AttachableAlteredType.Attached);

        if (TryComp<StrapComponent>(uid, out var strap))
        {
            foreach (var buckledEntity in strap.BuckledEntities)
            {
                if (args.Alteration == AttachableAlteredType.Attached)
                {
                    _actionsSystem.AddAction(buckledEntity, ref component.BarbellRepAction, SharedBarbellBenchSystem.BarbellRepActionId, uid);
                }
                else if (args.Alteration == AttachableAlteredType.Detached)
                {
                    if (component.BarbellRepAction is { Valid: true } action)
                    {
                        _actionsSystem.RemoveProvidedAction(buckledEntity, uid, action);
                    }
                }
            }
        }

        switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                var barbellMeta = MetaData(args.Attachable);
                _metaData.SetEntityName(overlay, barbellMeta.EntityName);
                _metaData.SetEntityDescription(overlay, barbellMeta.EntityDescription);
                break;

            case AttachableAlteredType.Detached:
                var overlayMeta = MetaData(overlay);
                if (overlayMeta.EntityPrototype != null)
                {
                    _metaData.SetEntityName(overlay, overlayMeta.EntityPrototype.Name);
                    _metaData.SetEntityDescription(overlay, overlayMeta.EntityPrototype.Description);
                }

                if (TryComp<StrapComponent>(uid, out var strapComp))
                {
                    foreach (var buckledEntity in strapComp.BuckledEntities)
                    {
                        if (HasComp<BarbellPinnedComponent>(buckledEntity))
                        {
                            _virtualItem.DeleteInHandsMatching(buckledEntity, args.Attachable);

                            RemComp<BarbellPinnedComponent>(buckledEntity);
                            RemComp<ActiveBarbellPinnedComponent>(buckledEntity);
                            _popup.PopupEntity(Loc.GetString("barbell-bench-recovered"), buckledEntity, buckledEntity, PopupType.Medium);
                        }
                    }
                }
                break;
        }
    }

    private void OnStartup(EntityUid uid, BarbellBenchComponent component, ComponentStartup args)
    {
        EnsureOverlay(uid, component);
        UpdateAppearance(uid, component);
    }

    protected override void OnStrapped(Entity<BarbellBenchComponent> bench, ref StrappedEvent args)
    {
        base.OnStrapped(bench, ref args);
    }

    private void OnShutdown(EntityUid uid, BarbellBenchComponent component, ComponentShutdown args)
    {
        if (component.OverlayEntity is { } overlay && Exists(overlay))
            Del(overlay);
        component.OverlayEntity = null;
    }

    private void EnsureOverlay(EntityUid uid, BarbellBenchComponent component)
    {
        if (component.OverlayEntity is { } existing && Exists(existing))
            return;

        var coords = Transform(uid).Coordinates;
        var overlay = Spawn(component.OverlayPrototype, coords);

        var overlayXform = Transform(overlay);
        _transform.SetParent(overlay, overlayXform, uid);
        _transform.SetCoordinates(overlay, overlayXform, new EntityCoordinates(uid, Vector2.Zero));
        overlayXform.LocalRotation = Angle.Zero;

        component.OverlayEntity = overlay;
        Dirty(uid, component);
    }

    private void OnPerformRep(EntityUid uid, BarbellBenchComponent component, BarbellBenchPerformRepEvent args)
    {
        if (component.IsPerformingRep)
            return;

        if (Container.TryGetContainer(uid, component.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
        {
            var barbell = barbellContainer.ContainedEntities[0];
            if (TryComp<BarbellLiftComponent>(barbell, out var lift))
            {
                _performingReps.Add(args.Performer);

                _stamina.TakeStaminaDamage(args.Performer, lift.StaminaCost, source: args.Performer, with: barbell, visual: true);
                _popup.PopupEntity(Loc.GetString(lift.EmoteLocSelf), args.Performer, args.Performer, PopupType.Medium);

                if (!HasComp<MobIpcComponent>(args.Performer))
                {
                    PullerComponent? pullerComp = null;
                    if (Resolve(args.Performer, ref pullerComp))
                    {
                        if (!_playerPullStrength.TryGetValue(args.Performer, out var currentStrength))
                        {
                            currentStrength = 0f;
                        }

                        var previousStrength = currentStrength;
                        currentStrength += 0.02f;
                        _playerPullStrength[args.Performer] = currentStrength;

                        float targetReduction;
                        if (currentStrength >= 1.00f)
                        {
                            targetReduction = 1.00f;
                        }
                        else if (currentStrength >= 0.70f)
                        {
                            targetReduction = 0.70f;
                        }
                        else if (currentStrength >= 0.40f)
                        {
                            targetReduction = 0.40f;
                        }
                        else
                        {
                            targetReduction = pullerComp.PulledDensityReduction;
                        }

                        pullerComp.PulledDensityReduction = targetReduction;
                        Dirty(args.Performer, pullerComp);

                        var performer = args.Performer;
                        if (previousStrength < 0.40f && currentStrength >= 0.40f)
                        {
                            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
                            {
                                if (Exists(performer))
                                    _popup.PopupEntity(Loc.GetString("barbell-bench-strength-increased"), performer, performer, PopupType.Medium);
                            });
                        }
                        else if (previousStrength < 0.70f && currentStrength >= 0.70f)
                        {
                            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
                            {
                                if (Exists(performer))
                                    _popup.PopupEntity(Loc.GetString("barbell-bench-strength-increased"), performer, performer, PopupType.Medium);
                            });
                        }
                        else if (previousStrength < 1.00f && currentStrength >= 1.00f)
                        {
                            Timer.Spawn(TimeSpan.FromSeconds(0.5f), () =>
                            {
                                if (Exists(performer))
                                    _popup.PopupEntity(Loc.GetString("barbell-bench-strength-increased"), performer, performer, PopupType.Medium);
                            });
                        }

                        if (currentStrength >= 1.00f && !_staminaBonusMaxLevelApplied.GetValueOrDefault(args.Performer, false))
                        {
                            if (TryComp<StaminaComponent>(args.Performer, out var staminaComp))
                            {
                                staminaComp.CritThreshold += 25f;
                                _staminaBonusMaxLevelApplied[args.Performer] = true;
                                Dirty(args.Performer, staminaComp);
                            }
                        }
                    }
                }
            }
        }

        component.IsPerformingRep = true;
        Dirty(uid, component);
        UpdateAppearance(uid, component);

        var sound = new SoundCollectionSpecifier(component.RepSoundCollection);
        Timer.Spawn(TimeSpan.FromSeconds(component.RepSoundDelay), () =>
        {
            if (Exists(uid))
            {
                var filter = Filter.Pvs(uid, entityManager: EntityManager);
                _audio.PlayGlobal(sound, filter, recordReplay: true);
            }
        });

        Timer.Spawn(TimeSpan.FromSeconds(component.RepDuration), () =>
        {
            if (!TryComp<BarbellBenchComponent>(uid, out var comp))
                return;

            comp.IsPerformingRep = false;
            Dirty(uid, comp);
            UpdateAppearance(uid, comp);

            if (TryComp<BuckleComponent>(args.Performer, out var buckle) && buckle.BuckledTo == uid)
            {
                _performingReps.Remove(args.Performer);
            }
        });

        args.Handled = true;
    }

    private void OnBeforeStaminaCrit(EntityUid uid, StaminaComponent component, ref BeforeStaminaCritEvent args)
    {
        if (!_performingReps.Contains(uid))
            return;

        if (!TryComp<BuckleComponent>(uid, out var buckle) || buckle.BuckledTo == null)
            return;

        if (!TryComp<BarbellBenchComponent>(buckle.BuckledTo, out var bench))
            return;

        if (!Container.TryGetContainer(buckle.BuckledTo.Value, bench.BarbellSlotId, out var barbellContainer) || barbellContainer.Count == 0)
            return;

        if (HasComp<MobIpcComponent>(uid))
            return;

        var barbell = barbellContainer.ContainedEntities[0];

        var pinnedComp = EnsureComp<BarbellPinnedComponent>(uid);
        pinnedComp.Bench = buckle.BuckledTo;
        pinnedComp.PinnedAt = _gameTiming.CurTime;
        Dirty(uid, pinnedComp);

        if (TryComp<StaminaComponent>(uid, out var staminaComp))
        {
            staminaComp.StunTime = TimeSpan.FromSeconds(PinDurationSeconds);
            Dirty(uid, staminaComp);
        }

        _virtualItem.TrySpawnVirtualItemInHand(barbell, uid, dropOthers: true);
        _virtualItem.TrySpawnVirtualItemInHand(barbell, uid, dropOthers: true);

        _alerts.ClearAlertCategory(uid, SharedBuckleSystem.BuckledAlertCategory);

        _popup.PopupEntity(Loc.GetString("barbell-bench-pinned"), uid, uid, PopupType.LargeCaution);

        EnsureComp<ActiveBarbellPinnedComponent>(uid);
    }

    private void OnPinnedUnbuckleAttempt(EntityUid uid, BarbellPinnedComponent component, ref UnbuckleAttemptEvent args)
    {
        if (HasComp<MobIpcComponent>(uid))
        {
            RemComp<BarbellPinnedComponent>(uid);
            RemComp<ActiveBarbellPinnedComponent>(uid);
            return;
        }

        if (args.User == uid)
        {
            var stunPassed = false;
            if (TryComp<KnockedDownComponent>(uid, out var knockedDown))
            {
                stunPassed = knockedDown.NextUpdate <= _gameTiming.CurTime;
            }

            var staminaRecovered = false;
            if (TryComp<StaminaComponent>(uid, out var staminaComp))
            {
                var currentStamina = _stamina.GetStaminaDamage(uid, staminaComp);
                staminaRecovered = currentStamina < staminaComp.CritThreshold && !staminaComp.Critical;
            }

            if (stunPassed || staminaRecovered)
            {
                if (component.Bench != null && Exists(component.Bench))
                {
                    if (TryComp<BarbellBenchComponent>(component.Bench, out var bench))
                    {
                        if (component.Bench.HasValue && Container.TryGetContainer(component.Bench.Value, bench.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
                        {
                            var barbell = barbellContainer.ContainedEntities[0];
                            _virtualItem.DeleteInHandsMatching(uid, barbell);
                        }
                    }
                }

                RemComp<BarbellPinnedComponent>(uid);
                RemComp<ActiveBarbellPinnedComponent>(uid);
                return;
            }

            args.Cancelled = true;
            if (args.Popup)
            {
                _popup.PopupEntity(Loc.GetString("barbell-bench-cannot-unbuckle"), uid, uid, PopupType.MediumCaution);
            }
            return;
        }

        if (component.Bench != null && Exists(component.Bench) && TryComp<BarbellBenchComponent>(component.Bench, out var benchOther)
            && component.Bench.HasValue && Container.TryGetContainer(component.Bench.Value, benchOther.BarbellSlotId, out var barbellContainerOther) && barbellContainerOther.Count > 0)
        {
            var barbellEnt = barbellContainerOther.ContainedEntities[0];
            _virtualItem.DeleteInHandsMatching(uid, barbellEnt);
        }

        RemComp<BarbellPinnedComponent>(uid);
        RemComp<ActiveBarbellPinnedComponent>(uid);

        _stamina.ExitStamCrit(uid);
        _statusEffects.TryRemoveStatusEffect(uid, SharedStunSystem.StunId);
        RemComp<KnockedDownComponent>(uid);
    }

    private void OnPinnedInteractHand(EntityUid uid, BarbellPinnedComponent component, ref InteractHandEvent args)
    {
        if (args.User == uid)
        {
            var stunPassed = false;
            if (TryComp<KnockedDownComponent>(uid, out var knockedDown))
            {
                stunPassed = knockedDown.NextUpdate <= _gameTiming.CurTime;
            }

            if (!stunPassed)
            {
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("barbell-bench-pinned"), uid, uid, PopupType.MediumCaution);
            }
        }
    }

    private void OnPinnedUnbuckleAlert(EntityUid uid, BarbellPinnedComponent component, ref UnbuckleAlertEvent args)
    {
        args.Handled = true;
    }

    private void OnPinnedGetVerbs(EntityUid uid, BarbellPinnedComponent component, ref GetVerbsEvent<InteractionVerb> args)
    {
        var stunPassed = false;
        if (TryComp<KnockedDownComponent>(uid, out var knockedDown))
        {
            stunPassed = knockedDown.NextUpdate <= _gameTiming.CurTime;
        }

        if (!stunPassed)
        {
            var verbsToRemove = args.Verbs.Where(v => v.Category == VerbCategory.Unbuckle).ToList();
            foreach (var verb in verbsToRemove)
            {
                args.Verbs.Remove(verb);
            }
        }
    }

    private void OnPinnedShutdown(EntityUid uid, BarbellPinnedComponent component, ComponentShutdown args)
    {
        RemComp<ActiveBarbellPinnedComponent>(uid);
    }

    private void OnPinnedStaminaDamage(EntityUid uid, BarbellPinnedComponent component, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value < 0f)
        {
            args.Value *= 0.02f;
        }
    }

    private void CleanupPullerData(EntityUid uid)
    {
        _playerPullStrength.Remove(uid);
        _staminaBonusMaxLevelApplied.Remove(uid);
    }

    private void OnPullerShutdown(EntityUid uid, PullerComponent component, ComponentShutdown args)
    {
        CleanupPullerData(uid);
    }

    private void OnPullerTerminating(EntityUid uid, PullerComponent component, EntityTerminatingEvent args)
    {
        CleanupPullerData(uid);
    }

    protected override void OnUnstrapped(Entity<BarbellBenchComponent> bench, ref UnstrappedEvent args)
    {
        base.OnUnstrapped(bench, ref args);

        _performingReps.Remove(args.Buckle.Owner);
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent ev)
    {
        _performingReps.Remove(ev.Entity.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;

        if (curTime >= _nextSuffocationDamage)
        {
            _nextSuffocationDamage = curTime + TimeSpan.FromSeconds(1f);

            var query = EntityQueryEnumerator<ActiveBarbellPinnedComponent, BarbellPinnedComponent>();
            while (query.MoveNext(out var uid, out _, out var pinned))
            {
                if (TryComp<StaminaComponent>(uid, out var staminaComp))
                {
                    var currentStamina = _stamina.GetStaminaDamage(uid, staminaComp);
                    if (currentStamina < staminaComp.CritThreshold && !staminaComp.Critical)
                    {
                        if (pinned.Bench != null && Exists(pinned.Bench))
                        {
                            if (TryComp<BarbellBenchComponent>(pinned.Bench, out var bench))
                            {
                                if (pinned.Bench.HasValue && Container.TryGetContainer(pinned.Bench.Value, bench.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
                                {
                                    var barbell = barbellContainer.ContainedEntities[0];
                                    _virtualItem.DeleteInHandsMatching(uid, barbell);
                                }
                            }
                        }

                        RemComp<BarbellPinnedComponent>(uid);
                        RemComp<ActiveBarbellPinnedComponent>(uid);
                        _popup.PopupEntity(Loc.GetString("barbell-bench-recovered"), uid, uid, PopupType.Medium);
                        continue;
                    }
                }

                var stunPassed = false;
                if (TryComp<KnockedDownComponent>(uid, out var knockedDown))
                {
                    stunPassed = knockedDown.NextUpdate <= curTime;
                }

                if (stunPassed && pinned.Bench != null && Exists(pinned.Bench))
                {
                    if (TryComp<StrapComponent>(pinned.Bench, out var strap))
                    {
                        _alerts.ShowAlert(uid, strap.BuckledAlertType);
                    }
                }

                var pinDuration = TimeSpan.FromSeconds(PinDurationSeconds);
                var suffocationActive = (curTime - pinned.PinnedAt) < pinDuration;

                if (pinned.Bench != null && Exists(pinned.Bench))
                {
                    if (TryComp<BarbellBenchComponent>(pinned.Bench, out var bench))
                    {
                        if (pinned.Bench.HasValue && Container.TryGetContainer(pinned.Bench.Value, bench.BarbellSlotId, out var barbellContainer) && barbellContainer.Count > 0)
                        {
                            if (suffocationActive && TryComp<RespiratorComponent>(uid, out var respirator))
                            {
                                _respirator.UpdateSaturation(uid, -2f, respirator);

                                _damageable.TryChangeDamage(uid, respirator.Damage, interruptsDoAfters: false);
                                _damageable.TryChangeDamage(uid, respirator.Damage, interruptsDoAfters: false);
                                _damageable.TryChangeDamage(uid, respirator.Damage, interruptsDoAfters: false);
                            }

                            if (!stunPassed)
                            {
                                _alerts.ClearAlertCategory(uid, SharedBuckleSystem.BuckledAlertCategory);
                            }

                            continue;
                        }
                    }
                }

                RemComp<BarbellPinnedComponent>(uid);
                RemComp<ActiveBarbellPinnedComponent>(uid);
                _popup.PopupEntity(Loc.GetString("barbell-bench-recovered"), uid, uid, PopupType.Medium);
            }
        }
    }

}
