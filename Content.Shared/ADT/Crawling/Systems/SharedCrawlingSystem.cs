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
/// <summary>
/// после изменений от визардов, эта система стала костыльной, потому на TODO перекинуть её куда-то
/// </summary>
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
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandingStateComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<StandingStateComponent, DownedEvent>(OnDown);
        SubscribeLocalEvent<StandingStateComponent, GetExplosionResistanceEvent>(OnExplosion);

        SubscribeLocalEvent<StandingStateComponent, AttemptClimbEvent>(OnClimbAttemptWhileCrawling);
    }

    private void OnDown(EntityUid uid, StandingStateComponent component, DownedEvent args)
    {
        _appearance.SetData(uid, CrawlingVisuals.Standing, false);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, true);
    }

    private void OnExplosion(EntityUid uid, StandingStateComponent component, GetExplosionResistanceEvent args)
    {
        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
    }

    private void OnStood(EntityUid uid, StandingStateComponent comp, ref StandAttemptEvent args)
    {
        var lookup = _lookup.GetEntitiesInRange<ClimbableComponent>(Transform(uid).Coordinates, 0.25f);
        if (lookup.Count != 0)
        {
            _bonk.TryBonk(uid, lookup.First(), source: uid);
            args.Cancel();
            return;
        }
        _appearance.SetData(uid, CrawlingVisuals.Standing, true);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, false);
    }

    private void OnClimbAttemptWhileCrawling(EntityUid uid, StandingStateComponent comp, ref AttemptClimbEvent args)
    {
        if (args.Cancelled || comp.Standing)
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
