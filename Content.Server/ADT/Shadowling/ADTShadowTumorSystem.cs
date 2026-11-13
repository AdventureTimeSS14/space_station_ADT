using Content.Server.ADT.Body;
using Content.Goobstation.Shared.LightDetection.Components;
using Content.Shared.ADT.Shadowling;
using Content.Shared.Body;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shadowling;

public sealed class ADTShadowTumorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShadowTumorComponent, OrganGotRemovedEvent>(OnTumorRemoved, before: [typeof(OrganGrantComponentsSystem)]);
    }

    private void OnTumorRemoved(Entity<ADTShadowTumorComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!HasComp<ADTShadowlingThrallComponent>(args.Target))
            return;

        _popup.PopupEntity(Loc.GetString("shadowling-dethrall-freed"), args.Target, args.Target, PopupType.LargeCaution);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTShadowTumorComponent, LightDetectionComponent>();
        while (query.MoveNext(out var uid, out var tumor, out var detection))
        {
            if (tumor.NextUpdate > _timing.CurTime)
                continue;

            if (_organQuery.TryComp(uid, out var organ) && organ.Body != null)
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
