using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Components.Equipment;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology.Systems;

/// <summary>
/// This handles any actions that slime mobs may have.
/// </summary>
public sealed partial class SlimeLatchSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeLatchEvent>(OnLatchAttempt);
        SubscribeLocalEvent<SlimeComponent, SlimeLatchDoAfterEvent>(OnSlimeLatchDoAfter);
        SubscribeLocalEvent<SlimeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<SlimeDamageOvertimeComponent, MobStateChangedEvent>(OnMobStateChangedSOD);
        SubscribeLocalEvent<SlimeComponent, MobStateChangedEvent>(OnMobStateChangedSlime);
        SubscribeLocalEvent<SlimeComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<SlimeComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<SlimeComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sodQuery = EntityQueryEnumerator<SlimeDamageOvertimeComponent>();
        while (sodQuery.MoveNext(out var uid, out var dotComp))
            UpdateHunger((uid, dotComp));
    }

    private void UpdateHunger(Entity<SlimeDamageOvertimeComponent> ent)
    {
        if (_gameTiming.CurTime < ent.Comp.NextTickTime || _mobState.IsDead(ent))
            return;

        var addedHunger = (float)ent.Comp.Damage.GetTotal();
        ent.Comp.NextTickTime = _gameTiming.CurTime + ent.Comp.Interval;
        _damageable.TryChangeDamage(ent, ent.Comp.Damage, ignoreResistances: true);

        if (ent.Comp.SourceEntityUid is { } source && TryComp<HungerComponent>(ent.Comp.SourceEntityUid, out var hunger))
        {
            _hunger.ModifyHunger(source, addedHunger, hunger);
            Dirty(source, hunger);
        }
    }

    private void OnMobStateChangedSOD(Entity<SlimeDamageOvertimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var source = ent.Comp.SourceEntityUid;
        if (source.HasValue && TryComp<SlimeComponent>(source, out var slime))
            Unlatch((source.Value, slime));
    }

    private void OnMobStateChangedSlime(Entity<SlimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Unlatch(ent);
    }

    private void OnPullAttempt(Entity<SlimeComponent> ent, ref PullAttemptEvent args)
    {
        if (IsLatched(ent) && args.PullerUid == ent.Owner)
        {
            args.Cancelled = true;
            return;
        }

        Unlatch(ent);
    }

    private void OnEntRemovedFromContainer(Entity<SlimeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (!HasComp<XenoVacuumTankComponent>(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    private void OnEntInsertedIntoContainer(Entity<SlimeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!HasComp<XenoVacuumTankComponent>(args.Container.Owner))
            return;

        Unlatch(ent);
    }

    private void OnLatchAttempt(SlimeLatchEvent args)
    {
        if (TerminatingOrDeleted(args.Target)
        || TerminatingOrDeleted(args.Performer)
        || !TryComp<SlimeComponent>(args.Performer, out var slime))
            return;

        var ent = new Entity<SlimeComponent>(args.Performer, slime);

        if (IsLatched(ent))
        {
            Unlatch(ent);
            return;
        }

        if (CanLatch((args.Performer, slime), args.Target))
        {
            StartSlimeLatchDoAfter((args.Performer, slime), args.Target);
            return;
        }
    }

    private bool StartSlimeLatchDoAfter(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (_mobState.IsDead(target))
        {
            var targetDeadPopup = Loc.GetString("slime-latch-fail-target-dead");
            _popup.PopupEntity(targetDeadPopup, ent, ent);

            return false;
        }

        if (ent.Comp.Stomach.Count >= ent.Comp.MaxContainedEntities)
        {
            var maxEntitiesPopup = Loc.GetString("slime-latch-fail-max-entities");
            _popup.PopupEntity(maxEntitiesPopup, ent, ent);

            return false;
        }

        var attemptPopup = Loc.GetString("slime-latch-attempt", ("slime", ent), ("ent", target));
        _popup.PopupEntity(attemptPopup, ent, PopupType.MediumCaution);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.LatchDoAfterDuration, new SlimeLatchDoAfterEvent(), ent, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        EnsureComp<BeingConsumedComponent>(target);
        _doAfter.TryStartDoAfter(doAfterArgs);
        return true;
    }

    private void OnSlimeLatchDoAfter(Entity<SlimeComponent> ent, ref SlimeLatchDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (args.Handled || args.Cancelled)
        {
            RemCompDeferred<BeingConsumedComponent>(target);
            return;
        }

        Latch(ent, target);
        args.Handled = true;
    }

    private void OnExamined(Entity<SlimeComponent> slime, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || slime.Comp.Stomach.Count <= 0)
            return;

        var text = Loc.GetString("slime-examined-text", ("num", slime.Comp.Stomach.Count));
        args.PushMarkup(text);
    }

    #region Helpers

    public bool IsLatched(Entity<SlimeComponent> ent)
        => ent.Comp.LatchedTarget.HasValue;

    public bool IsLatched(Entity<SlimeComponent> ent, EntityUid target)
        => IsLatched(ent) && ent.Comp.LatchedTarget!.Value == target;

    public bool CanLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        return !(IsLatched(ent)
            || _mobState.IsDead(target)
            || !_actionBlocker.CanInteract(ent, target)
            || !HasComp<MobStateComponent>(target));
    }

    public bool NpcTryLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (!CanLatch(ent, target))
            return false;

        return StartSlimeLatchDoAfter(ent, target);
    }

    public void Latch(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (IsLatched(ent))
            Unlatch(ent);

        _xform.SetCoordinates(ent, Transform(target).Coordinates);
        _xform.SetParent(ent, target);
        if (TryComp<InputMoverComponent>(ent, out var inpm))
            inpm.CanMove = false;

        ent.Comp.LatchedTarget = target;

        EnsureComp(target, out SlimeDamageOvertimeComponent comp);
        comp.SourceEntityUid = ent;

        _audio.PlayEntity(ent.Comp.EatSound, ent, ent);
        _popup.PopupEntity(Loc.GetString("slime-action-latch-success", ("slime", ent), ("target", target)), ent, PopupType.SmallCaution);

        Dirty(ent);
        Dirty(target, comp);
    }

    public void Unlatch(Entity<SlimeComponent> ent)
    {
        if (!IsLatched(ent))
            return;

        var target = ent.Comp.LatchedTarget!.Value;

        RemCompDeferred<BeingConsumedComponent>(target);
        RemCompDeferred<SlimeDamageOvertimeComponent>(target);

        _xform.SetParent(ent, _xform.GetParentUid(target));
        if (TryComp<InputMoverComponent>(ent, out var inpm))
            inpm.CanMove = true;

        ent.Comp.LatchedTarget = null;
    }

    #endregion
}
