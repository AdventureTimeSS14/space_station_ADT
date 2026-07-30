using System.Diagnostics.CodeAnalysis;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.ADT.Hierophant;
using Content.Shared.ADT.Hierophant.Effects;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hierophant;

public sealed class HierophantSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HierophantAttacksSystem _attacks = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MegafaunaSystem _megafauna = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<HierophantComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<HierophantComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<HierophantComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HierophantComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<HierophantComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<HierophantComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpawnedBeacon = Spawn(ent.Comp.BeaconProto, _transform.GetMoverCoordinates(ent.Owner));
        ent.Comp.NextArenaAt = _timing.CurTime;

        foreach (var action in ent.Comp.Actions)
        {
            _actions.AddAction(ent.Owner, action);
        }
    }

    private void OnShutdown(Entity<HierophantComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.SpawnedBeacon != null)
            QueueDel(ent.Comp.SpawnedBeacon.Value);
    }

    private void OnMove(Entity<HierophantComponent> ent, ref MoveEvent args)
    {
        if (args.ParentChanged)
            return;

        if (args.OldPosition == args.NewPosition)
            return;

        if (_mobState.IsDead(ent))
            return;

        if (_timing.CurTime - ent.Comp.LastMoveSound >= ent.Comp.MoveSoundCooldown)
        {
            ent.Comp.LastMoveSound = _timing.CurTime;
            _audio.PlayPvs(ent.Comp.MoveSound, ent.Owner, AudioParams.Default.WithVolume(-4f));
        }

        var map = _transform.ToMapCoordinates(args.NewPosition);
        if (map.MapId == MapId.Nullspace)
            return;

        if (ent.Comp.LastTrailPosition is { } last
            && last.MapId == map.MapId
            && (last.Position - map.Position).LengthSquared() < ent.Comp.TrailStepDistance * ent.Comp.TrailStepDistance)
            return;

        ent.Comp.LastTrailPosition = map;

        var squares = Spawn(ent.Comp.SquaresProto, args.OldPosition);
        var delta = args.NewPosition.Position - args.OldPosition.Position;

        if (delta != default)
            _transform.SetLocalRotation(squares, delta.ToWorldAngle());

        if (TryGetTarget(ent.Owner, out var target))
            _attacks.ArenaTrap(ent, target.Value);
    }

    private void OnDamageChanged(Entity<HierophantComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.GetTotal() <= FixedPoint2.Zero)
            return;

        if (!ent.Comp.Blinking)
        {
            ent.Comp.DidReset = false;
            ent.Comp.TimeoutAt = null;
        }

        CalculateRage(ent);
    }

    private void OnMobStateChanged(Entity<HierophantComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _megafauna.Say(ent.Owner, "adt-hierophant-death");

        _attacks.Burst(ent, _transform.GetMoverCoordinates(ent.Owner), 10);
        _attacks.CollapseWalls(ent.Owner);

        SetBlinking(ent, false);
        _appearance.SetData(ent.Owner, HierophantVisuals.Attacking, false);
    }

    private void OnRefreshSpeed(Entity<HierophantComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Blinking)
            args.ModifySpeed(0f, 0f);
        else if (ent.Comp.Enraged)
            args.ModifySpeed(2f, 2f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        RunSequences(now);
        RunTimeouts(now);
    }

    private void RunSequences(TimeSpan now)
    {
        var query = EntityQueryEnumerator<HierophantSequenceComponent, HierophantComponent>();

        while (query.MoveNext(out var uid, out var sequence, out var boss))
        {
            for (var i = 0; i < sequence.Queue.Count; i++)
            {
                var step = sequence.Queue[i];

                if (now < step.ExecuteAt)
                    continue;

                _attacks.ExecuteStep((uid, boss), step);
                sequence.Queue.RemoveAt(i);
                i--;
            }

            if (sequence.Queue.Count == 0)
                RemCompDeferred<HierophantSequenceComponent>(uid);
        }
    }

    private void RunTimeouts(TimeSpan now)
    {
        var query = EntityQueryEnumerator<HierophantComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.SpawnedBeacon == null || TerminatingOrDeleted(comp.SpawnedBeacon.Value))
                continue;

            if (_mobState.IsDead(uid))
                continue;

            if (HasComp<ActorComponent>(uid))
                continue;

            var beaconCoords = _transform.GetMapCoordinates(comp.SpawnedBeacon.Value);
            var ourCoords = _transform.GetMapCoordinates(uid);
            var atBeacon = beaconCoords.MapId == ourCoords.MapId
                           && (beaconCoords.Position - ourCoords.Position).LengthSquared() < 0.5f;

            if (TryGetTarget(uid, out _) || atBeacon)
            {
                comp.TimeoutAt = null;
                continue;
            }

            comp.TimeoutAt ??= now + comp.TimeoutTime;

            if (now < comp.TimeoutAt || comp.DidReset)
                continue;

            comp.DidReset = true;
            comp.TimeoutAt = null;

            _megafauna.Say(uid, "adt-hierophant-returning");
            _attacks.Blink((uid, comp), beaconCoords);
            HealOnReturn((uid, comp));
        }
    }

    private void HealOnReturn(Entity<HierophantComponent> ent)
    {
        if (!TryComp<MobThresholdsComponent>(ent, out var thresholds))
            return;

        var maxHealth = 0f;
        foreach (var (damage, _) in thresholds.Thresholds)
        {
            if ((float) damage > maxHealth)
                maxHealth = (float) damage;
        }

        var damageTaken = (float) _damageable.GetTotalDamage(ent.Owner);
        var heal = Math.Max(damageTaken * 0.5f, Math.Min(damageTaken, maxHealth * 0.1f));

        if (heal <= 0f)
            return;

        _damageable.TryChangeDamage(ent.Owner, -ent.Comp.SelfRepair * heal, true);

        var remaining = damageTaken - heal;
        var line = remaining <= maxHealth * 0.1f
            ? "adt-hierophant-repairs-complete"
            : "adt-hierophant-repairs-compromised";

        _megafauna.Say(ent.Owner, line);
    }

    public void CalculateRage(Entity<HierophantComponent> ent)
    {
        ent.Comp.DidReset = false;

        var damageTaken = (float) _damageable.GetTotalDamage(ent.Owner);
        var floor = ent.Comp.Enraged ? 40f : 0f;
        ent.Comp.AngerModifier = Math.Clamp(Math.Max(damageTaken / 42f, floor), 0f, 50f);

        ent.Comp.BurstRange = 3 + (int) MathF.Round(ent.Comp.AngerModifier * 0.08f);
        ent.Comp.BeamRange = 5 + (int) MathF.Round(ent.Comp.AngerModifier * 0.12f);
    }

    public void SetBlinking(Entity<HierophantComponent> ent, bool value)
    {
        if (ent.Comp.Blinking == value)
            return;

        ent.Comp.Blinking = value;
        _speed.RefreshMovementSpeedModifiers(ent.Owner);

        if (TryComp<HTNComponent>(ent.Owner, out var htn))
        {
            if (value)
                htn.Blackboard.SetValue("HierophantBlinking", true);
            else
                htn.Blackboard.Remove<bool>("HierophantBlinking");
        }

        Dirty(ent);
    }

    public void SetDense(EntityUid uid, bool value)
    {
        _physics.SetCanCollide(uid, value);
    }

    public bool TryGetTarget(EntityUid uid, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (!TryComp<HTNComponent>(uid, out var htn))
            return false;

        if (!htn.Blackboard.TryGetValue<EntityUid>("Target", out var found, EntityManager))
            return false;

        if (TerminatingOrDeleted(found))
            return false;

        target = found;
        return true;
    }

}
