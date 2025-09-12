using Content.Shared.ADT.Grab;

namespace Content.Shared.GrabProtection;

public sealed class GrabProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabProtectionComponent, ModifyGrabStageTimeEvent>(OnGrab);
    }
    private void OnGrab(EntityUid uid, GrabProtectionComponent component, ref ModifyGrabStageTimeEvent args)
    {
        args.Cancelled = true;
    }
}
