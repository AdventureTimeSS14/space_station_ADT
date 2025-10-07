using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;
using Content.Shared.Alert;
using Content.Shared.Climbing.Components;
using Content.Shared.ADT.Grab;
using Content.Shared.Climbing.Systems;
using Content.Shared.CombatMode;
using System.Numerics;
using Content.Shared.Throwing;
using Content.Shared.Buckle;
using Robust.Shared.Physics.Components;
using Content.Shared.Gravity;
using Content.Shared.Coordinates;
using Content.Shared.Climbing.Events;

namespace Content.Shared.ADT.Crawling;

public abstract class SharedCrawlingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly BonkSystem _bonk = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, CrawlStandupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CrawlerComponent, StandAttemptEvent>(OnStandUp);
        SubscribeLocalEvent<CrawlerComponent, DownAttemptEvent>(OnFall);
        SubscribeLocalEvent<CrawlerComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CrawlerComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<CrawlerComponent, CrawlingKeybindEvent>(ToggleCrawling);

        SubscribeLocalEvent<CrawlingComponent, ComponentInit>(OnCrawlSlowdownInit);
        SubscribeLocalEvent<CrawlingComponent, ComponentShutdown>(OnCrawlSlowRemove);
        SubscribeLocalEvent<CrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<CrawlerComponent, MapInitEvent>(OnCrawlerInit);

        SubscribeLocalEvent<ClimbableComponent, AttemptClimbEvent>(OnClimbAttemptWhileCrawling);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawling,
                InputCmdHandler.FromDelegate(ToggleCrawlingKeybind, handle: false))
            .Register<SharedCrawlingSystem>();
    }

    private void ToggleCrawlingKeybind(ICommonSession? session)
    {
        if (session?.AttachedEntity == null)
            return;
        var ev = new CrawlingKeybindEvent();
        RaiseLocalEvent(session.AttachedEntity.Value, ev);
    }

    private void ToggleCrawling(EntityUid uid, CrawlerComponent component, CrawlingKeybindEvent args)
    {
        // should not start do after if the player is buckled to anything. Either that or we should unbuckle player. I'd prefer not starting do after at all
        if (_buckle.IsBuckled(uid))
            return;

        // checks players standing state, downing player if they are standding and starts doafter with standing up if they are downed
        switch (_standing.IsDown(uid))
        {
            case false:

                var tablesNearby = _lookup.GetEntitiesInRange<ClimbableComponent>(Transform(uid).Coordinates, 0.25f);

                if (tablesNearby.Count > 0)
                    return;
                _standing.Down(uid, dropHeldItems: false);
                // дропкик перенесён в отдельный компонент. По крайней мере пока-что
                // if (!TryComp<CombatModeComponent>(uid, out var combatMode) ||
                //     combatMode.IsInCombatMode)
                // {
                //     var targetTile = Vector2.Zero;

                //     if (TryComp<PhysicsComponent>(uid, out var physics) && !_gravity.IsWeightless(uid))
                //     {
                //         var velocity = physics.LinearVelocity;

                //         if (velocity.LengthSquared() > 0)
                //         {
                //             var direction = velocity.Normalized();

                //             var currentPosition = uid.ToCoordinates();

                //             var targetTileX = currentPosition.X + direction.X * 1;
                //             var targetTileY = currentPosition.Y + direction.Y * 1;

                //             int tileX = (int)MathF.Round(targetTileX);
                //             int tileY = (int)MathF.Round(targetTileY);

                //             targetTile = new Vector2(tileX, tileY);
                //         }
                //     }
                //     EnsureComp<GrabThrownComponent>(uid);
                //     _throwing.TryThrow(uid, targetTile, 8, animated: false, playSound: false, doSpin: false);
                // }
                break;
            case true:
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.StandUpTime,
                    new CrawlStandupDoAfterEvent(),
                    uid, used: uid)
                {
                    BreakOnDamage = true
                });
                break;
        }
    }

    private void OnDoAfter(EntityUid uid, CrawlerComponent component, CrawlStandupDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var lookup = _lookup.GetEntitiesInRange<ClimbableComponent>(Transform(uid).Coordinates, 0.25f);
        if (lookup.Count != 0)
        {
            _bonk.TryBonk(uid, lookup.First(), source: uid);
            return;
        }

        _standing.Stand(uid);
    }

    private void OnStandUp(EntityUid uid, CrawlerComponent component, StandAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        RemCompDeferred<CrawlingComponent>(uid);
        _alerts.ClearAlert(uid, component.CrawlingAlert);
    }

    private void OnFall(EntityUid uid, CrawlerComponent component, DownAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        _alerts.ShowAlert(uid, component.CrawlingAlert);

        EnsureComp<CrawlingComponent>(uid);
    }

    private void OnStunned(EntityUid uid, CrawlerComponent component, StunnedEvent args)
    {
        EnsureComp<CrawlingComponent>(uid);
        _alerts.ShowAlert(uid, component.CrawlingAlert);
    }

    private void OnGetExplosionResistance(EntityUid uid, CrawlerComponent component,
        ref GetExplosionResistanceEvent args)
    {
        // fall on explosion damage and lower explosion damage of crawling
        if (_standing.IsDown(uid))
            args.DamageCoefficient *= component.DownedDamageCoefficient;
        else
        {
            var ev = new ExplosionDownAttemptEvent(args.ExplosionPrototype);
            RaiseLocalEvent(uid, ref ev);
            if (!ev.Cancelled)
                _standing.Down(uid);
        }
    }

    private void OnCrawlSlowdownInit(EntityUid uid, CrawlingComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _appearance.SetData(uid, CrawlingVisuals.Standing, false);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, true);
    }

    private void OnCrawlSlowRemove(EntityUid uid, CrawlingComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, false);
        _appearance.SetData(uid, CrawlingVisuals.Standing, true);
    }

    private void OnRefreshMovespeed(EntityUid uid, CrawlingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnCrawlerInit(EntityUid uid, CrawlerComponent comp, MapInitEvent args)
    {
        _appearance.SetData(uid, CrawlingVisuals.Standing, true);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, false);
    }

    private void OnClimbAttemptWhileCrawling(EntityUid uid, ClimbableComponent comp, ref AttemptClimbEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<CrawlingComponent>(args.Climber))
            args.Cancelled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CrawlStandupDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CrawlingKeybindEvent
{
}


[NetSerializable, Serializable]
public enum CrawlingVisuals : byte
{
    Standing,
    Crawling,
}
