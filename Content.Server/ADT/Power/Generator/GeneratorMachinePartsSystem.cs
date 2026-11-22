using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Power.Generator;

public sealed class GeneratorMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneratorMachinePartsComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<GeneratorMachinePartsComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, GeneratorMachinePartsComponent component, RefreshPartsEvent args)
    {
        var laserTier = args.GetPartRating(MachinePartIds.MicroLaser, 1f);
        var matterTier = args.GetPartRating(MachinePartIds.MatterBin, 1f);

        component.OutputMultiplier = GetLaserOutput(laserTier) * GetMatterOutput(matterTier);
        component.ConsumptionMultiplier = GetLaserConsumption(laserTier) * GetMatterConsumption(matterTier);
    }

    private void OnUpgradeExamine(EntityUid uid, GeneratorMachinePartsComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-power-output", component.OutputMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-fuel-consumption", component.ConsumptionMultiplier, benefit: false);
    }

    private static float GetLaserOutput(float tier) => tier switch
    {
        >= 5f => 2f,
        >= 4f => 1.5f,
        >= 3f => 1.3333f,
        >= 2f => 1.1667f,
        _ => 1f,
    };

    private static float GetLaserConsumption(float tier) => tier switch
    {
        >= 5f => 2.25f,
        >= 4f => 1.75f,
        >= 3f => 1.5f,
        >= 2f => 1.25f,
        _ => 1f,
    };

    private static float GetMatterConsumption(float tier) => tier switch
    {
        >= 5f => 0.25f,
        >= 4f => 0.5f,
        >= 3f => 0.7f,
        >= 2f => 0.85f,
        _ => 1f,
    };

    private static float GetMatterOutput(float tier) => tier switch
    {
        >= 5f => 0.5f,
        >= 4f => 0.7f,
        >= 3f => 0.8f,
        >= 2f => 0.9f,
        _ => 1f,
    };
}