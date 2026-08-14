using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Chemistry.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Content.Server.Speech.Components;
using System.Linq;

namespace Content.Server.ADT.Xenobiology.Systems;

/// <summary>
/// This handles any actions that slime mobs may have.
/// </summary>
public sealed partial class SlimeLatchSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeLatchEvent>(OnLatchAttempt);
        SubscribeLocalEvent<SlimeComponent, SlimeLatchDoAfterEvent>(OnSlimeLatchDoAfter);
        SubscribeLocalEvent<SlimeComponent, DoAfterAttemptEvent<SlimeLatchDoAfterEvent>>(OnDoAfterAttempt);

        SubscribeLocalEvent<SlimeDamageOvertimeComponent, MobStateChangedEvent>(OnMobStateChangedSOD);
        SubscribeLocalEvent<SlimeComponent, MobStateChangedEvent>(OnMobStateChangedSlime);
        SubscribeLocalEvent<SlimeComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<SlimeComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
        SubscribeLocalEvent<SlimeComponent, EntGotInsertedIntoContainerMessage>(OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<SlimeComponent, SlimeMitosisEvent>(OnSlimeMitosis);
        SubscribeLocalEvent<SlimeComponent, EntityTerminatingEvent>(OnSlimeTerminating);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sodQuery = EntityQueryEnumerator<SlimeDamageOvertimeComponent>();
        while (sodQuery.MoveNext(out var uid, out var dotComp))
        {
            if (dotComp.SourceEntityUid is not { } source || 
                Deleted(source) || 
                !TryComp<SlimeComponent>(source, out var slimeComp) || 
                !IsLatched((source, slimeComp)))
            {
                CleanupLatchedComponents(uid);
                continue;
            }

            UpdateHunger((uid, dotComp));
        }

        var query = EntityQueryEnumerator<SlimeComponent>();
        while (query.MoveNext(out var uid, out var slime))
        {
            var slimeEnt = new Entity<SlimeComponent>(uid, slime);

            if (!IsLatched(slimeEnt))
                continue;

            var target = slime.LatchedTarget!.Value;

            if (Deleted(target))
            {
                Unlatch(slimeEnt);
                continue;
            }

            if (!_mobState.IsDead(target))
                continue;

            if (IsPlayerControlled(target))
            {
                Unlatch(slimeEnt);
                continue;
            }

            ConsumeCorpse(slimeEnt, target);
        }
    }

    #region Event Handlers

    private void OnSlimeTerminating(Entity<SlimeComponent> ent, ref EntityTerminatingEvent args)
    {
        if (IsLatched(ent))
            Unlatch(ent);
    }

    private void OnMobStateChangedSOD(Entity<SlimeDamageOvertimeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var source = ent.Comp.SourceEntityUid;
        if (!source.HasValue || !TryComp<SlimeComponent>(source, out var slime))
            return;

        var slimeEnt = new Entity<SlimeComponent>(source.Value, slime);

        if (IsPlayerControlled(ent) || _mobState.IsDead(slimeEnt))
        {
            Unlatch(slimeEnt);
            return;
        }

        ConsumeCorpse(slimeEnt, ent);
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

    private void OnEntGotRemovedFromContainer(Entity<SlimeComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        Unlatch(ent);
    }

    private void OnEntGotInsertedIntoContainer(Entity<SlimeComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        Unlatch(ent);
    }

    private void OnSlimeMitosis(Entity<SlimeComponent> ent, ref SlimeMitosisEvent args)
    {
        var target = ent.Comp.LatchedTarget;

        Unlatch(ent);

        if (target is not { } latchTarget || Deleted(latchTarget) || args.Offspring.Count == 0)
            return;

        var newSlime = _random.Pick(args.Offspring);
        if (!TryComp<SlimeComponent>(newSlime, out var slimeComp))
            return;

        Latch(new Entity<SlimeComponent>(newSlime, slimeComp), latchTarget);
    }

    private void OnLatchAttempt(SlimeLatchEvent args)
    {
        if (Deleted(args.Target) || Deleted(args.Performer) || !TryComp<SlimeComponent>(args.Performer, out var slime))
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
        }
    }

    private void OnDoAfterAttempt(EntityUid uid, SlimeComponent comp, ref DoAfterAttemptEvent<SlimeLatchDoAfterEvent> args)
    {
        if (HasComp<BeingLatchedComponent>(args.Event.Target))
            args.Cancel();
    }

    private void OnSlimeLatchDoAfter(Entity<SlimeComponent> ent, ref SlimeLatchDoAfterEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (args.Handled || args.Cancelled)
            return;

        Latch(ent, target);
        args.Handled = true;
    }

    #endregion

    #region Core Logic

    private void UpdateHunger(Entity<SlimeDamageOvertimeComponent> ent)
    {
        if (_gameTiming.CurTime < ent.Comp.NextTickTime || _mobState.IsDead(ent))
            return;

        ent.Comp.NextTickTime = _gameTiming.CurTime + ent.Comp.Interval;

        var target = ent.Owner;
        if (Deleted(target))
        {
            CleanupLatchedComponents(target);
            return;
        }

        if (ent.Comp.SourceEntityUid is not { } source || Deleted(source) || !TryComp<SlimeComponent>(source, out _))
        {
            CleanupLatchedComponents(target);
            return;
        }

        // Дополнительная проверка - слайм должен быть прикреплен
        if (!IsLatched((source, Comp<SlimeComponent>(source))))
        {
            CleanupLatchedComponents(target);
            return;
        }

        // Наносим урон цели
        if (TryComp<DamageableComponent>(ent, out var damageable))
            _damageable.TryChangeDamage((ent.Owner, damageable), ent.Comp.Damage, ignoreResistances: true);

        // Восполняем голод слайма ТОЛЬКО если он прикреплен
        var addedHunger = (float)ent.Comp.Damage.GetTotal();
        if (TryComp<HungerComponent>(source, out var hunger))
        {
            _hunger.ModifyHunger(source, addedHunger, hunger);
            Dirty(source, hunger);
        }

        // Трансфер растворов
        if (!TryComp<BodyComponent>(source, out var bodyComp))
            return;

        var stomachList = new List<Entity<StomachComponent>>();
        _body.TryGetOrgansWithComponent(new Entity<BodyComponent?>(source, bodyComp), out stomachList);

        if (stomachList.Count == 0)
            return;

        float availableVolume = 0;
        foreach (var stomach in stomachList)
        {
            if (_solutionContainer.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp.Solution, out var sol))
                availableVolume += (float)sol.AvailableVolume;
        }

        if (TryComp<BloodstreamComponent>(ent, out var bloodstream)
            && _solutionContainer.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var blood)
            && _solutionContainer.ResolveSolution(ent.Owner, bloodstream.MetabolitesSolutionName, ref bloodstream.MetabolitesSolution, out var chem))
        {
            float bloodProportion = (float)(blood.Volume / (chem.Volume + blood.Volume));
            float chemProportion = 1 - bloodProportion;
            float bloodTransfer = Math.Min(ent.Comp.SuctionUnits * bloodProportion, availableVolume * bloodProportion);
            float chemTransfer = Math.Min(ent.Comp.SuctionUnits * chemProportion, availableVolume * chemProportion);
            
            foreach (var stomach in stomachList)
            {
                var bloodSolution = blood.SplitSolutionWithout(FixedPoint2.New(bloodTransfer / stomachList.Count), ent.Comp.ToxinReagent);
                _stomach.TryTransferSolution(stomach.Owner, bloodSolution, stomach);
                var chemSolution = blood.SplitSolution(FixedPoint2.New(chemTransfer / stomachList.Count));
                _stomach.TryTransferSolution(stomach.Owner, chemSolution, stomach);
            }
            chem.AddReagent(ent.Comp.ToxinReagent, FixedPoint2.New(ent.Comp.ToxinUnits));
        }
    }

    private void ConsumeCorpse(Entity<SlimeComponent> slime, EntityUid corpse)
    {
        if (Deleted(corpse))
        {
            Unlatch(slime);
            return;
        }

        Unlatch(slime);

        if (slime.Comp.EatSound != null)
            _audio.PlayEntity(slime.Comp.EatSound, slime, slime);

        if (HasComp<MonkeyAccentComponent>(corpse))
        {
            slime.Comp.Friendship = MathF.Min(1f, slime.Comp.Friendship + slime.Comp.FriendshipPerMeal);
            Dirty(slime);
        }
    }

    #endregion

    #region Public Methods

    public bool IsLatched(Entity<SlimeComponent> ent)
        => ent.Comp.LatchedTarget.HasValue;

    public bool IsLatched(Entity<SlimeComponent> ent, EntityUid target)
        => IsLatched(ent) && ent.Comp.LatchedTarget!.Value == target;

    public bool CanLatch(Entity<SlimeComponent> ent, EntityUid target)
    {
        return !(IsLatched(ent)
            || _mobState.IsDead(target)
            || !_actionBlocker.CanInteract(ent, target)
            || !HasComp<MobStateComponent>(target)
            || HasComp<BeingLatchedComponent>(target)
            || Deleted(target));
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

        if (Deleted(target))
            return;

        _xform.SetCoordinates(ent, Transform(target).Coordinates);
        _xform.SetParent(ent, target);
        if (TryComp<InputMoverComponent>(ent, out var inpm))
            inpm.CanMove = false;

        // Отключаем физику слайма, чтобы он не дёргал цель столкновениями
        if (TryComp<PhysicsComponent>(ent, out var physics))
            _physics.SetCanCollide(ent, false, body: physics);

        ent.Comp.LatchedTarget = target;

        EnsureComp<BeingLatchedComponent>(target);
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

        CleanupLatchedComponents(target);

        if (TryComp<TransformComponent>(target, out var targetXform)
            && _xform.IsParentOf(targetXform, ent.Owner))
            _xform.SetParent(ent.Owner, _xform.GetParentUid(target));

        if (TryComp<InputMoverComponent>(ent, out var inpm))
            inpm.CanMove = true;

        // Возвращаем физику слайму
        if (TryComp<PhysicsComponent>(ent, out var physics))
            _physics.SetCanCollide(ent, true, body: physics);

        ent.Comp.LatchedTarget = null;
    }

    #endregion

    #region Helpers

    private bool IsPlayerControlled(EntityUid entity)
    {
        return _mind.TryGetMind(entity, out _, out _);
    }

    private void CleanupLatchedComponents(EntityUid target)
    {
        RemCompDeferred<BeingLatchedComponent>(target);
        RemCompDeferred<SlimeDamageOvertimeComponent>(target);
    }

    private bool StartSlimeLatchDoAfter(Entity<SlimeComponent> ent, EntityUid target)
    {
        if (_mobState.IsDead(target))
        {
            var targetDeadPopup = Loc.GetString("slime-latch-fail-target-dead", ("ent", target));
            _popup.PopupEntity(targetDeadPopup, ent, ent);
            return false;
        }

        if (ent.Comp.Stomach.Count >= ent.Comp.MaxContainedEntities)
        {
            var maxEntitiesPopup = Loc.GetString("slime-latch-fail-max-entities", ("ent", target));
            _popup.PopupEntity(maxEntitiesPopup, ent, ent);
            return false;
        }

        if (HasComp<BeingLatchedComponent>(target))
        {
            var alreadyLatchedPopup = Loc.GetString("slime-latch-fail-already-latched", ("ent", target));
            _popup.PopupEntity(alreadyLatchedPopup, ent, ent);
            return false;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, ent.Comp.LatchDoAfterDuration, new SlimeLatchDoAfterEvent(), ent, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return false;

        var attemptPopup = Loc.GetString("slime-latch-attempt", ("slime", ent), ("ent", target));
        _popup.PopupEntity(attemptPopup, ent, PopupType.MediumCaution);
        return true;
    }

    #endregion
}
