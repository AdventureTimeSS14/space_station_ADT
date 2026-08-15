using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class OreProcessorUpgradeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreProcessorUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<OreProcessorUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, OreProcessorUpgradeComponent component, RefreshPartsEvent args)
    {
        var laserTier = args.GetPartRating(MachinePartIds.MicroLaser, 1f);
        var binTier = args.GetPartRating(MachinePartIds.MatterBin, 1f);

        component.OutputMultiplier = RefreshPartsEvent.GetTierMultiplier(laserTier, 0.20f)
            * RefreshPartsEvent.GetTierMultiplier(binTier, -0.10f);

        component.PointsMultiplier = RefreshPartsEvent.GetTierMultiplier(binTier, 0.20f)
            * RefreshPartsEvent.GetTierMultiplier(laserTier, -0.10f);
    }

    private static void OnUpgradeExamine(EntityUid uid, OreProcessorUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-ore-output", component.OutputMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-ore-points", component.PointsMultiplier, benefit: true);
    }
}
