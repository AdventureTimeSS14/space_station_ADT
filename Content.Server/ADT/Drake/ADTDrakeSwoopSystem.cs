using System.Numerics;
using Content.Server.NPC.HTN;
using Content.Shared.ActionBlocker;
using Content.Shared.ADT.Drake;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeSwoopSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ADTDrakeArenaSystem _arena = default!;
    [Dependency] private readonly ADTDrakeSystem _drake = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private const string SwoopingKey = "DrakeSwooping";

    private const int MaxStepsPerTick = 8;

    private readonly HashSet<EntityUid> _landingHits = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeSwoopComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTDrakeSwoopComponent, UpdateCanMoveEvent>(OnUpdateCanMove);
        SubscribeLocalEvent<ADTDrakeSwoopComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<ADTDrakeComponent, ADTDrakeArenaFinishedEvent>(OnArenaFinished);
    }

    private void OnShutdown(Entity<ADTDrakeSwoopComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        _physics.SetCanCollide(ent, true);
        _blocker.UpdateCanMove(ent);

        if (TryComp<HTNComponent>(ent, out var htn))
            htn.Blackboard.Remove<bool>(SwoopingKey);
    }

    private void OnUpdateCanMove(Entity<ADTDrakeSwoopComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnBeforeDamageChanged(Entity<ADTDrakeSwoopComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (ent.Comp.Invulnerable)
            args.Cancelled = true;
    }

    public bool IsSwooping(EntityUid uid)
    {
        return HasComp<ADTDrakeSwoopComponent>(uid);
    }

    public bool TrySwoop(
        Entity<ADTDrakeComponent> ent,
        EntityUid target,
        bool lavaArena,
        TimeSpan cooldown,
        ADTDrakeSwoopFollowUp followUp)
    {
        if (_mobState.IsDead(ent) || IsSwooping(ent))
            return false;

        if (!TryGetTile(ent, out var gridUid, out var grid, out var origin))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        var targetTile = _map.TileIndicesFor(gridUid, grid, _transform.GetMapCoordinates(target));

        bool negative;
        if (targetTile.X < origin.X)
            negative = true;
        else if (targetTile.X > origin.X)
            negative = false;
        else
            negative = _random.Prob(0.5f);

        Spawn(negative ? ent.Comp.SwoopRiseLeftProto : ent.Comp.SwoopRiseRightProto, Transform(ent).Coordinates);

        _drake.Aggro(ent, target);

        var swoop = EnsureComp<ADTDrakeSwoopComponent>(ent);
        swoop.Phase = ADTDrakeSwoopPhase.Rising;
        swoop.PhaseEndsAt = _timing.CurTime + ent.Comp.SwoopRiseTime;
        swoop.Target = target;
        swoop.LavaArena = lavaArena;
        swoop.LavaSuccess = true;
        swoop.Cooldown = cooldown;
        swoop.FollowUp = followUp;
        swoop.InitialX = origin.X;
        swoop.Negative = !negative;
        Dirty(ent, swoop);

        _physics.SetCanCollide(ent, false);
        _blocker.UpdateCanMove(ent);

        if (TryComp<HTNComponent>(ent, out var htn))
            htn.Blackboard.SetValue(SwoopingKey, true);

        _popup.PopupEntity(Loc.GetString("adt-drake-swoop-rise", ("drake", ent.Owner)), ent, PopupType.LargeCaution);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDrakeSwoopComponent, ADTDrakeComponent>();

        while (query.MoveNext(out var uid, out var swoop, out var drake))
        {
            if (_mobState.IsDead(uid))
            {
                RemCompDeferred<ADTDrakeSwoopComponent>(uid);
                continue;
            }

            var ent = new Entity<ADTDrakeComponent>(uid, drake);

            switch (swoop.Phase)
            {
                case ADTDrakeSwoopPhase.Rising:
                    if (now >= swoop.PhaseEndsAt)
                        SetPhase(uid, swoop, ADTDrakeSwoopPhase.Ascending, now + drake.SwoopAscendTime);
                    break;

                case ADTDrakeSwoopPhase.Ascending:
                    if (now < swoop.PhaseEndsAt)
                        break;

                    SetPhase(uid, swoop, ADTDrakeSwoopPhase.Chasing, now);
                    swoop.NextStepAt = now;
                    break;

                case ADTDrakeSwoopPhase.Chasing:
                    UpdateChase(ent, swoop, now);
                    break;

                case ADTDrakeSwoopPhase.Arena:
                    break;

                case ADTDrakeSwoopPhase.Descending:
                    if (now >= swoop.PhaseEndsAt)
                        Land(ent, swoop, now);
                    break;

                case ADTDrakeSwoopPhase.Landed:
                    if (now >= swoop.PhaseEndsAt)
                        Finish(ent, swoop);
                    break;
            }
        }
    }

    private void UpdateChase(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop, TimeSpan now)
    {
        for (var i = 0; i < MaxStepsPerTick && now >= swoop.NextStepAt; i++)
        {
            swoop.NextStepAt += ent.Comp.SwoopStepDelay;

            if (!TryStepTowardsTarget(ent, swoop))
            {
                EndChase(ent, swoop, now);
                return;
            }
        }
    }

    private bool TryStepTowardsTarget(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop)
    {
        if (swoop.Target is not { } target || TerminatingOrDeleted(target))
            return false;

        if (!TryGetTile(ent, out var gridUid, out var grid, out var origin))
            return false;

        var targetCoords = _transform.GetMapCoordinates(target);
        if (targetCoords.MapId != _transform.GetMapCoordinates(ent).MapId)
            return false;

        var targetTile = _map.TileIndicesFor(gridUid, grid, targetCoords);
        if (targetTile == origin)
            return false;

        var step = new Vector2i(Math.Sign(targetTile.X - origin.X), Math.Sign(targetTile.Y - origin.Y));
        _transform.SetCoordinates(ent, _map.GridTileToLocal(gridUid, grid, origin + step));
        return true;
    }

    private void EndChase(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop, TimeSpan now)
    {
        if (!swoop.LavaArena)
        {
            StartDescent(ent, swoop, now);
            return;
        }

        SetPhase(ent, swoop, ADTDrakeSwoopPhase.Arena, now);
        _arena.StartArena(ent, swoop.Target);
    }

    private void OnArenaFinished(Entity<ADTDrakeComponent> ent, ref ADTDrakeArenaFinishedEvent args)
    {
        if (!TryComp<ADTDrakeSwoopComponent>(ent, out var swoop) || swoop.Phase != ADTDrakeSwoopPhase.Arena)
            return;

        swoop.LavaSuccess = args.Success;
        StartDescent(ent, swoop, _timing.CurTime);
    }

    public void StartDescent(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop, TimeSpan now)
    {
        if (TryGetTile(ent, out _, out _, out var tile))
        {
            var range = ent.Comp.SwoopDirectionChangeRange;

            if (swoop.Negative)
            {
                if (tile.X >= swoop.InitialX + 1 && tile.X <= swoop.InitialX + range)
                    swoop.Negative = false;
            }
            else
            {
                if (tile.X >= swoop.InitialX - range && tile.X <= swoop.InitialX - 1)
                    swoop.Negative = true;
            }
        }

        _transform.SetWorldRotation(ent, Direction.South.ToAngle());

        var coords = Transform(ent).Coordinates;
        Spawn(swoop.Negative ? ent.Comp.SwoopLandLeftProto : ent.Comp.SwoopLandRightProto, coords);
        Spawn(ent.Comp.SwoopLandingMarkerProto, coords);

        SetPhase(ent, swoop, ADTDrakeSwoopPhase.Descending, now + ent.Comp.SwoopDescentTime);
    }

    private void Land(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop, TimeSpan now)
    {
        SetPhase(ent, swoop, ADTDrakeSwoopPhase.Landed, now + ent.Comp.SwoopLandedTime);

        _audio.PlayPvs(ent.Comp.SwoopLandSound, ent);

        if (TryGetTile(ent, out var gridUid, out var grid, out var origin))
            HitLandingArea(ent, gridUid, grid, origin);

        ShakeCameras(ent);
        _physics.SetCanCollide(ent, true);
    }

    private void HitLandingArea(Entity<ADTDrakeComponent> ent, EntityUid gridUid, MapGridComponent grid, Vector2i origin)
    {
        _landingHits.Clear();

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var tile = origin + new Vector2i(x, y);

                foreach (var target in _lookup.GetLocalEntitiesIntersecting(gridUid, tile, gridComp: grid))
                {
                    if (target == ent.Owner || !_landingHits.Add(target))
                        continue;

                    if (HasComp<MobStateComponent>(target))
                    {
                        HitMob(ent, target, new Vector2i(x, y));
                        continue;
                    }

                    if (HasComp<MechComponent>(target))
                        Damage(ent, target, ent.Comp.SwoopMechDamage);
                }
            }
        }
    }

    private void HitMob(Entity<ADTDrakeComponent> ent, EntityUid target, Vector2i offset)
    {
        if (!_mobState.IsAlive(target))
        {
            _popup.PopupEntity(Loc.GetString("adt-drake-swoop-crush", ("drake", ent.Owner), ("target", target)), ent, PopupType.LargeCaution);

            if (_gibbing.Gib(target, true, ent.Owner).Count == 0)
                QueueDel(target);

            return;
        }

        Damage(ent, target, ent.Comp.SwoopDamage);

        if (TerminatingOrDeleted(target))
            return;

        var direction = offset == Vector2i.Zero
            ? _random.Pick(AllDirections)
            : new Vector2(offset.X, offset.Y);

        _throwing.TryThrow(target, Vector2.Normalize(direction) * ent.Comp.SwoopThrowRange, user: ent.Owner);
        _popup.PopupEntity(Loc.GetString("adt-drake-swoop-throw", ("drake", ent.Owner), ("target", target)), target, PopupType.Medium);
    }

    private void ShakeCameras(Entity<ADTDrakeComponent> ent)
    {
        var center = _transform.GetMapCoordinates(ent);

        foreach (var actor in _lookup.GetEntitiesInRange<ActorComponent>(center, ent.Comp.SwoopShakeRange))
        {
            var delta = _transform.GetMapCoordinates(actor).Position - center.Position;
            if (delta.LengthSquared() < 0.0001f)
                delta = new Vector2(0.01f, 0f);

            _recoil.KickCamera(actor, Vector2.Normalize(delta) * ent.Comp.SwoopShakeStrength);
        }
    }

    private void Finish(Entity<ADTDrakeComponent> ent, ADTDrakeSwoopComponent swoop)
    {
        var ev = new ADTDrakeSwoopFinishedEvent(swoop.FollowUp, swoop.Target, swoop.LavaSuccess);

        RemCompDeferred<ADTDrakeSwoopComponent>(ent);
        _drake.SetRecoveryTime(ent, swoop.Cooldown);

        RaiseLocalEvent(ent, ref ev);
    }

    private void SetPhase(EntityUid uid, ADTDrakeSwoopComponent swoop, ADTDrakeSwoopPhase phase, TimeSpan endsAt)
    {
        swoop.Phase = phase;
        swoop.PhaseEndsAt = endsAt;
        Dirty(uid, swoop);
    }

    private void Damage(Entity<ADTDrakeComponent> ent, EntityUid target, float amount)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", amount);
        _damageable.TryChangeDamage(target, damage, origin: ent.Owner);
    }

    private bool TryGetTile(EntityUid uid, out EntityUid gridUid, out MapGridComponent grid, out Vector2i tile)
    {
        gridUid = default;
        grid = default!;
        tile = default;

        var xform = Transform(uid);

        if (xform.GridUid is not { } found || !TryComp(found, out MapGridComponent? foundGrid))
            return false;

        gridUid = found;
        grid = foundGrid;
        tile = _map.TileIndicesFor(found, foundGrid, xform.Coordinates);
        return true;
    }

    private static readonly Vector2[] AllDirections =
    {
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
        new(1, 1),
        new(1, -1),
        new(-1, 1),
        new(-1, -1),
    };
}

[ByRefEvent]
public readonly record struct ADTDrakeSwoopFinishedEvent(ADTDrakeSwoopFollowUp FollowUp, EntityUid? Target, bool LavaSuccess);
