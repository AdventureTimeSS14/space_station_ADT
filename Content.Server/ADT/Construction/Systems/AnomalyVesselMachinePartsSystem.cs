using Content.Server.Anomaly;
using Content.Server.Anomaly.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;
public sealed class AnomalyVesselMachinePartsSystem : EntitySystem
{
    private static readonly float[] TierPointMultipliers = [1.10f, 1.15f, 1.20f, 1.30f];

    [Dependency] private readonly AnomalySystem _anomaly = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyVesselComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AnomalyVesselComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, AnomalyVesselComponent component, RefreshPartsEvent args)
    {
        var tier = (int) Math.Clamp(MathF.Round(args.GetPartRating(MachinePartIds.MicroLaser, 1f)), 1, TierPointMultipliers.Length);
        _anomaly.SetPointMultiplier(uid, TierPointMultipliers[tier - 1], component);
    }

    private static void OnUpgradeExamine(EntityUid uid, AnomalyVesselComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-anomaly-points", component.PointMultiplier, benefit: true);
    }
}
