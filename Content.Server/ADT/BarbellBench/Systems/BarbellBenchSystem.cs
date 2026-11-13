using System.Linq;
using Content.Server.ADT.BarbellBench.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.ADT.BarbellBench;
using Content.Shared.ADT.BarbellBench.Components;
using Content.Shared.ADT.BarbellBench.Systems;
using Content.Shared.ADT.Damage.Events;
using Content.Shared.ADT.Silicon;
using Content.Shared.ADT.Training;
using Content.Shared.Alert;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BarbellBench.Systems;

public sealed class BarbellBenchSystem : SharedBarbellBenchSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private RespiratorSystem _respirator = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private TrainingProgressSystem _training = default!;

    private static readonly TimeSpan PinDuration = TimeSpan.FromSeconds(27);
    private static readonly TimeSpan PinnedTickInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextPinnedTick;
    private readonly List<EntityUid> _finishedReps = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarbellBenchComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BarbellBenchComponent, BarbellBenchPerformRepEvent>(OnPerformRep);
        SubscribeLocalEvent<BarbellBenchComponent, EntInsertedIntoContainerMessage>(OnBarbellInserted);
        SubscribeLocalEvent<BarbellBenchComponent, EntRemovedFromContainerMessage>(OnBarbellRemoved);
        SubscribeLocalEvent<BarbellBenchComponent, UnstrappedEvent>(OnUnstrapped);

        SubscribeLocalEvent<BarbellPinnedComponent, UnbuckleAttemptEvent>(OnPinnedUnbuckleAttempt);
        SubscribeLocalEvent<BarbellPinnedComponent, UnbuckleAlertEvent>(OnPinnedUnbuckleAlert);
        SubscribeLocalEvent<BarbellPinnedComponent, ComponentShutdown>(OnPinnedShutdown);
        SubscribeLocalEvent<BarbellPinnedComponent, BeforeStaminaDamageEvent>(OnPinnedStaminaDamage);
        SubscribeLocalEvent<BarbellPinnedComponent, InteractHandEvent>(OnPinnedInteractHand,
            before: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<BarbellPinnedComponent, GetVerbsEvent<InteractionVerb>>(OnPinnedGetVerbs,
            before: new[] { typeof(SharedBuckleSystem) });

        SubscribeLocalEvent<StaminaComponent, BeforeStaminaCritEvent>(OnBeforeStaminaCrit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        UpdateReps(now);

        if (now < _nextPinnedTick)
            return;

        _nextPinnedTick = now + PinnedTickInterval;
        UpdatePinned(now);
    }

    private void OnStartup(Entity<BarbellBenchComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
        _appearance.SetData(ent, BarbellBenchVisuals.HasBarbell, TryGetBarbell(ent, out _));
    }

    private void OnBarbellInserted(Entity<BarbellBenchComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BarbellSlotId)
            return;

        _appearance.SetData(ent, BarbellBenchVisuals.HasBarbell, true);

        if (TryComp<StrapComponent>(ent, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities)
            {
                _actionsSystem.AddAction(buckled, ref ent.Comp.BarbellRepAction, BarbellRepActionId, ent);
            }
        }
    }

    private void OnBarbellRemoved(Entity<BarbellBenchComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.BarbellSlotId)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        _appearance.SetData(ent, BarbellBenchVisuals.HasBarbell, false);

        if (TryComp<StrapComponent>(ent, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities.ToArray())
            {
                if (ent.Comp.BarbellRepAction is { Valid: true } action)
                    _actionsSystem.RemoveProvidedAction(buckled, ent, action);

                if (TryComp<BarbellPinnedComponent>(buckled, out var pinned))
                    Unpin((buckled, pinned), args.Entity, true, true);
            }
        }
    }

    private void OnPerformRep(Entity<BarbellBenchComponent> ent, ref BarbellBenchPerformRepEvent args)
    {
        if (ent.Comp.IsPerformingRep || !TryGetBarbell(ent, out var barbell))
            return;

        var performer = args.Performer;
        var now = _timing.CurTime;
        var rep = EnsureComp<ActiveBarbellRepComponent>(ent);
        rep.Performer = performer;
        rep.SoundTime = now + TimeSpan.FromSeconds(ent.Comp.RepSoundDelay);
        rep.EndTime = now + TimeSpan.FromSeconds(ent.Comp.RepDuration);
        rep.SoundPlayed = false;

        SetPerformingRep(ent, true);
        args.Handled = true;

        if (TryComp<BarbellLiftComponent>(barbell, out var lift))
        {
            _stamina.TakeStaminaDamage(performer, lift.StaminaCost, source: performer, with: barbell, visual: true);
            _popup.PopupEntity(Loc.GetString(lift.EmoteLocSelf), performer, performer, PopupType.Medium);
        }

        _training.AddTrainingProgress(performer, barbell);
    }

    private void UpdateReps(TimeSpan now)
    {
        _finishedReps.Clear();

        var query = EntityQueryEnumerator<ActiveBarbellRepComponent, BarbellBenchComponent>();
        while (query.MoveNext(out var uid, out var rep, out var bench))
        {
            if (!rep.SoundPlayed && now >= rep.SoundTime)
            {
                rep.SoundPlayed = true;
                var sound = new SoundCollectionSpecifier(bench.RepSoundCollection);
                var audioParams = AudioParams.Default.WithMaxDistance(bench.RepSoundMaxDistance);
                _audio.PlayEntity(sound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, audioParams);
            }

            if (now >= rep.EndTime)
                _finishedReps.Add(uid);
        }

        foreach (var uid in _finishedReps)
        {
            RemComp<ActiveBarbellRepComponent>(uid);

            if (TryComp<BarbellBenchComponent>(uid, out var bench))
                SetPerformingRep((uid, bench), false);
        }
    }

    private void SetPerformingRep(Entity<BarbellBenchComponent> ent, bool value)
    {
        ent.Comp.IsPerformingRep = value;
        Dirty(ent);
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    protected override void OnUnstrapped(Entity<BarbellBenchComponent> bench, ref UnstrappedEvent args)
    {
        base.OnUnstrapped(bench, ref args);
        RemComp<ActiveBarbellRepComponent>(bench);

        var buckled = args.Buckle.Owner;
        if (TryComp<BarbellPinnedComponent>(buckled, out var pinned) && pinned.Bench == bench.Owner)
            Unpin((buckled, pinned), null, false, true);
    }

    private void OnBeforeStaminaCrit(Entity<StaminaComponent> ent, ref BeforeStaminaCritEvent args)
    {
        if (HasComp<MobIpcComponent>(ent))
            return;

        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo is not { } benchUid)
            return;

        if (!TryComp<ActiveBarbellRepComponent>(benchUid, out var rep) || rep.Performer != ent.Owner)
            return;

        if (!TryComp<BarbellBenchComponent>(benchUid, out var bench) || !TryGetBarbell((benchUid, bench), out var barbell))
            return;

        var pinned = EnsureComp<BarbellPinnedComponent>(ent);
        pinned.OriginalStunTime ??= ent.Comp.StunTime;
        pinned.Bench = benchUid;
        pinned.PinnedAt = _timing.CurTime;
        Dirty(ent, pinned);
        EnsureComp<ActiveBarbellPinnedComponent>(ent);

        ent.Comp.StunTime = PinDuration;
        Dirty(ent);

        _virtualItem.TrySpawnVirtualItemInHand(barbell, ent, dropOthers: true);
        _virtualItem.TrySpawnVirtualItemInHand(barbell, ent, dropOthers: true);

        _alerts.ClearAlertCategory(ent.Owner, SharedBuckleSystem.BuckledAlertCategory);
        _popup.PopupEntity(Loc.GetString("barbell-bench-pinned"), ent, ent, PopupType.LargeCaution);
    }

    private void OnPinnedUnbuckleAttempt(Entity<BarbellPinnedComponent> ent, ref UnbuckleAttemptEvent args)
    {
        if (HasComp<MobIpcComponent>(ent))
        {
            Unpin(ent, null, false);
            return;
        }

        if (args.User == ent.Owner)
        {
            if (StunPassed(ent) || StaminaRecovered(ent))
            {
                Unpin(ent, null, false);
                return;
            }

            args.Cancelled = true;
            if (args.Popup)
                _popup.PopupEntity(Loc.GetString("barbell-bench-cannot-unbuckle"), ent, ent, PopupType.MediumCaution);

            return;
        }

        Unpin(ent, null, false, true);
    }

    private void OnPinnedInteractHand(Entity<BarbellPinnedComponent> ent, ref InteractHandEvent args)
    {
        if (args.User != ent.Owner || StunPassed(ent))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("barbell-bench-pinned"), ent, ent, PopupType.MediumCaution);
    }

    private void OnPinnedUnbuckleAlert(Entity<BarbellPinnedComponent> ent, ref UnbuckleAlertEvent args)
    {
        args.Handled = true;
    }

    private void OnPinnedGetVerbs(Entity<BarbellPinnedComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (StunPassed(ent))
            return;

        args.Verbs.RemoveWhere(v => v.Category == VerbCategory.Unbuckle);
    }

    private void OnPinnedShutdown(Entity<BarbellPinnedComponent> ent, ref ComponentShutdown args)
    {
        RemComp<ActiveBarbellPinnedComponent>(ent);
    }

    private void OnPinnedStaminaDamage(Entity<BarbellPinnedComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value < 0f)
            args.Value *= 0.02f;
    }

    private void UpdatePinned(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ActiveBarbellPinnedComponent, BarbellPinnedComponent>();
        while (query.MoveNext(out var uid, out _, out var pinned))
        {
            if (StaminaRecovered(uid) || !TryGetPinnedBarbell(pinned, out _))
            {
                Unpin((uid, pinned), null, true);
                continue;
            }

            var stunPassed = StunPassed(uid);

            if (stunPassed && TryComp<StrapComponent>(pinned.Bench, out var strap))
                _alerts.ShowAlert(uid, strap.BuckledAlertType);
            else
                _alerts.ClearAlertCategory(uid, SharedBuckleSystem.BuckledAlertCategory);

            if (now - pinned.PinnedAt >= PinDuration || !TryComp<RespiratorComponent>(uid, out var respirator))
                continue;

            _respirator.UpdateSaturation(uid, -2f, respirator);
            _damageable.TryChangeDamage(uid, respirator.Damage * 3, interruptsDoAfters: false);
        }
    }

    private void Unpin(Entity<BarbellPinnedComponent> ent, EntityUid? barbell, bool popup, bool release = false)
    {
        if (barbell != null)
            _virtualItem.DeleteInHandsMatching(ent, barbell.Value);
        else if (TryGetPinnedBarbell(ent.Comp, out var pinnedBarbell))
            _virtualItem.DeleteInHandsMatching(ent, pinnedBarbell);

        if (ent.Comp.OriginalStunTime is { } stunTime && TryComp<StaminaComponent>(ent, out var stamina))
        {
            stamina.StunTime = stunTime;
            Dirty(ent, stamina);
        }

        RemComp<BarbellPinnedComponent>(ent);
        RemComp<ActiveBarbellPinnedComponent>(ent);

        if (release)
        {
            _stamina.ExitStamCrit(ent);
            _statusEffects.TryRemoveStatusEffect(ent, SharedStunSystem.StunId);
            RemComp<KnockedDownComponent>(ent);
        }

        if (popup)
            _popup.PopupEntity(Loc.GetString("barbell-bench-recovered"), ent, ent, PopupType.Medium);
    }

    private bool StunPassed(EntityUid uid)
    {
        return !TryComp<KnockedDownComponent>(uid, out var knockedDown) || knockedDown.NextUpdate <= _timing.CurTime;
    }

    private bool StaminaRecovered(EntityUid uid)
    {
        return TryComp<StaminaComponent>(uid, out var stamina) &&
               _stamina.GetStaminaDamage(uid, stamina) < stamina.CritThreshold &&
               !stamina.Critical;
    }

    private bool TryGetPinnedBarbell(BarbellPinnedComponent pinned, out EntityUid barbell)
    {
        barbell = default;
        if (pinned.Bench is not { } benchUid || !TryComp<BarbellBenchComponent>(benchUid, out var bench))
            return false;

        return TryGetBarbell((benchUid, bench), out barbell);
    }

    private bool TryGetBarbell(Entity<BarbellBenchComponent> ent, out EntityUid barbell)
    {
        barbell = default;
        if (!Container.TryGetContainer(ent, ent.Comp.BarbellSlotId, out var container) || container.ContainedEntities.Count == 0)
            return false;

        barbell = container.ContainedEntities[0];
        return true;
    }
}
