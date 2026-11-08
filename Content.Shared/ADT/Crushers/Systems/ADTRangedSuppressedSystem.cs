using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class ADTRangedSuppressedSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRangedSuppressedComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    public void Suppress(EntityUid uid, TimeSpan duration)
    {
        var comp = EnsureComp<ADTRangedSuppressedComponent>(uid);
        var now = _timing.CurTime;
        comp.Until = (comp.Until > now ? comp.Until : now) + duration;
        Dirty(uid, comp);
    }

    public bool IsSuppressed(EntityUid uid)
    {
        return TryComp<ADTRangedSuppressedComponent>(uid, out var comp) && _timing.CurTime < comp.Until;
    }

    private void OnShotAttempted(Entity<ADTRangedSuppressedComponent> ent, ref ShotAttemptedEvent args)
    {
        if (_timing.CurTime < ent.Comp.Until)
            args.Cancel();
    }
}
