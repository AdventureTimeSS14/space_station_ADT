using Content.Server.Anomaly;
using Content.Server.Anomaly.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;
public sealed class AnomalyVesselMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyVesselComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AnomalyVesselComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, AnomalyVesselComponent component, RefreshPartsEvent args)
    {
        _anomaly.SetPointMultiplier(uid, args.GetStatMultiplier(MachineStat.Output), component);
    }

    private static void OnUpgradeExamine(EntityUid uid, AnomalyVesselComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-anomaly-points", component.PointMultiplier, benefit: true);
    }
}
