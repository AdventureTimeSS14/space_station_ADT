using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

namespace Content.Server.ADT.Construction.Systems;

public sealed class MonkeyRecyclerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyMonkeyRecyclerComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<XenobiologyMonkeyRecyclerComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, XenobiologyMonkeyRecyclerComponent component, RefreshPartsEvent args)
    {
        var matterTier = args.GetPartRating(MachinePartIds.MatterBin, 1f);
        component.CubeProduction = (int)matterTier;
    }

    private static void OnUpgradeExamine(EntityUid uid, XenobiologyMonkeyRecyclerComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-monkey-output", component.CubeProduction, benefit: true);
    }
}
