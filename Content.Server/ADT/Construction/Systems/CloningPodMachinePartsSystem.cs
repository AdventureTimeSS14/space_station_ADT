using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Cloning;

namespace Content.Server.ADT.Construction.Systems;

public sealed class CloningPodMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CloningPodComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<CloningPodComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, CloningPodComponent component, RefreshPartsEvent args)
    {
        var servoTier = args.GetPartRating(MachinePartIds.Servo, 1f);
        var scanTier = args.GetPartRating(MachinePartIds.ScanningModule, 1f);

        // Базовые части (тир 1) без прибавок
        component.SpeedMultiplier = servoTier;
        component.CloningSafety = scanTier;
    }

    private static void OnUpgradeExamine(EntityUid uid, CloningPodComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-cloning-speed", component.SpeedMultiplier);
        args.AddPercentageUpgrade("machine-upgrade-cloning-safety", component.CloningSafety);
    }
}
