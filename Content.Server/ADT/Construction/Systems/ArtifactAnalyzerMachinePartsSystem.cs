using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Server.ADT.Construction.Systems;

public sealed class ArtifactAnalyzerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactAnalyzerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ArtifactAnalyzerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, ArtifactAnalyzerComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<ArtifactAnalyzerUpgradeComponent>(uid, out var upgrade))
            return;

        var laserTier = args.GetPartRating(MachinePartIds.MicroLaser, 1f);
        upgrade.PointMultiplier = RefreshPartsEvent.GetTierMultiplier(laserTier, 0.10f);
    }

    private void OnUpgradeExamine(EntityUid uid, ArtifactAnalyzerComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<ArtifactAnalyzerUpgradeComponent>(uid, out var upgrade))
            return;

        args.AddPercentageUpgrade("machine-upgrade-artifact-points", upgrade.PointMultiplier, benefit: true);
    }
}
