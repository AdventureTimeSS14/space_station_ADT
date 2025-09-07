using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.Actions.Events;
using Content.Shared.ADT.Grab;
using Content.Shared.ADT.Hands;
using Content.Shared.Climbing.Events;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.GrabProtection;

namespace Content.Shared.Movement.Pulling.Systems;

public abstract partial class PullingSystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private void InitializeGrab()
    {
        SubscribeLocalEvent<PullerComponent, GrabStageChangedEvent>(HandleGrabStageChanged);
        SubscribeLocalEvent<PullerComponent, SetGrabStageDoAfterEvent>(OnSetGrabStage);
        SubscribeLocalEvent<PullerComponent, BeforeVirtualItemThrownEvent>(OnVirtualItemThrow);
        SubscribeLocalEvent<PullerComponent, DisarmAttemptEvent>(OnDisarmAttempt);

        SubscribeLocalEvent<PullableComponent, GrabStageChangedEvent>(HandlePullableGrabStageChanged);
        SubscribeLocalEvent<PullableComponent, SelfBeforeClimbEvent>(OnBeforeClimb);
        SubscribeLocalEvent<PullableComponent, GrabEscapeDoAfterEvent>(OnEscapeDoAfter);
        SubscribeLocalEvent<PullableComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<PullableComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        SubscribeLocalEvent<PullableComponent, StartPullAttemptEvent>(OnStartPullAttempt);

        SubscribeLocalEvent<GrabThrownComponent, ThrowDoHitEvent>(OnThrownDoHit);
        SubscribeLocalEvent<GrabThrownComponent, LandEvent>(OnThrownLand);

        SubscribeLocalEvent<ModifyGrabStageTimeComponent, ModifyGrabStageTimeEvent>(OnGrabStageTimeModify);
        SubscribeLocalEvent<ModifyGrabStageTimeComponent, InventoryRelayedEvent<ModifyGrabStageTimeEvent>>(OnRelayGrabStageTimeModify);
    }

    #region Events handle
    private void HandleGrabStageChanged(EntityUid uid, PullerComponent puller, ref GrabStageChangedEvent args)
    {
        if (args.Puller.Owner != uid)
            return;

        _alertsSystem.ShowAlert(uid, puller.PullingAlert, (short)args.NewStage);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.NewStage > args.OldStage)
            _doAfter.Cancel(args.Pulling.Comp.EscapeAttemptDoAfter);

        var oldHands = puller.GrabStats[args.OldStage].RequiredHands;
        var newHands = puller.GrabStats[args.NewStage].RequiredHands;
        if (oldHands == newHands)
            return;

        if (newHands > oldHands)
        {
            if (!TryComp<HandsComponent>(uid, out var hands))
                return;

            var toSpawn = newHands - oldHands;
            for (var i = 0; i < toSpawn; i++)
            {
                if (_virtualItem.TrySpawnVirtualItemInHand(args.Pulling, uid, out var virtualItem, true))
                    puller.VirtualItems.Add(GetNetEntity(virtualItem.Value));
            }
        }
        else
        {
            var toRemove = oldHands - newHands;
            for (var i = 0; i < toRemove; i++)
            {
                if (puller.VirtualItems.Count <= 0)
                    break;

                var item = GetEntity(puller.VirtualItems.Last());

                if (Exists(item) && !Terminating(item))
                {
                    _virtualItem.DeleteVirtualItem((item, Comp<VirtualItemComponent>(item)), uid);
                    puller.VirtualItems.Remove(puller.VirtualItems.Last());
                }
            }
        }

        if (_net.IsServer)
            Dirty(uid, puller);
    }

    private void OnSetGrabStage(EntityUid uid, PullerComponent comp, SetGrabStageDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            comp.StageIncreaseDoAfter = null;
            return;
        }

        if (!comp.Pulling.HasValue)
            return;

        if (!TryComp<PullableComponent>(comp.Pulling, out var pullable))
            return;

        if (args.Direction > 0)
            TryIncreaseGrabStage((uid, comp), (comp.Pulling.Value, pullable));
        else
            TryLowerGrabStage((uid, comp), (comp.Pulling.Value, pullable), uid);

        comp.StageIncreaseDoAfter = null;
    }

    private void OnVirtualItemThrow(EntityUid uid, PullerComponent comp, BeforeVirtualItemThrownEvent args)
    {
        if (comp.Pulling != args.BlockingEntity)
            return;

        if (comp.NextStageChange > _timing.CurTime)
            return;

        // Throwing is possible only at hard and choke stages
        if (comp.Stage < GrabStage.Hard)
        {
            args.Cancel();
            return;
        }

        var target = comp.Pulling.Value;
        if (!TryComp<PullableComponent>(target, out var pullable))
            return;

        args.Cancel();
        comp.NextStageChange = _timing.CurTime + TimeSpan.FromSeconds(1f);
        comp.GrabbingDirection = 0;

        TryStopPull(target, pullable, uid);

        _blocker.UpdateCanMove(target);
        _modifierSystem.RefreshMovementSpeedModifiers(uid);

        Throw((uid, comp), (target, pullable), args.Coords);
    }

    private void OnDisarmAttempt(EntityUid uid, PullerComponent comp, DisarmAttemptEvent args)
    {
        if (args.DisarmerUid == uid)
            return;
        if (comp.Stage < GrabStage.Soft)
            return;
        if (args.DisarmerUid != comp.Pulling)
            return;
        args.Cancelled = true;
    }
    private void HandlePullableGrabStageChanged(EntityUid uid, PullableComponent puller, ref GrabStageChangedEvent args)
    {
        if (args.Puller.Owner == uid)
            return;

        _alertsSystem.ShowAlert(uid, puller.PulledAlert, (short)args.NewStage);

        if (args.NewStage != GrabStage.Choke)
            return;
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;
        foreach (var hand in hands.Hands)
        {
            _handsSystem.TryDrop(uid, hand.Value);
        }
    }

    private void OnBeforeClimb(EntityUid uid, PullableComponent comp, SelfBeforeClimbEvent args)
    {
        if (args.PuttingOnTable == args.GettingPutOnTable)
            return;
        if (args.PuttingOnTable != comp.Puller)
            return;
        if (!TryComp<PullerComponent>(args.PuttingOnTable, out var pullerComp))
            return;
        if (pullerComp.Stage < GrabStage.Hard)
            return;

        args.Cancel();

        var puller = args.PuttingOnTable;
        var stunTime = TimeSpan.FromSeconds(2);

        _damageable.TryChangeDamage(uid, new(_proto.Index<DamageTypePrototype>("Blunt"), 17));
        _stun.TryParalyze(uid, stunTime, true);
        _audio.PlayPredicted(new SoundCollectionSpecifier("TrayHit"), uid, args.PuttingOnTable);
        TryStopPull(uid, comp);

        // Someone else slamed you onto the table.
        // This is only run in server so you need to use popup entity.
        if (!_net.IsServer)
            return;

        var gettingPutOnTableName = Identity.Entity(uid, EntityManager);
        var puttingOnTableName = Identity.Entity(puller, EntityManager);

        _popup.PopupEntity(
            Loc.GetString("forced-bonkable-success-message",
                ("bonker", puttingOnTableName),
                ("victim", gettingPutOnTableName),
                ("bonkable", args.BeingClimbedOn)),
                args.GettingPutOnTable,
                PopupType.MediumCaution);
    }

    private void OnEscapeDoAfter(EntityUid uid, PullableComponent comp, GrabEscapeDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            comp.EscapeAttemptDoAfter = null;
            comp.EscapeAttemptCounter = 1;
            return;
        }

        if (!TryComp<PullerComponent>(comp.Puller, out var puller))
            return;

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), uid, uid);

        // Check how much doafters we need to escape and repeat them if EscapeAttemptCounter is lesser
        if (puller.GrabStats[puller.Stage].DoaftersToEscape > comp.EscapeAttemptCounter)
        {
            if (_timing.IsFirstTimePredicted)
            {
                comp.EscapeAttemptCounter++;
                Dirty(uid, comp);
            }

            _popup.PopupPredicted(Loc.GetString("grab-escape-attempt-popup-self"), Loc.GetString("grab-escape-attempt-popup-others", ("pullable", Identity.Entity(uid, EntityManager))), uid, uid, PopupType.Small);
            if (args.DoAfter.Id == comp.EscapeAttemptDoAfter)
                args.Repeat = true;
        }
        else
        {
            comp.EscapeAttemptCounter = 1;
            _popup.PopupPredicted(Loc.GetString("grab-escape-success-popup-self"), Loc.GetString("grab-escape-success-popup-others", ("pullable", Identity.Entity(uid, EntityManager))), uid, uid, PopupType.Small);
            TryLowerGrabStage((comp.Puller.Value, puller), (uid, comp), uid);
            args.Repeat = false;
            comp.EscapeAttemptDoAfter = null;
            _blocker.UpdateCanMove(uid);
        }
    }

    private void OnUpdateCanMove(EntityUid uid, PullableComponent comp, UpdateCanMoveEvent args)
    {
        if (!comp.Puller.HasValue)
            return;
        if (!TryComp<PullerComponent>(comp.Puller, out var puller))
            return;
        if (puller.Stage == GrabStage.None)
            return;
        args.Cancel();
    }

    private void OnBeingPulledAttempt(EntityUid uid, PullableComponent comp, BeingPulledAttemptEvent args)
    {
        if (comp.Puller == args.Puller)
            return;
        if (!TryComp<PullerComponent>(comp.Puller, out var puller))
            return;
        if (puller.Stage == GrabStage.None)
            return;
        args.Cancel();
    }

    private void OnStartPullAttempt(EntityUid uid, PullableComponent comp, StartPullAttemptEvent args)
    {
        if (!comp.Puller.HasValue)
            return;
        if (!TryComp<PullerComponent>(comp.Puller, out var puller))
            return;
        if (puller.Stage == GrabStage.None)
            return;
        args.Cancel();
    }

    private void OnThrownDoHit(EntityUid uid, GrabThrownComponent comp, ThrowDoHitEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (comp.HitEntities.Contains(args.Target))
            return;
        comp.HitEntities.Add(args.Target);

        if (TryComp<PhysicsComponent>(args.Target, out var physics) && physics.BodyType == BodyType.Static)
        {
            var stunTime = TimeSpan.FromSeconds(1);
            _damageable.TryChangeDamage(uid, new(_proto.Index<DamageTypePrototype>("Blunt"), 8));
            _stun.TryParalyze(uid, stunTime, true);
            _audio.PlayPredicted(new SoundCollectionSpecifier("MetalThud"), uid, uid);
        }

        // To avoid mouse throwing abuse
        if (HasComp<ItemComponent>(uid))
            return;

        if (!HasComp<StaminaComponent>(args.Target))
            return;
        if (_standing.IsDown(args.Target))
            return;

        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), uid, uid);
        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(2), true);
        _stamina.TakeStaminaDamage(args.Target, 65f);
        _stamina.TakeStaminaDamage(uid, 65f);
        _standing.Down(uid);

        comp.CollideCounter++;
        if (comp.CollideCounter < comp.MaxCollides)
            return;

        _stun.TryParalyze(uid, TimeSpan.FromSeconds(2), true);
        if (!_gravity.IsWeightless(uid))
            _physics.SetLinearVelocity(uid, Vector2.Zero);
        if (TryComp<ThrownItemComponent>(uid, out var thrown))
            _thrown.LandComponent(uid, thrown, Comp<PhysicsComponent>(uid), true);
    }

    private void OnThrownLand(EntityUid uid, GrabThrownComponent comp, LandEvent args)
    {
        RemComp(uid, comp);
    }
    public void Throw(
        EntityUid uid,
        EntityUid thrower,
        Vector2 vector,
        float grabThrownSpeed)
    {
        EnsureComp<GrabThrownComponent>(uid);

        _throwing.TryThrow(uid, vector, grabThrownSpeed, animated: false);
    }

    private void OnGrabStageTimeModify(EntityUid uid, ModifyGrabStageTimeComponent comp, ref ModifyGrabStageTimeEvent args)
    {
        if (comp.Modifiers.TryGetValue(args.Stage, out var value))
            args.Modifier *= value;
    }

    private void OnRelayGrabStageTimeModify(EntityUid uid, ModifyGrabStageTimeComponent comp, ref InventoryRelayedEvent<ModifyGrabStageTimeEvent> args)
    {
        if (comp.Modifiers.TryGetValue(args.Args.Stage, out var value))
            args.Args.Modifier *= value;
    }
    #endregion

    #region Pulic functions
    public bool TryStartPullingOrGrab(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling == pullable.Owner)
            return TryIncreaseGrabStageOrStopPulling(puller, pullable);

        return TryStartPull(puller, pullable);
    }

    public bool TryIncreaseGrabStageOrStopPulling(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (HasComp<GrabProtectionComponent>(pullable.Owner))
            return TryStopPull(pullable, pullable.Comp, puller);
        if (puller.Comp.Pulling != pullable.Owner)
            return TryStartPull(puller, pullable);
        if (!_combat.IsInCombatMode(puller.Owner))
            return TryStopPull(pullable, pullable.Comp, puller);

        if (!HasComp<MobStateComponent>(pullable))
            return TryStopPull(pullable, pullable.Comp, puller);

        return TryStartGrabDoAfter(puller, pullable, 1);
    }

    public bool TryLowerGrabStageOrStopPulling(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;
        if (!_combat.IsInCombatMode(puller.Owner))
            return TryStopPull(pullable, pullable.Comp, puller);
        if (!HasComp<MobStateComponent>(pullable))
            return TryStopPull(pullable, pullable.Comp, puller);

        return TryStartGrabDoAfter(puller, pullable, -1);
    }

    public virtual bool TryIncreaseGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;

        if (puller.Comp.Stage == GrabStage.Choke)
            return false;

        if (puller.Comp.NextStageChange > _timing.CurTime)
            return false;

        var ev = new ModifyGrabStageTimeEvent(puller.Comp.Stage + 1);
        RaiseLocalEvent(pullable.Owner, ref ev);
        if (ev.Cancelled)
            return false;

        // Check if the puller has enough hands to progress to the next stage
        if (puller.Comp.GrabStats.TryGetValue(puller.Comp.Stage + 1, out var stats))
        {
            var freeableHands = 0;
            if (TryComp<HandsComponent>(puller, out var hands))
                freeableHands = _handsSystem.CountFreeableHands((puller.Owner, hands));

            if (freeableHands < stats.RequiredHands)
                return false;
        }

        puller.Comp.NextStageChange = _timing.CurTime + TimeSpan.FromSeconds(1f);

        puller.Comp.GrabbingDirection = 1;
        _modifierSystem.RefreshMovementSpeedModifiers(puller);

        // Raise the grab stage changed event
        var message = new GrabStageChangedEvent(puller, pullable, puller.Comp.Stage, puller.Comp.Stage + 1);
        RaiseLocalEvent(puller, ref message);
        RaiseLocalEvent(pullable, ref message);
        _blocker.UpdateCanMove(pullable);

        _adminLogger.Add(LogType.Grab, LogImpact.Low, $"{ToPrettyString(puller.Owner)} increased grab stage for {ToPrettyString(pullable.Owner)} (Current - {puller.Comp.Stage + 1})");
        return true;
    }

    public virtual bool TryLowerGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, EntityUid user)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;
        if (puller.Comp.NextStageChange > _timing.CurTime && user != pullable.Owner)
            return false;

        // Stop pulling if the puller is at the lowest grab stage
        if (puller.Comp.Stage == GrabStage.None)
        {
            StopPulling(pullable.Owner, pullable);
            return true;
        }

        puller.Comp.NextStageChange = _timing.CurTime + TimeSpan.FromSeconds(1f);
        puller.Comp.GrabbingDirection = -1;
        _modifierSystem.RefreshMovementSpeedModifiers(puller);

        // Raise the grab stage changed event
        var message = new GrabStageChangedEvent(puller, pullable, puller.Comp.Stage, puller.Comp.Stage - 1);
        RaiseLocalEvent(puller, ref message);
        RaiseLocalEvent(pullable, ref message);
        _blocker.UpdateCanMove(pullable);

        _adminLogger.Add(LogType.Grab, LogImpact.Low, $"{ToPrettyString(puller.Owner)} lowered grab stage for {ToPrettyString(pullable.Owner)} (Current - {puller.Comp.Stage - 1})");
        return true;
    }

    public bool TryStartGrabDoAfter(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, int direction)
    {
        if (puller.Comp.Pulling != pullable.Owner)
            return false;

        if (puller.Comp.StageIncreaseDoAfter.HasValue)
            return true;

        if (puller.Comp.Stage == GrabStage.Choke && direction > 0)
            return false;

        if (puller.Comp.Stage == GrabStage.None && direction < 0)
        {
            TryStopPull(pullable, pullable, puller);
            return true;
        }

        if (puller.Comp.NextStageChange > _timing.CurTime)
            return false;

        if (HasComp<GrabProtectionComponent>(pullable.Owner))
            return false;

        var targetStage = puller.Comp.Stage + direction;

        // Check if the puller has enough hands to progress to the next stage
        if (puller.Comp.GrabStats.TryGetValue(targetStage, out var stats))
        {
            var freeableHands = 0;
            if (TryComp<HandsComponent>(puller, out var hands))
                freeableHands = _handsSystem.CountFreeableHands((puller.Owner, hands));

            if (freeableHands < stats.RequiredHands)
                return false;

            // We dont need to start DoAfter if stage sets instantly
            if (stats.SetStageTime <= 0)
                return direction > 0 ? TryIncreaseGrabStage(puller, pullable) : TryLowerGrabStage(puller, pullable, puller.Owner);
        }

        if (!_net.IsServer)
            return true;
        var time = puller.Comp.GrabStats[targetStage].SetStageTime;

        var doAfterArgs = new DoAfterArgs(EntityManager, puller, time, new SetGrabStageDoAfterEvent(direction), puller)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            BreakOnHandChange = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = true,
            BlockDuplicate = true
        };

        return _doAfter.TryStartDoAfter(doAfterArgs, out puller.Comp.StageIncreaseDoAfter);
    }

    public bool TryEscapeFromGrab(Entity<PullableComponent> pullable, Entity<PullerComponent?> puller)
    {
        if (!Resolve(puller, ref puller.Comp))
            return false;
        if (puller.Comp.Pulling != pullable.Owner)
            return false;
        if (pullable.Comp.EscapeAttemptDoAfter.HasValue)
            return true;

        if (puller.Comp.Stage == GrabStage.None)
        {
            TryStopPull(pullable, pullable.Comp, pullable);
            return true;
        }

        if (pullable.Comp.LastEscapeAttempt + TimeSpan.FromSeconds(puller.Comp.EscapeAttemptDelay) > _timing.CurTime)
            return false;

        pullable.Comp.LastEscapeAttempt = _timing.CurTime;

        // Do escape effects
        _popup.PopupPredicted(Loc.GetString("grab-escape-attempt-popup-self"), Loc.GetString("grab-escape-attempt-popup-others", ("pullable", Identity.Entity(pullable, EntityManager))), pullable, pullable, PopupType.Small);

        if (!_net.IsServer)
            return true;

        var doAfterArgs = new DoAfterArgs(EntityManager, pullable, puller.Comp.GrabStats[puller.Comp.Stage].EscapeAttemptTime, new GrabEscapeDoAfterEvent(), pullable)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            BreakOnHandChange = false,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = true,
            BlockDuplicate = true
        };

        _adminLogger.Add(LogType.Grab, LogImpact.Low, $"{ToPrettyString(pullable.Owner)} started to escage from {ToPrettyString(pullable.Owner)}'s grab");
        return _doAfter.TryStartDoAfter(doAfterArgs, out pullable.Comp.EscapeAttemptDoAfter);
    }

    public void ResetGrabStageTime(Entity<PullerComponent> puller)
    {
        puller.Comp.NextStageChange -= TimeSpan.FromSeconds(2);
    }

    public virtual void Throw(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, EntityCoordinates coords)
    {
    }
    #endregion
}