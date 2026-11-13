using Content.Shared.ADT.Crushers;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Crushers.Systems;
using Content.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Crushers;

public sealed class CrusherSpearSystem : SharedCrusherSpearSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    private static readonly TimeSpan RequestInterval = TimeSpan.FromSeconds(0.3);

    public bool TryRequestThrow(EntityUid weapon, EntityCoordinates target)
    {
        if (!TryComp<ADTCrusherSpearThrowComponent>(weapon, out var spear))
            return false;

        var now = _timing.CurTime;

        if (spear.Thrower != null || now < spear.NextRequest)
            return true;

        if (TryComp<UseDelayComponent>(weapon, out var delay) && _useDelay.IsDelayed((weapon, delay), spear.DelayId))
            return true;

        spear.NextRequest = now + RequestInterval;
        RaiseNetworkEvent(new ADTCrusherSpearThrowRequestEvent(GetNetEntity(weapon), GetNetCoordinates(target)));
        return true;
    }
}
