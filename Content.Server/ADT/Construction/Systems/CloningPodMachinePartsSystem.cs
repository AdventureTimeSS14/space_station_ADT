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
        component.SpeedMultiplier = args.GetStatMultiplier(MachineStat.Speed);
        component.CloningSafety = args.GetStatMultiplier(MachineStat.Output);
    }

    private static void OnUpgradeExamine(EntityUid uid, CloningPodComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-cloning-speed", component.SpeedMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-cloning-safety", component.CloningSafety, benefit: true);
    }
}
