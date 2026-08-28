using Content.Goobstation.Shared.LightDetection.Components;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shadowling;

public sealed class ADTShadowTumorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTShadowTumorComponent, LightDetectionComponent>();
        while (query.MoveNext(out var uid, out var tumor, out var detection))
        {
            if (tumor.NextUpdate > _timing.CurTime)
                continue;

            tumor.NextUpdate = _timing.CurTime + tumor.UpdateInterval;

            if (detection.OnLight)
            {
                tumor.Integrity--;
                Dirty(uid, tumor);

                if (tumor.Integrity <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("shadowling-tumor-collapses", ("tumor", uid)), uid);
                    QueueDel(uid);
                }

                continue;
            }

            if (tumor.Integrity >= tumor.MaxIntegrity)
                continue;

            tumor.Integrity++;
            Dirty(uid, tumor);
        }
    }
}
