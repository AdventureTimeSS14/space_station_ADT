using Content.Shared.ADT.ImplantActivationVision;
using Robust.Shared.Timing;

namespace Content.Server.ADT.ImplantActivationVision;

public sealed class ImplantActivationVisionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ImplantActivationVisionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > curTime)
                continue;

            RemCompDeferred<ImplantActivationVisionComponent>(uid);
        }
    }
}
