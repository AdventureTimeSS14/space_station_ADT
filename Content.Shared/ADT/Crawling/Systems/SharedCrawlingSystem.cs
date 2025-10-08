using Content.Shared.Explosion;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Content.Shared.Climbing.Events;

namespace Content.Shared.ADT.Crawling;
/// <summary>
/// после изменений от визардов, эта система стала костыльной, потому на TODO перекинуть её куда-то
/// </summary>
public abstract class SharedCrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StandingStateComponent, GetExplosionResistanceEvent>(OnExplosion);
        SubscribeLocalEvent<StandingStateComponent, AttemptClimbEvent>(OnClimbAttemptWhileCrawling);
    }

    private void OnExplosion(EntityUid uid, StandingStateComponent component, GetExplosionResistanceEvent args)
    {
        _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);
    }

    private void OnClimbAttemptWhileCrawling(EntityUid uid, StandingStateComponent comp, ref AttemptClimbEvent args)
    {
        if (args.Cancelled || comp.Standing)
            return;

        args.Cancelled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class CrawlingKeybindEvent
{
}
