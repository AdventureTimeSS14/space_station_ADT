using Content.Server.Body.Components;
using Content.Shared.ADT.Traits;

namespace Content.Server.ADT.Hemophilia;

public sealed partial class HemophiliaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HemophiliaComponent, MapInitEvent>(OnInitHemophilia);
        SubscribeLocalEvent<HemophiliaComponent, ComponentShutdown>(OnShutdown);

    }

    private void OnInitHemophilia(EntityUid uid, HemophiliaComponent component, MapInitEvent args)
    {
        if (TryComp<BloodstreamComponent>(uid, out var bldcomp))
        {
            bldcomp.BleedReductionAmount *= component.Modifier;
        }
    }
    private void OnShutdown(EntityUid uid, HemophiliaComponent component, ComponentShutdown args)
    {
        if (TryComp<BloodstreamComponent>(uid, out var bldcomp))
        {
            bldcomp.BleedReductionAmount /= component.Modifier;
        }
    }
}
